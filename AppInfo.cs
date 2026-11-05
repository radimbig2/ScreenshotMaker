using System.Reflection;

namespace ScreenshotMaker
{
    internal static class AppInfo
    {
        public const string Id = "ScreenshotMaker";
        public const string Name = "Screenshot Maker";
        public const string RepositoryUrl = "https://github.com/radimbig2/ScreenshotMaker";
        private const string IconResourceName = "ScreenshotMaker.app.ico";

        public static string DataDirectory { get; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Id);

        public static string Version { get; } =
            typeof(AppInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion.Split('+')[0]
            ?? "dev";

        public static Icon LoadIcon(Size size)
        {
            using var stream = typeof(AppInfo).Assembly.GetManifestResourceStream(IconResourceName)
                ?? throw new InvalidOperationException("Application icon resource is missing.");
            return new(stream, size);
        }
    }
}
