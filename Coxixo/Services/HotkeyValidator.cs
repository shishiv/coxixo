using System.Windows.Forms;
using Coxixo.Models;

namespace Coxixo.Services;

/// <summary>
/// Validates hotkey combinations against reserved and potentially conflicting keys.
/// Provides three-level validation: Reserved (hard-blocked), Warned (soft-blocked), Valid.
/// </summary>
public static class HotkeyValidator
{
    /// <summary>
    /// Validation result levels.
    /// </summary>
    public enum ValidationResult
    {
        /// <summary>Valid hotkey combination with no issues.</summary>
        Valid,
        /// <summary>Reserved combination that cannot be used (hard-blocked).</summary>
        Reserved,
        /// <summary>Potentially conflicting combination that requires user acknowledgment (soft-blocked).</summary>
        Warned
    }

    /// <summary>
    /// Validation outcome containing result level and optional message.
    /// </summary>
    public record ValidationOutcome(ValidationResult Result, string? Message);

    // Reserved combinations (hard-blocked)
    private static readonly HashSet<(Keys key, bool ctrl, bool alt, bool shift)> _reserved = new()
    {
        // F12 alone - debugger attach
        (Keys.F12, false, false, false),

        // Ctrl+Alt+Delete - Windows security interrupt (technically unreachable but document it)
        (Keys.Delete, true, true, false),

        // Alt+F4 - close window
        (Keys.F4, false, true, false),

        // Ctrl+Alt+F4 - Windows MDI shortcut
        (Keys.F4, true, true, false),

        // PrintScreen - screenshots (with all modifier combinations)
        (Keys.PrintScreen, false, false, false),
        (Keys.PrintScreen, true, false, false),
        (Keys.PrintScreen, false, true, false),
        (Keys.PrintScreen, false, false, true),
        (Keys.PrintScreen, true, true, false),
        (Keys.PrintScreen, true, false, true),
        (Keys.PrintScreen, false, true, true),
        (Keys.PrintScreen, true, true, true),

        // Escape - cannot be reliably hooked
        (Keys.Escape, false, false, false),
    };

    // Warned combinations (soft-blocked with specific messages)
    private static readonly Dictionary<(Keys key, bool ctrl, bool alt, bool shift), string> _warned = new()
    {
        // Clipboard shortcuts
        [(Keys.C, true, false, false)] = "This may interfere with clipboard shortcuts",
        [(Keys.V, true, false, false)] = "This may interfere with clipboard shortcuts",
        [(Keys.X, true, false, false)] = "This may interfere with clipboard shortcuts",
        [(Keys.A, true, false, false)] = "This may interfere with select-all shortcuts",

        // Undo/redo
        [(Keys.Z, true, false, false)] = "This may interfere with undo/redo",
        [(Keys.Y, true, false, false)] = "This may interfere with undo/redo",

        // Common app shortcuts
        [(Keys.S, true, false, false)] = "This may interfere with saving in other apps",
        [(Keys.W, true, false, false)] = "This may interfere with closing tabs in browsers",

        // Window switching
        [(Keys.Tab, false, true, false)] = "This may interfere with window switching",
    };

    // Modifier-only keys that cannot be used as primary keys
    private static readonly HashSet<Keys> _modifierKeys = new()
    {
        Keys.ControlKey, Keys.LControlKey, Keys.RControlKey,
        Keys.ShiftKey, Keys.LShiftKey, Keys.RShiftKey,
        Keys.Menu, Keys.LMenu, Keys.RMenu,
        Keys.LWin, Keys.RWin,
        Keys.None
    };

    /// <summary>
    /// Validates a hotkey combination against reserved and warned lists.
    /// </summary>
    /// <param name="combo">The hotkey combination to validate.</param>
    /// <returns>Validation outcome with result level and optional message.</returns>
    public static ValidationOutcome Validate(HotkeyCombo combo)
    {
        // Block modifier-only presses
        if (_modifierKeys.Contains(combo.Key))
        {
            return new ValidationOutcome(
                ValidationResult.Reserved,
                "Modifier keys cannot be used as the primary hotkey");
        }

        // Block any combination with Win key as modifier
        // Note: HotkeyCombo doesn't currently have Win modifier support,
        // but if Key itself is Win, it's already caught above.
        // This is future-proofing for when Win modifier is added.

        var tuple = (combo.Key, combo.Ctrl, combo.Alt, combo.Shift);

        // Check reserved first
        if (_reserved.Contains(tuple))
        {
            return tuple switch
            {
                (Keys.F12, false, false, false) =>
                    new ValidationOutcome(ValidationResult.Reserved, "F12 is reserved for debugger attach in Windows"),

                (Keys.Delete, true, true, false) =>
                    new ValidationOutcome(ValidationResult.Reserved, "Ctrl+Alt+Delete is a Windows security interrupt"),

                (Keys.F4, false, true, false) =>
                    new ValidationOutcome(ValidationResult.Reserved, "Alt+F4 closes the active window"),

                (Keys.F4, true, true, false) =>
                    new ValidationOutcome(ValidationResult.Reserved, "Reserved Windows MDI shortcut"),

                var (key, _, _, _) when key == Keys.PrintScreen =>
                    new ValidationOutcome(ValidationResult.Reserved, "PrintScreen is reserved for screenshots"),

                (Keys.Escape, _, _, _) =>
                    new ValidationOutcome(ValidationResult.Reserved, "Escape cannot be used as a hotkey"),

                _ => new ValidationOutcome(ValidationResult.Reserved, "This combination is reserved by Windows")
            };
        }

        // Check warned next
        if (_warned.TryGetValue(tuple, out var message))
        {
            return new ValidationOutcome(ValidationResult.Warned, message);
        }

        // Valid
        return new ValidationOutcome(ValidationResult.Valid, null);
    }
}
