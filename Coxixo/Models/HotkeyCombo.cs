using System.Windows.Forms;

namespace Coxixo.Models;

/// <summary>
/// Represents a hotkey combination with modifier keys (Ctrl, Alt, Shift).
/// Supports both bare keys (e.g., F8) and modifier combinations (e.g., Ctrl+Shift+F8).
/// </summary>
public class HotkeyCombo
{
    /// <summary>
    /// The primary key in the combination. Default is F8.
    /// </summary>
    public Keys Key { get; set; } = Keys.F8;

    /// <summary>
    /// Whether Ctrl modifier is required.
    /// </summary>
    public bool Ctrl { get; set; }

    /// <summary>
    /// Whether Alt modifier is required.
    /// </summary>
    public bool Alt { get; set; }

    /// <summary>
    /// Whether Shift modifier is required.
    /// </summary>
    public bool Shift { get; set; }

    /// <summary>
    /// Returns true if any modifiers are set.
    /// </summary>
    public bool HasModifiers => Ctrl || Alt || Shift;

    /// <summary>
    /// Returns a factory default HotkeyCombo with F8 and no modifiers.
    /// </summary>
    public static HotkeyCombo Default() => new() { Key = Keys.F8 };

    /// <summary>
    /// Converts the hotkey combo to an array of segments for badge display.
    /// Returns modifiers first (in order: Ctrl, Alt, Shift), then the key name.
    /// Example: Ctrl+Shift+F8 returns ["Ctrl", "Shift", "F8"]
    /// </summary>
    public string[] ToSegments()
    {
        var segments = new List<string>();
        if (Ctrl) segments.Add("Ctrl");
        if (Alt) segments.Add("Alt");
        if (Shift) segments.Add("Shift");
        segments.Add(Key.ToString());
        return segments.ToArray();
    }

    /// <summary>
    /// Converts the hotkey combo to a plain text display string for tooltips.
    /// Example: "Ctrl+Shift+F8"
    /// </summary>
    public string ToDisplayString()
    {
        return string.Join("+", ToSegments());
    }

    /// <summary>
    /// Value equality: two HotkeyCombo instances are equal if all fields match.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not HotkeyCombo other)
            return false;

        return Key == other.Key
            && Ctrl == other.Ctrl
            && Alt == other.Alt
            && Shift == other.Shift;
    }

    /// <summary>
    /// Hash code based on all fields for value equality.
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(Key, Ctrl, Alt, Shift);
    }
}
