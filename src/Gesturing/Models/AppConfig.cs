using System.Text.Json.Serialization;

namespace Gesturing.Models;

public class AppConfig
{
    [JsonPropertyName("auto_start")]
    public bool AutoStart { get; set; }

    [JsonPropertyName("is_enabled")]
    public bool IsEnabled { get; set; }

    [JsonPropertyName("target_processes")]
    public List<string> TargetProcesses { get; set; } = new();

    [JsonPropertyName("rules")]
    public List<GestureRule> Rules { get; set; } = new();
}
