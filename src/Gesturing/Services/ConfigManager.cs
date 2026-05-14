using System.Text.Json;
using Gesturing.Models;

namespace Gesturing.Services;

public static class ConfigManager
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public static AppConfig Load()
    {
        var configPath = ConfigPathResolver.GetConfigPath();

        if (!File.Exists(configPath))
        {
            return DefaultConfig.Create();
        }

        try
        {
            var json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<AppConfig>(json, SerializerOptions)
                   ?? DefaultConfig.Create();
        }
        catch (JsonException)
        {
            var backupPath = configPath + ".bak";
            File.Move(configPath, backupPath, overwrite: true);
            return DefaultConfig.Create();
        }
        catch (IOException)
        {
            return DefaultConfig.Create();
        }
    }

    public static void Save(AppConfig config)
    {
        var configPath = ConfigPathResolver.GetConfigPath();
        var json = JsonSerializer.Serialize(config, SerializerOptions);
        File.WriteAllText(configPath, json);
    }
}
