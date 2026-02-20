using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Coxixo.Models;
using Coxixo.Services;

namespace Coxixo.Controls;

/// <summary>
/// Custom UserControl for capturing and displaying hotkey combinations with styled badges.
/// Replaces the TextBox-based hotkey picker with a more intentional visual design.
/// </summary>
public class HotkeyPickerControl : UserControl
{
    // Dark theme colors (WCAG AA compliant)
    private static readonly Color SurfaceColor = Color.FromArgb(0x25, 0x25, 0x26);
    private static readonly Color BorderColor = Color.FromArgb(0x3C, 0x3C, 0x3C);
    private static readonly Color PrimaryColor = Color.FromArgb(0x00, 0x78, 0xD4);
    private static readonly Color BadgeBgColor = Color.FromArgb(0x00, 0x5A, 0x9E); // Darker shade for WCAG AA
    private static readonly Color BadgeTextColor = Color.White;
    private static readonly Color MutedColor = Color.FromArgb(0x80, 0x80, 0x80);

    // Control state
    private HotkeyCombo? _selectedCombo;
    private HotkeyCombo? _previousCombo; // For cancel/restore
    private bool _isCapturing;
    private string? _validationMessage;
    private string? _validationSeverity; // "error", "warn", or null

    // Pending state during capture (live modifier preview)
    private bool _pendingCtrl;
    private bool _pendingAlt;
    private bool _pendingShift;

    // Typography
    private readonly Font _badgeFont = new Font("Segoe UI", 8F, FontStyle.Bold);
    private readonly Font _placeholderFont = new Font("Segoe UI", 9F);
    private readonly StringFormat _centerFormat;

