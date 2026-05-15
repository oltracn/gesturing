namespace Gesturing.Models;

public static class DefaultConfig
{
    public static AppConfig Create()
    {
        return new AppConfig
        {
            AutoStart = false,
            IsEnabled = true,
            TargetProcesses = new List<string>
            {
                "chrome.exe",
                "msedge.exe",
                "firefox.exe"
            },
            Rules = new List<GestureRule>
            {
                new()
                {
                    GestureSequence = new List<string> { "L" },
                    Hotkey = new HotkeyDef
                    {
                        Modifiers = new List<string>(),
                        Key = "BrowserBack"
                    }
                },
                new()
                {
                    GestureSequence = new List<string> { "R" },
                    Hotkey = new HotkeyDef
                    {
                        Modifiers = new List<string>(),
                        Key = "BrowserForward"
                    }
                },
                new()
                {
                    GestureSequence = new List<string> { "D", "R" },
                    Hotkey = new HotkeyDef
                    {
                        Modifiers = new List<string> { "Ctrl" },
                        Key = "W"
                    }
                },
                new()
                {
                    GestureSequence = new List<string> { "D", "L" },
                    Hotkey = new HotkeyDef
                    {
                        Modifiers = new List<string> { "Ctrl", "Shift" },
                        Key = "T"
                    }
                }
            }
        };
    }
}
