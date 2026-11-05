using Microsoft.Win32;

namespace ScreenshotMaker.Services
{
    internal static class StartupRegistration
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public static bool IsEnabled
        {
            get
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
                return key?.GetValue(AppInfo.Id) is string;
            }
        }

        private static string QuotedExecutablePath => $"\"{Environment.ProcessPath}\"";

        public static void SetEnabled(bool enabled)
        {
            if (enabled)
            {
                using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
                key.SetValue(AppInfo.Id, QuotedExecutablePath);
            }
            else
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
                key?.DeleteValue(AppInfo.Id, throwOnMissingValue: false);
            }
        }

        public static void RefreshPath()
        {
            if (IsEnabled)
            {
                SetEnabled(true);
            }
        }
    }
}
