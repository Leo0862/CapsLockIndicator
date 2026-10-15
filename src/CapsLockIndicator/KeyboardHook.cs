using System.ComponentModel;
using System.Runtime.InteropServices;

namespace CapsLockIndicator;

/// <summary>
/// Global low-level keyboard hook that reports Caps Lock key releases.
/// Must be created on a thread that runs a message loop (the UI thread).
/// </summary>
internal sealed class KeyboardHook : IDisposable
{
    // Keep a reference to the delegate so the GC does not collect it while the hook is active.
    private readonly NativeMethods.LowLevelKeyboardProc _proc;
    private IntPtr _hookHandle;
    private bool _capsDown;

    /// <summary>Raised once per physical Caps Lock press, after the key is released.</summary>
    public event EventHandler? CapsLockPressed;

    public KeyboardHook()
    {
        _proc = HookCallback;
        _hookHandle = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL, _proc, NativeMethods.GetModuleHandle(null), 0);

        if (_hookHandle == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to install keyboard hook.");
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var info = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
            if (info.vkCode == NativeMethods.VK_CAPITAL)
            {
                int msg = wParam.ToInt32();
                if (msg is NativeMethods.WM_KEYDOWN or NativeMethods.WM_SYSKEYDOWN)
                {
                    // Ignore auto-repeat while the key is held down.
                    _capsDown = true;
                }
                else if ((msg is NativeMethods.WM_KEYUP or NativeMethods.WM_SYSKEYUP) && _capsDown)
                {
                    _capsDown = false;
                    CapsLockPressed?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        // Never swallow the key; always pass it on.
        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }
    }
}
