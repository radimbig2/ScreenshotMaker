using ScreenshotMaker.Services;

namespace ScreenshotMaker.UI
{
    internal sealed record GallerySnapshot(LibraryStats Stats, IReadOnlyList<GalleryItem> Items)
    {
        public void DisposeThumbnails()
        {
            foreach (var item in Items)
            {
                item.Thumbnail?.Dispose();
            }
        }
    }
}
