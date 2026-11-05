using System.Drawing.Imaging;

namespace ScreenshotMaker.Services
{
    internal sealed class ScreenshotLibrary
    {
        private const long JpegQuality = 92L;
        private static readonly HashSet<string> ImageExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
        private readonly AppSettings _settings;
        private readonly TimeProvider _timeProvider;
        private readonly Lock _saveLock = new();

        public string Folder => _settings.SaveFolder;

        public ScreenshotLibrary(
            AppSettings settings,
            TimeProvider timeProvider)
        {
            _settings = settings;
            _timeProvider = timeProvider;
        }

        public string Save(Image image)
        {
            lock (_saveLock)
            {
                Directory.CreateDirectory(Folder);
                var baseName = $"Screenshot {_timeProvider.GetLocalNow():yyyy-MM-dd HH.mm.ss}";

                if (_settings.Format == ImageFileFormat.Jpeg)
                {
                    var jpegPath = CreateUniquePath(Folder, baseName, ".jpg");
                    SaveJpeg(image, jpegPath);
                    return jpegPath;
                }

                var pngPath = CreateUniquePath(Folder, baseName, ".png");
                image.Save(pngPath, ImageFormat.Png);
                return pngPath;
            }
        }

        public LibraryStats GetStats()
        {
            var count = 0;
            var totalBytes = 0L;

            foreach (var file in EnumerateImages())
            {
                count++;
                totalBytes += file.Length;
            }

            return new(count, totalBytes);
        }

        public IReadOnlyList<FileInfo> GetRecent(int maxCount)
        {
            return EnumerateImages()
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .Take(maxCount)
                .ToList();
        }

        private IEnumerable<FileInfo> EnumerateImages()
        {
            var directory = new DirectoryInfo(Folder);

            if (!directory.Exists)
            {
                return [];
            }

            return directory.EnumerateFiles().Where(file => ImageExtensions.Contains(file.Extension));
        }

        private static string CreateUniquePath(string folder, string baseName, string extension)
        {
            var path = Path.Combine(folder, baseName + extension);

            for (var index = 2; File.Exists(path); index++)
            {
                path = Path.Combine(folder, $"{baseName} ({index}){extension}");
            }

            return path;
        }

        private static void SaveJpeg(Image image, string path)
        {
            var encoder = ImageCodecInfo.GetImageEncoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
            using var parameters = new EncoderParameters(1);
            parameters.Param[0] = new EncoderParameter(Encoder.Quality, JpegQuality);
            image.Save(path, encoder, parameters);
        }
    }
}
