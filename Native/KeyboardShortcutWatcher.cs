using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ScreenshotMaker.Native
{
    internal sealed class KeyboardShortcutWatcher : IDisposable
    {
        private readonly NativeMethods.LowLevelKeyboardProc _hookCallback;
        private nint _hook;

        public event EventHandler<CaptureShortcut>? ShortcutPressed;

        public KeyboardShortcutWatcher()
        {
            _hookCallback = HandleKeyboardEvent;
            var moduleHandle = NativeMethods.GetModuleHandle(null);
            _hook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _hookCallback, moduleHandle, 0);

            if (_hook == 0)
            {
                throw new Win32Exception();
            }
        }

        public void Dispose()
        {
            if (_hook == 0)
            {
                return;
            }

            NativeMethods.UnhookWindowsHookEx(_hook);
            _hook = 0;
        }

        private nint HandleKeyboardEvent(int nCode, nint wParam, nint lParam)
        {
            if (nCode >= 0)
            {
                RaiseShortcutIfMatched((int)wParam, Marshal.ReadInt32(lParam));
            }

            return NativeMethods.CallNextHookEx(_hook, nCode, wParam, lParam);
        }

        private void RaiseShortcutIfMatched(int message, int virtualKey)
        {
            if (virtualKey == NativeMethods.VK_SNAPSHOT)
            {
                ShortcutPressed?.Invoke(this, CaptureShortcut.PrintScreen);
                return;
            }

            var isKeyDown = message is NativeMethods.WM_KEYDOWN or NativeMethods.WM_SYSKEYDOWN;

            if (isKeyDown && virtualKey == NativeMethods.VK_S && IsWinShiftHeld())
            {
                ShortcutPressed?.Invoke(this, CaptureShortcut.SnippingTool);
            }
        }

        private static bool IsWinShiftHeld()
        {
            var isLeftWinHeld = NativeMethods.IsKeyDown(NativeMethods.VK_LWIN);
            var isRightWinHeld = NativeMethods.IsKeyDown(NativeMethods.VK_RWIN);
            return (isLeftWinHeld || isRightWinHeld) && NativeMethods.IsKeyDown(NativeMethods.VK_SHIFT);
        }
    }
}
