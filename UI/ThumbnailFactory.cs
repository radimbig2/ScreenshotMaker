using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace ScreenshotMaker.UI
{
    internal static class ThumbnailFactory
    {
        public static Bitmap? TryCreate(string path, Size size)
        {
            try
            {
                return Create(path, size);
            }
            catch (Exception ex) when (ex is IOException or ArgumentException or OutOfMemoryException)
            {
                Trace.TraceWarning($"Could not create thumbnail for {path}: {ex.Message}");
                return null;
            }
        }

        private static Bitmap Create(string path, Size size)
        {
            using var stream = File.OpenRead(path);
            using var source = Image.FromStream(stream, useEmbeddedColorManagement: false, validateImageData: false);
            var scale = Math.Min((float)size.Width / source.Width, (float)size.Height / source.Height);
            var width = source.Width * scale;
            var height = source.Height * scale;
            var thumbnail = new Bitmap(size.Width, size.Height);
            using var graphics = Graphics.FromImage(thumbnail);
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.Clear(Color.Transparent);
            graphics.DrawImage(source, (size.Width - width) / 2, (size.Height - height) / 2, width, height);
            return thumbnail;
        }
    }
}
