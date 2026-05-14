using System.Text.Json.Serialization;

namespace Gesturing.Models;

public class HotkeyDef
{
    [JsonPropertyName("modifiers")]
    public List<string> Modifiers { get; set; } = new();

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;
}
