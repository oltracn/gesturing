namespace Gesturing.Services;

public static class ConfigPathResolver
{
    private const string PortableMarkerFileName = "portable.marker";
    private const string AppDataFolderName = "Gesturing";

    public static bool IsPortableMode()
    {
        var markerPath = Path.Combine(AppContext.BaseDirectory, PortableMarkerFileName);
        return File.Exists(markerPath);
    }

    public static string GetConfigDirectory()
    {
        string directory;

        if (IsPortableMode())
        {
            directory = AppContext.BaseDirectory;
        }
        else
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            directory = Path.Combine(appDataPath, AppDataFolderName);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        return directory;
    }

    public static string GetConfigPath()
    {
        return Path.Combine(GetConfigDirectory(), "config.json");
    }
}
