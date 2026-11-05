using System.Diagnostics;
using ScreenshotMaker.Services;
using ScreenshotMaker.UI;
using Velopack;

namespace ScreenshotMaker
{
    internal static class Program
    {
        private const string ShowWindowArgument = "--show";

        [STAThread]
        private static void Main(string[] args)
        {
            var isFirstRun = false;

            VelopackApp.Build()
                .OnFirstRun(_ => isFirstRun = true)
                .OnBeforeUninstallFastCallback(_ => StartupRegistration.SetEnabled(false))
                .Run();

            using var singleInstance = SingleInstance.TryAcquire();

            if (singleInstance is null)
            {
                SingleInstance.ActivateRunningInstance();
                return;
            }

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += OnThreadException;
            ApplicationConfiguration.Initialize();
            Application.SetColorMode(SystemColorMode.System);

            var showWindowOnStart = isFirstRun || args.Contains(ShowWindowArgument);
            Application.Run(new TrayApplicationContext(singleInstance, TimeProvider.System, showWindowOnStart));
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Trace.TraceError($"Unhandled UI exception: {e.Exception}");
            MessageBox.Show(e.Exception.Message, AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