    /// <summary>
    /// Gets or sets the currently selected hotkey combination.
    /// </summary>
    public HotkeyCombo? SelectedCombo
    {
        get => _selectedCombo;
        set
        {
            _selectedCombo = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets the current validation message (if any).
    /// </summary>
    public string? ValidationMessage => _validationMessage;

    /// <summary>
    /// Gets the current validation severity: "error", "warn", or null.
    /// </summary>
    public string? ValidationSeverity => _validationSeverity;

    /// <summary>
    /// Fired when the selected combo changes.
    /// </summary>
    public event EventHandler? ComboChanged;

    /// <summary>
    /// Fired when validation state changes.
    /// </summary>
    public event EventHandler? ValidationChanged;

    public HotkeyPickerControl()
    {
        // Enable double buffering and user paint
        this.SetStyle(
            ControlStyles.Selectable |
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer,
            true);

        this.MinimumSize = new Size(100, 28);
        this.Size = new Size(280, 32);
        this.Cursor = Cursors.Hand;
        this.TabStop = true;

        _centerFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        EnterCaptureMode();
    }

    private void EnterCaptureMode()
    {
        _isCapturing = true;
        _previousCombo = _selectedCombo;
        _pendingCtrl = false;
        _pendingAlt = false;
        _pendingShift = false;
        _validationMessage = null;
        _validationSeverity = null;
        this.Focus();
        Invalidate();
        ValidationChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CancelCapture()
    {
        _isCapturing = false;
        _selectedCombo = _previousCombo;
        _validationMessage = null;
        _validationSeverity = null;
        Invalidate();
        ValidationChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ResetToDefault()
    {
        _isCapturing = false;
        SelectedCombo = HotkeyCombo.Default();
        _validationMessage = null;
        _validationSeverity = null;
        Invalidate();
        ComboChanged?.Invoke(this, EventArgs.Empty);
        ValidationChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (!_isCapturing)
            return base.ProcessCmdKey(ref msg, keyData);

        var modifiers = keyData & Keys.Modifiers;
        var key = keyData & Keys.KeyCode;

        // Escape cancels
        if (key == Keys.Escape)
        {
            CancelCapture();
            return true;
        }

        // Delete/Backspace resets to default
        if (key == Keys.Delete || key == Keys.Back)
        {
            ResetToDefault();
            return true;
        }

        // Ignore modifier-only presses (they update preview)
        if (key == Keys.ControlKey || key == Keys.ShiftKey || key == Keys.Menu ||
            key == Keys.LWin || key == Keys.RWin || key == Keys.None)
            return true;

        // Build combo from captured input
        var combo = new HotkeyCombo
        {
            Key = key,
            Ctrl = modifiers.HasFlag(Keys.Control),
            Alt = modifiers.HasFlag(Keys.Alt),
            Shift = modifiers.HasFlag(Keys.Shift)
        };

        // Validate
        var validation = HotkeyValidator.Validate(combo);
        if (validation.Result == HotkeyValidator.ValidationResult.Reserved)
        {
            _validationMessage = validation.Message;
            _validationSeverity = "error";
            Invalidate(); // Redraw to show error
            // Do NOT accept — stay in capture mode
            ValidationChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        // Warned or Valid — accept
        SelectedCombo = combo;
        _validationMessage = validation.Message;
        _validationSeverity = validation.Result == HotkeyValidator.ValidationResult.Warned ? "warn" : null;
        _isCapturing = false;
        Invalidate();
        ComboChanged?.Invoke(this, EventArgs.Empty);
        ValidationChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (!_isCapturing)
            return;

        // Update pending modifier preview
        bool changed = false;
        if (e.Control && !_pendingCtrl) { _pendingCtrl = true; changed = true; }
        if (e.Alt && !_pendingAlt) { _pendingAlt = true; changed = true; }
        if (e.Shift && !_pendingShift) { _pendingShift = true; changed = true; }

        if (changed)
            Invalidate();
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        if (!_isCapturing)
            return;

        // Update pending modifier preview
        bool changed = false;
        if (!e.Control && _pendingCtrl) { _pendingCtrl = false; changed = true; }
        if (!e.Alt && _pendingAlt) { _pendingAlt = false; changed = true; }
        if (!e.Shift && _pendingShift) { _pendingShift = false; changed = true; }

        if (changed)
            Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Draw background
        using (var bgBrush = new SolidBrush(SurfaceColor))
        {
            g.FillRectangle(bgBrush, this.ClientRectangle);
        }

        // Draw border
        var borderColor = _isCapturing ? PrimaryColor : BorderColor;
        using (var borderPen = new Pen(borderColor, 1))
        {
            var borderRect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            g.DrawRectangle(borderPen, borderRect);
        }

        if (_selectedCombo == null && !_isCapturing)
        {
            // Draw placeholder text
            using (var placeholderBrush = new SolidBrush(MutedColor))
            {
                var placeholderRect = new RectangleF(8, 0, this.Width - 16, this.Height);
                g.DrawString("Click to set hotkey...", _placeholderFont, placeholderBrush,
                    placeholderRect, new StringFormat { LineAlignment = StringAlignment.Center });
            }
        }
        else
        {
            // Get segments to render
            string[] segments = _isCapturing ? GetPendingSegments() : (_selectedCombo?.ToSegments() ?? Array.Empty<string>());

            // Draw each badge
            float x = 8; // left padding
            float badgeHeight = 18;
            float y = (this.Height - badgeHeight) / 2; // vertically centered

            foreach (var segment in segments)
            {
                var size = g.MeasureString(segment, _badgeFont);
                var badgeWidth = size.Width + 12; // 6px padding each side
                var badgeRect = new RectangleF(x, y, badgeWidth, badgeHeight);

                // Choose colors based on state (pending vs confirmed)
                bool isPending = _isCapturing && segments.Length > 0;
                var bgColor = isPending ? BorderColor : BadgeBgColor;
                var textColor = isPending ? MutedColor : BadgeTextColor;

                // Draw rounded rectangle badge
                using (var path = CreateRoundedRectPath(badgeRect, 3))
                using (var bgBrush = new SolidBrush(bgColor))
                {
                    g.FillPath(bgBrush, path);
                }

                // Draw text centered in badge
                using (var textBrush = new SolidBrush(textColor))
                {
                    g.DrawString(segment, _badgeFont, textBrush, badgeRect, _centerFormat);
                }

                x += badgeWidth + 4; // gap between badges
            }

            // If capturing but no modifiers yet, show "Press a key combination..."
            if (_isCapturing && segments.Length == 0)
            {
                using (var placeholderBrush = new SolidBrush(MutedColor))
                {
                    var placeholderRect = new RectangleF(8, 0, this.Width - 16, this.Height);
                    g.DrawString("Press a key combination...", _placeholderFont, placeholderBrush,
                        placeholderRect, new StringFormat { LineAlignment = StringAlignment.Center });
                }
            }
        }
    }

    private string[] GetPendingSegments()
    {
        var segments = new List<string>();
        if (_pendingCtrl) segments.Add("Ctrl");
        if (_pendingAlt) segments.Add("Alt");
        if (_pendingShift) segments.Add("Shift");
        return segments.ToArray();
    }

    private GraphicsPath CreateRoundedRectPath(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        float diameter = radius * 2;

        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _badgeFont?.Dispose();
            _placeholderFont?.Dispose();
            _centerFormat?.Dispose();
        }
        base.Dispose(disposing);
    }
}
