using System.Diagnostics;
using System.Security;
using Microsoft.VisualBasic.FileIO;
using ScreenshotMaker.Services;

namespace ScreenshotMaker.UI
{
    internal sealed class MainForm : Form
    {
        private const int MaxRecentScreenshots = 60;
        private const int MaxImageListSide = 256;
        private const int SettingsColumnWidth = 300;
        private static readonly Size ThumbnailLogicalSize = new(176, 110);
        private static readonly Size WindowLogicalSize = new(900, 580);
        private readonly AppSettings _settings;
        private readonly ScreenshotLibrary _library;
        private readonly AppUpdater _updater;
        private readonly Action _restartToUpdate;
        private readonly Label _statsLabel = CreateSecondaryLabel(string.Empty);
        private readonly CheckBox _printScreenCheckBox = CreateCheckBox("Print Screen  (PrtSc, Alt+PrtSc)");
        private readonly CheckBox _snippingToolCheckBox = CreateCheckBox("Snipping Tool  (Win+Shift+S)");
        private readonly CheckBox _anyImageCheckBox = CreateCheckBox("Any image copied to the clipboard");
        private readonly Label _folderLabel = CreateSecondaryLabel(string.Empty);
        private readonly ComboBox _formatComboBox = new()
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 200
        };
        private readonly CheckBox _startWithWindowsCheckBox = CreateCheckBox("Start with Windows");
        private readonly CheckBox _notificationsCheckBox = CreateCheckBox("Show a notification after saving");
        private readonly CheckBox _autoUpdateCheckBox = CreateCheckBox("Check for updates automatically");
        private readonly Button _checkUpdatesButton = CreateButton("Check for updates");
        private readonly Label _updateStatusLabel = CreateSecondaryLabel(string.Empty);
        private readonly ImageList _thumbnails = new() { ColorDepth = ColorDepth.Depth32Bit };
        private readonly ListView _galleryView = new()
        {
            Dock = DockStyle.Fill,
            View = View.LargeIcon,
            BorderStyle = BorderStyle.None,
            MultiSelect = false,
            ShowItemToolTips = true
        };
        private readonly Label _emptyGalleryLabel = new()
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = SystemColors.GrayText,
            Text = "No screenshots yet.\nPress PrtSc or Win+Shift+S — they will appear here.",
            Visible = false
        };
        private readonly List<Image> _thumbnailImages = [];
        private int _galleryRefreshVersion;
        private bool _isLoadingSettings;

        public event EventHandler? SettingsChanged;

        public MainForm(
            AppSettings settings,
            ScreenshotLibrary library,
            AppUpdater updater,
            Action restartToUpdate)
        {
            _settings = settings;
            _library = library;
            _updater = updater;
            _restartToUpdate = restartToUpdate;

            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = AppInfo.Name;
            Icon = AppInfo.LoadIcon(new Size(32, 32));
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = WindowLogicalSize;
            MinimumSize = new Size(720, 480);
            Controls.Add(CreateRootLayout());
            ResumeLayout(performLayout: true);

            _thumbnails.ImageSize = ScaleThumbnailSize(LogicalToDeviceUnits(ThumbnailLogicalSize));
            _galleryView.LargeImageList = _thumbnails;
            _galleryView.ContextMenuStrip = CreateGalleryMenu();
            _galleryView.ItemActivate += OnGalleryItemActivate;
            _formatComboBox.Items.AddRange(["PNG — lossless", "JPEG — smaller files"]);

            LoadSettingsIntoControls();
            SubscribeToSettingChanges();
        }

        public void ShowAndActivate()
        {
            Show();

            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }

            Activate();
        }

        public void OnScreenshotSaved()
        {
            if (Visible)
            {
                _ = RefreshGalleryAsync();
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (Visible)
            {
                _ = RefreshGalleryAsync();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeThumbnailImages();
                _thumbnails.Dispose();
            }

            base.Dispose(disposing);
        }

        private TableLayoutPanel CreateRootLayout()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 16, 20, 16),
                ColumnCount = 2,
                RowCount = 2
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, SettingsColumnWidth));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var header = CreateHeader();
            root.Controls.Add(header, 0, 0);
            root.SetColumnSpan(header, 2);
            root.Controls.Add(CreateSettingsPanel(), 0, 1);
            root.Controls.Add(CreateGalleryPanel(), 1, 1);
            return root;
        }

        private TableLayoutPanel CreateHeader()
        {
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 2,
                Margin = new Padding(0, 0, 0, 12)
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var logo = new PictureBox
            {
                Image = AppInfo.LoadIcon(new Size(48, 48)).ToBitmap(),
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(44, 44),
                Margin = new Padding(0, 0, 12, 0)
            };
            var title = new Label
            {
                AutoSize = true,
                Text = AppInfo.Name,
                Font = new Font("Segoe UI Semibold", 16F),
                Margin = new Padding(0)
            };

            header.Controls.Add(logo, 0, 0);
            header.SetRowSpan(logo, 2);
            header.Controls.Add(title, 1, 0);
            header.Controls.Add(_statsLabel, 1, 1);
            return header;
        }

        private FlowLayoutPanel CreateSettingsPanel()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Margin = new Padding(0, 0, 16, 0)
            };

            var changeFolderButton = CreateButton("Change…");
            changeFolderButton.Click += OnChangeFolderClicked;
            var openFolderButton = CreateButton("Open");
            openFolderButton.Click += OnOpenFolderClicked;
            _folderLabel.AutoSize = false;
            _folderLabel.AutoEllipsis = true;
            _folderLabel.Size = new Size(SettingsColumnWidth - 24, 20);
            _checkUpdatesButton.Click += OnCheckUpdatesClicked;
            _updateStatusLabel.MaximumSize = new Size(SettingsColumnWidth - 24, 0);

            Control[] controls =
            [
                CreateSectionHeader("Capture"),
                _printScreenCheckBox,
                _snippingToolCheckBox,
                _anyImageCheckBox,
                CreateSectionHeader("Save to"),
                _folderLabel,
                CreateButtonRow(changeFolderButton, openFolderButton),
                CreateSecondaryLabel("Format"),
                _formatComboBox,
                CreateSectionHeader("General"),
                _startWithWindowsCheckBox,
                _notificationsCheckBox,
                _autoUpdateCheckBox,
                CreateSectionHeader($"Version {AppInfo.Version}"),
                _checkUpdatesButton,
                _updateStatusLabel
            ];

            panel.Controls.AddRange(controls);
            return panel;
        }

        private TableLayoutPanel CreateGalleryPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Margin = new Padding(0)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var openFolderButton = CreateButton("Open folder");
            openFolderButton.Click += OnOpenFolderClicked;
            var galleryHost = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 6, 0, 0)
            };
            galleryHost.Controls.Add(_emptyGalleryLabel);
            galleryHost.Controls.Add(_galleryView);

            panel.Controls.Add(CreateSectionHeader("Recent screenshots"), 0, 0);
            panel.Controls.Add(openFolderButton, 1, 0);
            panel.Controls.Add(galleryHost, 0, 1);
            panel.SetColumnSpan(galleryHost, 2);
            return panel;
        }

        private ContextMenuStrip CreateGalleryMenu()
        {
            ToolStripItem[] items =
            [
                new ToolStripMenuItem("Open", null, OnOpenSelectedClicked),
                new ToolStripMenuItem("Show in folder", null, OnShowSelectedInFolderClicked),
                new ToolStripSeparator(),
                new ToolStripMenuItem("Move to Recycle Bin", null, OnDeleteSelectedClicked)
            ];

            var menu = new ContextMenuStrip();
            menu.Items.AddRange(items);
            menu.Opening += OnGalleryMenuOpening;
            return menu;
        }

        private void LoadSettingsIntoControls()
        {
            _isLoadingSettings = true;
            _printScreenCheckBox.Checked = _settings.CapturePrintScreen;
            _snippingToolCheckBox.Checked = _settings.CaptureSnippingTool;
            _anyImageCheckBox.Checked = _settings.CaptureAnyClipboardImage;
            _formatComboBox.SelectedIndex = (int)_settings.Format;
            _notificationsCheckBox.Checked = _settings.ShowNotifications;
            _autoUpdateCheckBox.Checked = _settings.CheckForUpdates;
            _startWithWindowsCheckBox.Checked = StartupRegistration.IsEnabled;
            UpdateFolderLabel();
            _isLoadingSettings = false;
        }

        private void SubscribeToSettingChanges()
        {
            _printScreenCheckBox.CheckedChanged += OnCaptureSettingChanged;
            _snippingToolCheckBox.CheckedChanged += OnCaptureSettingChanged;
            _anyImageCheckBox.CheckedChanged += OnCaptureSettingChanged;
            _notificationsCheckBox.CheckedChanged += OnCaptureSettingChanged;
            _autoUpdateCheckBox.CheckedChanged += OnCaptureSettingChanged;
            _formatComboBox.SelectedIndexChanged += OnCaptureSettingChanged;
            _startWithWindowsCheckBox.CheckedChanged += OnStartWithWindowsChanged;
        }

        private void SaveSettings()
        {
            try
            {
                SettingsStore.Save(_settings);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                ShowWarning($"Could not save settings: {ex.Message}");
            }

            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateFolderLabel()
        {
            _folderLabel.Text = _settings.SaveFolder;
            _folderLabel.Tag = _settings.SaveFolder;
        }

        private async Task RefreshGalleryAsync()
        {
            var version = ++_galleryRefreshVersion;
            var thumbnailSize = _thumbnails.ImageSize;

            try
            {
                var snapshot = await Task.Run(() => LoadGallerySnapshot(thumbnailSize));

                if (version != _galleryRefreshVersion || IsDisposed)
                {
                    snapshot.DisposeThumbnails();
                    return;
                }

                ApplyGallerySnapshot(snapshot);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Trace.TraceWarning($"Could not load the gallery: {ex.Message}");
                _statsLabel.Text = "The screenshots folder is not available";
            }
        }

        private GallerySnapshot LoadGallerySnapshot(Size thumbnailSize)
        {
            var stats = _library.GetStats();
            var items = _library.GetRecent(MaxRecentScreenshots)
                .Select(file => new GalleryItem(file, ThumbnailFactory.TryCreate(file.FullName, thumbnailSize)))
                .ToList();
            return new(stats, items);
        }

        private void ApplyGallerySnapshot(GallerySnapshot snapshot)
        {
            _statsLabel.Text = DisplayFormat.LibrarySummary(snapshot.Stats);
            _galleryView.BeginUpdate();
            _galleryView.Items.Clear();
            _thumbnails.Images.Clear();
            DisposeThumbnailImages();

            foreach (var item in snapshot.Items)
            {
                _galleryView.Items.Add(CreateGalleryListItem(item));
            }

            _galleryView.EndUpdate();
            _emptyGalleryLabel.Visible = snapshot.Items.Count == 0;
            _galleryView.Visible = snapshot.Items.Count > 0;
        }

        private ListViewItem CreateGalleryListItem(GalleryItem item)
        {
            var listItem = new ListViewItem(item.File.LastWriteTime.ToString("d MMM, HH:mm"))
            {
                Tag = item.File.FullName,
                ToolTipText = $"{item.File.Name}\n{DisplayFormat.FileSize(item.File.Length)}"
            };

            if (item.Thumbnail is not null)
            {
                _thumbnails.Images.Add(item.File.FullName, item.Thumbnail);
                _thumbnailImages.Add(item.Thumbnail);
                listItem.ImageKey = item.File.FullName;
            }

            return listItem;
        }

        private void DisposeThumbnailImages()
        {
            foreach (var image in _thumbnailImages)
            {
                image.Dispose();
            }

            _thumbnailImages.Clear();
        }

        private string? GetSelectedPath()
        {
            if (_galleryView.SelectedItems.Count == 0)
            {
                return null;
            }

            return _galleryView.SelectedItems[0].Tag as string;
        }

        private async Task CheckForUpdatesAsync()
        {
            if (!_updater.IsInstalled)
            {
                _updateStatusLabel.Text = "Updates are available in the installed version only.";
                return;
            }

            _checkUpdatesButton.Enabled = false;
            _updateStatusLabel.Text = "Checking for updates…";

            try
            {
                await DownloadLatestUpdateAsync();
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"Update check failed: {ex}");
                _updateStatusLabel.Text = $"Update check failed: {ex.Message}";
            }
            finally
            {
                _checkUpdatesButton.Enabled = true;
            }
        }

        private async Task DownloadLatestUpdateAsync()
        {
            if (_updater.DownloadedUpdate is null)
            {
                var update = await _updater.CheckAsync();

                if (update is null)
                {
                    _updateStatusLabel.Text = "You have the latest version.";
                    return;
                }

                var version = update.TargetFullRelease.Version;
                await _updater.DownloadAsync(update, progress => ReportDownloadProgress(version.ToString(), progress));
            }

            var readyVersion = _updater.DownloadedUpdate!.TargetFullRelease.Version;
            _updateStatusLabel.Text = $"Version {readyVersion} is ready to install.";
            var answer = MessageBox.Show(
                $"Version {readyVersion} has been downloaded. Restart {AppInfo.Name} now to install it?",
                AppInfo.Name,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (answer == DialogResult.Yes)
            {
                _restartToUpdate();
            }
        }

        private void ReportDownloadProgress(string version, int progress)
        {
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }

            BeginInvoke(() => _updateStatusLabel.Text = $"Downloading {version}… {progress}%");
        }

        private void ShowWarning(string message)
        {
            MessageBox.Show(this, message, AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void OnCaptureSettingChanged(object? sender, EventArgs e)
        {
            if (_isLoadingSettings)
            {
                return;
            }

            _settings.CapturePrintScreen = _printScreenCheckBox.Checked;
            _settings.CaptureSnippingTool = _snippingToolCheckBox.Checked;
            _settings.CaptureAnyClipboardImage = _anyImageCheckBox.Checked;
            _settings.ShowNotifications = _notificationsCheckBox.Checked;
            _settings.CheckForUpdates = _autoUpdateCheckBox.Checked;
            _settings.Format = (ImageFileFormat)Math.Max(0, _formatComboBox.SelectedIndex);
            SaveSettings();
        }

        private void OnStartWithWindowsChanged(object? sender, EventArgs e)
        {
            if (_isLoadingSettings)
            {
                return;
            }

            try
            {
                StartupRegistration.SetEnabled(_startWithWindowsCheckBox.Checked);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException)
            {
                ShowWarning($"Could not change the startup setting: {ex.Message}");
                _isLoadingSettings = true;
                _startWithWindowsCheckBox.Checked = StartupRegistration.IsEnabled;
                _isLoadingSettings = false;
            }
        }

        private void OnChangeFolderClicked(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Choose where to save screenshots",
                UseDescriptionForTitle = true,
                InitialDirectory = _settings.SaveFolder,
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            _settings.SaveFolder = dialog.SelectedPath;
            UpdateFolderLabel();
            SaveSettings();
            _ = RefreshGalleryAsync();
        }

        private void OnOpenFolderClicked(object? sender, EventArgs e)
        {
            ShellLauncher.OpenFolder(_settings.SaveFolder);
        }

        private void OnCheckUpdatesClicked(object? sender, EventArgs e)
        {
            _ = CheckForUpdatesAsync();
        }

        private void OnGalleryItemActivate(object? sender, EventArgs e)
        {
            OnOpenSelectedClicked(sender, e);
        }

        private void OnGalleryMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = GetSelectedPath() is null;
        }

        private void OnOpenSelectedClicked(object? sender, EventArgs e)
        {
            if (GetSelectedPath() is { } path)
            {
                ShellLauncher.Open(path);
            }
        }

        private void OnShowSelectedInFolderClicked(object? sender, EventArgs e)
        {
            if (GetSelectedPath() is { } path)
            {
                ShellLauncher.ShowInFolder(path);
            }
        }

        private void OnDeleteSelectedClicked(object? sender, EventArgs e)
        {
            if (GetSelectedPath() is not { } path)
            {
                return;
            }

            try
            {
                FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OperationCanceledException)
            {
                Trace.TraceWarning($"Could not delete {path}: {ex.Message}");
            }

            _ = RefreshGalleryAsync();
        }

        private static Size ScaleThumbnailSize(Size size)
        {
            var scale = Math.Min(1d, (double)MaxImageListSide / Math.Max(size.Width, size.Height));
            return new((int)(size.Width * scale), (int)(size.Height * scale));
        }

        private static CheckBox CreateCheckBox(string text)
        {
            return new()
            {
                AutoSize = true,
                Text = text,
                Margin = new Padding(0, 2, 0, 2)
            };
        }

        private static Button CreateButton(string text)
        {
            return new()
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(8, 2, 8, 2),
                Text = text,
                Margin = new Padding(0, 4, 8, 4)
            };
        }

        private static Label CreateSecondaryLabel(string text)
        {
            return new()
            {
                AutoSize = true,
                Text = text,
                ForeColor = SystemColors.GrayText,
                Margin = new Padding(0, 4, 0, 2)
            };
        }

        private static Label CreateSectionHeader(string text)
        {
            return new()
            {
                AutoSize = true,
                Text = text,
                Font = new Font("Segoe UI Semibold", 10.5F),
                Margin = new Padding(0, 14, 0, 4)
            };
        }

        private static FlowLayoutPanel CreateButtonRow(params Control[] buttons)
        {
            var row = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0)
            };
            row.Controls.AddRange(buttons);
            return row;
        }
    }
}
