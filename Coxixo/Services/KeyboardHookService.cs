using System.Diagnostics;
using System.Runtime.InteropServices;
using Coxixo.Models;

namespace Coxixo.Services;

/// <summary>
/// Global keyboard hook service that detects push-to-talk key press and release events.
/// Uses WH_KEYBOARD_LL (low-level keyboard hook) to capture both WM_KEYDOWN and WM_KEYUP
/// messages regardless of which application has focus.
/// </summary>
public sealed class KeyboardHookService : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    // Virtual key codes for modifier keys
    private const int VK_CONTROL = 0x11;
    private const int VK_SHIFT = 0x10;
    private const int VK_MENU = 0x12; // Alt

    /// <summary>
    /// Fired when the target hotkey is pressed (key down).
    /// Only fires once per press, auto-repeat is filtered.
    /// </summary>
    public event EventHandler? HotkeyPressed;

    /// <summary>
    /// Fired when the target hotkey is released (key up).
    /// </summary>
    public event EventHandler? HotkeyReleased;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    // IMPORTANT: Store delegate in field to prevent garbage collection
    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hookId = IntPtr.Zero;
    private HotkeyCombo _targetCombo = HotkeyCombo.Default();
    private bool _isKeyDown = false;
    private bool _disposed = false;

    public KeyboardHookService()
    {
        _proc = HookCallback;
    }

    /// <summary>
    /// Gets or sets the target hotkey combination to monitor for push-to-talk.
    /// Default is F8 with no modifiers.
    /// </summary>
    public HotkeyCombo TargetCombo
    {
        get => _targetCombo;
        set => _targetCombo = value ?? HotkeyCombo.Default();
    }

    /// <summary>
    /// Legacy property for backward compatibility during transition.
    /// Sets the target key with no modifiers.
    /// </summary>
    public Keys TargetKey
    {
        get => _targetCombo.Key;
        set => _targetCombo = new HotkeyCombo { Key = value };
    }

    /// <summary>
    /// Gets whether the keyboard hook is currently active.
    /// </summary>
    public bool IsRunning => _hookId != IntPtr.Zero;

    /// <summary>
    /// Starts the global keyboard hook.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when hook installation fails.</exception>
    public void Start()
    {
        if (_hookId != IntPtr.Zero)
            return; // Already started

        _hookId = SetHook(_proc);
        if (_hookId == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(
                $"Failed to install keyboard hook. Error code: {error}");
        }
    }

    /// <summary>
    /// Stops the global keyboard hook.
    /// </summary>
    public void Stop()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
            _isKeyDown = false;
        }
    }

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
            GetModuleHandle(curModule.ModuleName), 0);
    }

    /// <summary>
    /// Checks if a modifier key is currently pressed using GetKeyState.
    /// Uses GetKeyState (not GetAsyncKeyState) for hook message queue synchronization.
    /// </summary>
    private static bool IsKeyDown(int vk) => (GetKeyState(vk) & 0x8000) != 0;

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            var key = (Keys)vkCode;
            var msg = wParam.ToInt32();

            // Check if this is the target key
            if (key == _targetCombo.Key)
            {
                // Check if modifier state matches exactly
                bool ctrlDown = IsKeyDown(VK_CONTROL);
                bool altDown = IsKeyDown(VK_MENU);
                bool shiftDown = IsKeyDown(VK_SHIFT);

                bool modifiersMatch = ctrlDown == _targetCombo.Ctrl
                                   && altDown == _targetCombo.Alt
                                   && shiftDown == _targetCombo.Shift;

                if (modifiersMatch)
                {
                    // Key pressed (not already down - prevents auto-repeat flood)
                    if ((msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN) && !_isKeyDown)
                    {
                        _isKeyDown = true;
                        HotkeyPressed?.Invoke(this, EventArgs.Empty);
                    }
                    // Key released
                    else if ((msg == WM_KEYUP || msg == WM_SYSKEYUP) && _isKeyDown)
                    {
                        _isKeyDown = false;
                        HotkeyReleased?.Invoke(this, EventArgs.Empty);
                    }
                }
            }

            // Handle modifier key release during active hotkey hold
            // If user releases a required modifier before releasing the main key,
            // treat it as a release event (critical for push-to-talk ergonomics)
            if (_isKeyDown && (msg == WM_KEYUP || msg == WM_SYSKEYUP))
            {
                bool isModifierRelease = key == Keys.ControlKey || key == Keys.LControlKey || key == Keys.RControlKey
                                      || key == Keys.ShiftKey || key == Keys.LShiftKey || key == Keys.RShiftKey
                                      || key == Keys.Menu || key == Keys.LMenu || key == Keys.RMenu;

                if (isModifierRelease)
                {
                    // Re-check if modifiers still match; if not, fire release
                    bool ctrlDown = IsKeyDown(VK_CONTROL);
                    bool altDown = IsKeyDown(VK_MENU);
                    bool shiftDown = IsKeyDown(VK_SHIFT);

                    bool stillMatch = ctrlDown == _targetCombo.Ctrl
                                   && altDown == _targetCombo.Alt
                                   && shiftDown == _targetCombo.Shift;

                    if (!stillMatch)
                    {
                        _isKeyDown = false;
                        HotkeyReleased?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }

    // P/Invoke declarations
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook,
        LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
        IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);
}
