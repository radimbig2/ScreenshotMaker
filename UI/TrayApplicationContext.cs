using System.Diagnostics;
using System.Security;
using ScreenshotMaker.Services;
using Velopack;

namespace ScreenshotMaker.UI
{
    internal sealed class TrayApplicationContext : ApplicationContext
    {
        private const int BalloonTimeoutMilliseconds = 3000;
        private static readonly TimeSpan BackgroundUpdateCheckDelay = TimeSpan.FromSeconds(15);
        private readonly SingleInstance _singleInstance;
        private readonly TimeProvider _timeProvider;
        private readonly AppSettings _settings;
        private readonly ScreenshotLibrary _library;
        private readonly CaptureService _captureService;
        private readonly AppUpdater _updater = new();
        private readonly Control _uiThreadInvoker = new();
        private readonly ToolStripMenuItem _openLastItem;
        private readonly ToolStripMenuItem _pauseItem;
        private readonly ToolStripMenuItem _installUpdateItem;
        private readonly NotifyIcon _trayIcon;
        private MainForm? _mainForm;
        private string? _lastSavedPath;
        private string? _balloonFilePath;

        public TrayApplicationContext(
            SingleInstance singleInstance,
            TimeProvider timeProvider,
            bool showWindowOnStart)
        {
            _singleInstance = singleInstance;
            _timeProvider = timeProvider;
            _settings = SettingsStore.Load();
            _library = new ScreenshotLibrary(_settings, timeProvider);
            _captureService = new CaptureService(_settings, _library, timeProvider);
            _ = _uiThreadInvoker.Handle;

            _openLastItem = new ToolStripMenuItem("Open last screenshot", null, OnOpenLastClicked) { Enabled = false };
            _pauseItem = new ToolStripMenuItem("Pause capturing", null, OnPauseClicked) { CheckOnClick = true };
            _installUpdateItem = new ToolStripMenuItem("Restart to update", null, OnInstallUpdateClicked)
            {
                Visible = false
            };
            _trayIcon = new NotifyIcon
            {
                Icon = AppInfo.LoadIcon(SystemInformation.SmallIconSize),
                Text = AppInfo.Name,
                ContextMenuStrip = CreateTrayMenu(),
                Visible = true
            };

            _trayIcon.MouseClick += OnTrayIconMouseClick;
            _trayIcon.BalloonTipClicked += OnBalloonTipClicked;
            _captureService.ScreenshotSaved += OnScreenshotSaved;
            _captureService.SaveFailed += OnSaveFailed;
            _updater.UpdateDownloaded += OnUpdateDownloaded;
            _singleInstance.ListenForActivation(OnActivationRequested);

            RefreshStartupEntry();

            if (showWindowOnStart)
            {
                ShowMainWindow();
            }

            _ = RefreshTrayStateAsync();
            _ = CheckForUpdatesInBackgroundAsync();
        }

        protected override void ExitThreadCore()
        {
            _trayIcon.Visible = false;
            _captureService.Dispose();
            _mainForm?.Dispose();
            _trayIcon.Dispose();
            _uiThreadInvoker.Dispose();
            base.ExitThreadCore();
        }

        private ContextMenuStrip CreateTrayMenu()
        {
            var openItem = new ToolStripMenuItem($"Open {AppInfo.Name}", null, OnOpenClicked)
            {
                Font = new Font(SystemFonts.MenuFont ?? Control.DefaultFont, FontStyle.Bold)
            };

            ToolStripItem[] items =
            [
                openItem,
                new ToolStripMenuItem("Open screenshots folder", null, OnOpenFolderClicked),
                _openLastItem,
                new ToolStripSeparator(),
                _pauseItem,
                _installUpdateItem,
                new ToolStripSeparator(),
                new ToolStripMenuItem("Exit", null, OnExitClicked)
            ];

            var menu = new ContextMenuStrip();
            menu.Items.AddRange(items);
            return menu;
        }

        private void ShowMainWindow()
        {
            if (_mainForm is null || _mainForm.IsDisposed)
            {
                _mainForm = new MainForm(_settings, _library, _updater, RestartToUpdate);
                _mainForm.SettingsChanged += OnSettingsChanged;
            }

            _mainForm.ShowAndActivate();
        }

        private void RestartToUpdate()
        {
            _trayIcon.Visible = false;
            _updater.ApplyAndRestart();
        }

