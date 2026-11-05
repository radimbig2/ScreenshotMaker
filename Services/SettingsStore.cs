using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScreenshotMaker.Services
{
    internal static class SettingsStore
    {
        private static readonly string FilePath = Path.Combine(AppInfo.DataDirectory, "settings.json");
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public static AppSettings Load()
        {
            var settings = TryReadFile() ?? new AppSettings();

            if (string.IsNullOrWhiteSpace(settings.SaveFolder))
            {
                settings.SaveFolder = AppSettings.DefaultSaveFolder;
            }

            return settings;
        }

        public static void Save(AppSettings settings)
        {
            Directory.CreateDirectory(AppInfo.DataDirectory);
            var tempPath = FilePath + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(settings, JsonOptions));
            File.Move(tempPath, FilePath, overwrite: true);
        }

        private static AppSettings? TryReadFile()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    return null;
                }

                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), JsonOptions);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                Trace.TraceWarning($"Could not read settings, using defaults: {ex.Message}");
                return null;
            }
        }
    }
}
