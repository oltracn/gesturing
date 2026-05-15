using Gesturing.Native;
using static Gesturing.Native.NativeMethods;

namespace Gesturing.Services;

public static class VirtualKeyMapper
{
    private static readonly Dictionary<string, ushort> _keyMap = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<ushort, string> _vkToName = new();

    private static void AddMapping(string name, ushort vk)
    {
        if (!_keyMap.ContainsKey(name))
        {
            _keyMap[name] = vk;
        }
        _vkToName.TryAdd(vk, name);
    }

    static VirtualKeyMapper()
    {
        AddMapping("BrowserBack", VK_BROWSER_BACK);
        AddMapping("BrowserForward", VK_BROWSER_FORWARD);
        AddMapping("BrowserRefresh", VK_BROWSER_REFRESH);
        AddMapping("BrowserStop", VK_BROWSER_STOP);
        AddMapping("BrowserSearch", VK_BROWSER_SEARCH);
        AddMapping("BrowserFavorites", VK_BROWSER_FAVORITES);
        AddMapping("BrowserHome", VK_BROWSER_HOME);
        AddMapping("Esc", 0x1B);
        AddMapping("Space", 0x20);
        AddMapping("Tab", 0x09);
        AddMapping("Enter", 0x0D);
        AddMapping("Backspace", 0x08);
        AddMapping("Delete", 0x2E);
        AddMapping("Insert", 0x2D);
        AddMapping("Home", 0x24);
        AddMapping("End", 0x23);
        AddMapping("PageUp", 0x21);
        AddMapping("PageDown", 0x22);
        AddMapping("Left", 0x25);
        AddMapping("Right", 0x27);
        AddMapping("Up", 0x26);
        AddMapping("Down", 0x28);
        AddMapping("PrintScreen", 0x2C);
        AddMapping("Pause", 0x13);
        AddMapping("CapsLock", 0x14);
        AddMapping("NumLock", 0x90);
        AddMapping("ScrollLock", 0x91);

        // A-Z
        for (char c = 'A'; c <= 'Z'; c++)
        {
            AddMapping(c.ToString(), (ushort)c);
        }

        // 0-9
        for (char c = '0'; c <= '9'; c++)
        {
            AddMapping(c.ToString(), (ushort)c);
        }

        // F1-F24
        for (int i = 1; i <= 24; i++)
        {
            AddMapping($"F{i}", (ushort)(0x6F + i));
        }

        // aliases (name → VK only, reverse uses canonical name)
        _keyMap["Escape"] = 0x1B;
        _keyMap["Return"] = 0x0D;
        _keyMap["Del"] = 0x2E;
        _keyMap["Ins"] = 0x2D;
        _keyMap["Back"] = 0x08;
    }

    public static ushort ToVkCode(string keyName)
    {
        if (_keyMap.TryGetValue(keyName, out var vk))
        {
            return vk;
        }

        // try single character
        if (keyName.Length == 1)
        {
            char c = keyName[0];
            if (c >= 'a' && c <= 'z')
            {
                return (ushort)char.ToUpperInvariant(c);
            }

            if (c >= 'A' && c <= 'Z')
            {
                return (ushort)c;
            }

            if (c >= '0' && c <= '9')
            {
                return (ushort)c;
            }
        }

        return 0;
    }

    public static ushort ToModifierVk(string modifier)
    {
        return modifier.ToLowerInvariant() switch
        {
            "ctrl" or "control" => VK_CONTROL,
            "shift" => VK_SHIFT,
            "alt" => VK_MENU,
            "win" or "windows" or "cmd" => VK_LWIN,
            _ => 0
        };
    }

    public static string ToKeyName(ushort vk)
    {
        if (_vkToName.TryGetValue(vk, out var name))
        {
            return name;
        }

        // fallback: single character representation
        if (vk >= 0x30 && vk <= 0x39) return ((char)vk).ToString();
        if (vk >= 0x41 && vk <= 0x5A) return ((char)vk).ToString();

        return $"0x{vk:X2}";
    }
}