        private void ShowBalloon(string title, string text, ToolTipIcon icon, string? filePath)
        {
            _balloonFilePath = filePath;
            _trayIcon.ShowBalloonTip(BalloonTimeoutMilliseconds, title, text, icon);
        }

        private async Task RefreshTrayStateAsync()
        {
            try
            {
                var (stats, recent) = await Task.Run(() => (_library.GetStats(), _library.GetRecent(1)));
                _lastSavedPath ??= recent.FirstOrDefault()?.FullName;
                _openLastItem.Enabled = _lastSavedPath is not null;
                UpdateTrayText(DisplayFormat.LibrarySummary(stats));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Trace.TraceWarning($"Could not read the screenshots folder: {ex.Message}");
                UpdateTrayText("Screenshots folder is not available");
            }
        }

        private void UpdateTrayText(string summary)
        {
            var text = $"{AppInfo.Name}\n{summary}";

            if (_captureService.IsPaused)
            {
                text += "\nPaused";
            }

            _trayIcon.Text = text;
        }

        private async Task CheckForUpdatesInBackgroundAsync()
        {
            if (!_settings.CheckForUpdates || !_updater.IsInstalled)
            {
                return;
            }

            try
            {
                await Task.Delay(BackgroundUpdateCheckDelay, _timeProvider);
                var update = await _updater.CheckAsync();

                if (update is not null)
                {
                    await _updater.DownloadAsync(update, null);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"Background update check failed: {ex.Message}");
            }
        }

        private void RefreshStartupEntry()
        {
            if (!_updater.IsInstalled)
            {
                return;
            }

            try
            {
                StartupRegistration.RefreshPath();
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException)
            {
                Trace.TraceWarning($"Could not refresh the startup entry: {ex.Message}");
            }
        }

        private void OnActivationRequested()
        {
            _uiThreadInvoker.BeginInvoke(ShowMainWindow);
        }

        private void OnScreenshotSaved(object? sender, string path)
        {
            _lastSavedPath = path;
            _openLastItem.Enabled = true;

            if (_settings.ShowNotifications)
            {
                ShowBalloon("Screenshot saved", $"{Path.GetFileName(path)}\nClick to open", ToolTipIcon.None, path);
            }

            _mainForm?.OnScreenshotSaved();
            _ = RefreshTrayStateAsync();
        }

        private void OnSaveFailed(object? sender, Exception exception)
        {
            ShowBalloon("Could not save screenshot", exception.Message, ToolTipIcon.Error, null);
        }

        private void OnUpdateDownloaded(object? sender, UpdateInfo update)
        {
            var version = update.TargetFullRelease.Version;
            _installUpdateItem.Text = $"Restart to update to {version}";
            _installUpdateItem.Visible = true;
            var message = $"Version {version} will be installed on the next restart.";
            ShowBalloon("Update ready", message, ToolTipIcon.Info, null);
        }

        private void OnSettingsChanged(object? sender, EventArgs e)
        {
            _lastSavedPath = null;
            _ = RefreshTrayStateAsync();
        }

        private void OnTrayIconMouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowMainWindow();
            }
        }

        private void OnBalloonTipClicked(object? sender, EventArgs e)
        {
            if (_balloonFilePath is not null && File.Exists(_balloonFilePath))
            {
                ShellLauncher.Open(_balloonFilePath);
                return;
            }

            ShowMainWindow();
        }

        private void OnOpenClicked(object? sender, EventArgs e)
        {
            ShowMainWindow();
        }

        private void OnOpenFolderClicked(object? sender, EventArgs e)
        {
            ShellLauncher.OpenFolder(_library.Folder);
        }

        private void OnOpenLastClicked(object? sender, EventArgs e)
        {
            if (_lastSavedPath is not null && File.Exists(_lastSavedPath))
            {
                ShellLauncher.Open(_lastSavedPath);
            }
        }

        private void OnPauseClicked(object? sender, EventArgs e)
        {
            _captureService.IsPaused = _pauseItem.Checked;
            _ = RefreshTrayStateAsync();
        }

        private void OnInstallUpdateClicked(object? sender, EventArgs e)
        {
            RestartToUpdate();
        }

        private void OnExitClicked(object? sender, EventArgs e)
        {
            ExitThread();
        }
    }
}
