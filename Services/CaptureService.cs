using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using ScreenshotMaker.Native;

namespace ScreenshotMaker.Services
{
    internal sealed class CaptureService : IDisposable
    {
        private const int ClipboardReadAttempts = 5;
        private static readonly TimeSpan ClipboardRetryDelay = TimeSpan.FromMilliseconds(50);
        private static readonly TimeSpan PrintScreenArmDuration = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan SnippingToolArmDuration = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan DuplicateSuppressionWindow = TimeSpan.FromSeconds(5);
        private readonly AppSettings _settings;
        private readonly ScreenshotLibrary _library;
        private readonly TimeProvider _timeProvider;
        private readonly ClipboardWatcher _clipboardWatcher = new();
        private readonly KeyboardShortcutWatcher _shortcutWatcher = new();
        private DateTimeOffset _armedUntil;
        private byte[]? _lastImageHash;
        private DateTimeOffset _lastSavedAt;

        public event EventHandler<string>? ScreenshotSaved;

        public event EventHandler<Exception>? SaveFailed;

        public bool IsPaused { get; set; }

        public CaptureService(
            AppSettings settings,
            ScreenshotLibrary library,
            TimeProvider timeProvider)
        {
            _settings = settings;
            _library = library;
            _timeProvider = timeProvider;
            _shortcutWatcher.ShortcutPressed += OnShortcutPressed;
            _clipboardWatcher.ClipboardChanged += OnClipboardChanged;
        }

        public void Dispose()
        {
            _shortcutWatcher.Dispose();
            _clipboardWatcher.Dispose();
        }

        private void OnShortcutPressed(object? sender, CaptureShortcut shortcut)
        {
            if (IsPaused)
            {
                return;
            }

            var armDuration = shortcut switch
            {
                CaptureShortcut.PrintScreen when _settings.CapturePrintScreen => PrintScreenArmDuration,
                CaptureShortcut.SnippingTool when _settings.CaptureSnippingTool => SnippingToolArmDuration,
                _ => TimeSpan.Zero
            };

            if (armDuration > TimeSpan.Zero)
            {
                _armedUntil = _timeProvider.GetUtcNow() + armDuration;
            }
        }

        private void OnClipboardChanged(object? sender, EventArgs e)
        {
            if (IsPaused)
            {
                return;
            }

            var now = _timeProvider.GetUtcNow();

            if (now >= _armedUntil && !_settings.CaptureAnyClipboardImage)
            {
                return;
            }

            var image = TryGetClipboardImage();

            if (image is null)
            {
                return;
            }

            _armedUntil = default;
            var hash = ComputeHash(image);

            if (IsRecentDuplicate(hash, now))
            {
                image.Dispose();
                return;
            }

            _lastImageHash = hash;
            _lastSavedAt = now;
            _ = SaveAsync(image);
        }

        private bool IsRecentDuplicate(byte[] hash, DateTimeOffset now)
        {
            if (_lastImageHash is null || now - _lastSavedAt >= DuplicateSuppressionWindow)
            {
                return false;
            }

            return hash.AsSpan().SequenceEqual(_lastImageHash);
        }

        private async Task SaveAsync(Bitmap image)
        {
            try
            {
                var path = await Task.Run(() => _library.Save(image));
                ScreenshotSaved?.Invoke(this, path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ExternalException)
            {
                Trace.TraceError($"Could not save screenshot: {ex}");
                SaveFailed?.Invoke(this, ex);
            }
            finally
            {
                image.Dispose();
            }
        }

        private static Bitmap? TryGetClipboardImage()
        {
            for (var attempt = 0; attempt < ClipboardReadAttempts; attempt++)
            {
                try
                {
                    return ReadClipboardImage();
                }
                catch (ExternalException)
                {
                    Thread.Sleep(ClipboardRetryDelay);
                }
            }

            return null;
        }

        private static Bitmap? ReadClipboardImage()
        {
            if (!Clipboard.ContainsImage())
            {
                return null;
            }

            using var image = Clipboard.GetImage();

            if (image is null)
            {
                return null;
            }

            return new(image);
        }

        private static byte[] ComputeHash(Bitmap bitmap)
        {
            var bounds = new Rectangle(Point.Empty, bitmap.Size);
            var data = bitmap.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try
            {
                var bytes = new byte[Math.Abs(data.Stride) * data.Height];
                Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
                return SHA256.HashData(bytes);
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }
    }
}
