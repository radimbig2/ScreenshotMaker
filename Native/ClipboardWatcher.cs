using System.ComponentModel;

namespace ScreenshotMaker.Native
{
    internal sealed class ClipboardWatcher : NativeWindow, IDisposable
    {
        public event EventHandler? ClipboardChanged;

        public ClipboardWatcher()
        {
            CreateHandle(new CreateParams { Parent = NativeMethods.HWND_MESSAGE });

            if (!NativeMethods.AddClipboardFormatListener(Handle))
            {
                var error = new Win32Exception();
                DestroyHandle();
                throw error;
            }
        }

        public void Dispose()
        {
            if (Handle == 0)
            {
                return;
            }

            NativeMethods.RemoveClipboardFormatListener(Handle);
            DestroyHandle();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_CLIPBOARDUPDATE)
            {
                ClipboardChanged?.Invoke(this, EventArgs.Empty);
            }

            base.WndProc(ref m);
        }
    }
}
