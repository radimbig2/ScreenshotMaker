namespace ScreenshotMaker.Services
{
    internal sealed class AppSettings
    {
        public static string DefaultSaveFolder { get; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screenshots");

        public string SaveFolder { get; set; } = DefaultSaveFolder;

        public ImageFileFormat Format { get; set; } = ImageFileFormat.Png;

        public bool CapturePrintScreen { get; set; } = true;

        public bool CaptureSnippingTool { get; set; } = true;

        public bool CaptureAnyClipboardImage { get; set; }

        public bool ShowNotifications { get; set; } = true;

        public bool CheckForUpdates { get; set; } = true;
    }
}
