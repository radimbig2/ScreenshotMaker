using System.ComponentModel;
using System.Diagnostics;

namespace ScreenshotMaker.UI
{
    internal static class ShellLauncher
    {
        public static void Open(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Win32Exception ex)
            {
                ShowError(ex);
            }
        }

        public static void OpenFolder(string folder)
        {
            Directory.CreateDirectory(folder);
            Open(folder);
        }

        public static void ShowInFolder(string path)
        {
            try
            {
                Process.Start("explorer.exe", $"/select,\"{path}\"");
            }
            catch (Win32Exception ex)
            {
                ShowError(ex);
            }
        }

        private static void ShowError(Exception exception)
        {
            MessageBox.Show(exception.Message, AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
