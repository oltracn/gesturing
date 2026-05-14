using System.Text.Json.Serialization;

namespace Gesturing.Models;

public class GestureRule
{
    [JsonPropertyName("gesture_sequence")]
    public List<string> GestureSequence { get; set; } = new();

    [JsonPropertyName("hotkey")]
    public HotkeyDef Hotkey { get; set; } = new();
}
