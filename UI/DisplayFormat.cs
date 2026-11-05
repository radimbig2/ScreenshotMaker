using ScreenshotMaker.Services;

namespace ScreenshotMaker.UI
{
    internal static class DisplayFormat
    {
        private const double BytesPerUnit = 1024d;
        private static readonly string[] SizeUnits = ["B", "KB", "MB", "GB", "TB"];

        public static string FileSize(long bytes)
        {
            var size = (double)bytes;
            var unitIndex = 0;

            while (size >= BytesPerUnit && unitIndex < SizeUnits.Length - 1)
            {
                size /= BytesPerUnit;
                unitIndex++;
            }

            return $"{size:0.#} {SizeUnits[unitIndex]}";
        }

        public static string LibrarySummary(LibraryStats stats)
        {
            if (stats.Count == 1)
            {
                return $"1 screenshot · {FileSize(stats.TotalBytes)}";
            }

            return $"{stats.Count:N0} screenshots · {FileSize(stats.TotalBytes)}";
        }
    }
}
