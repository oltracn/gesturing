using System.Runtime.InteropServices;
using Gesturing.Models;
using Gesturing.Native;
using static Gesturing.Native.NativeMethods;

namespace Gesturing.Services;

public static class InputSender
{
    public static void SendHotkey(HotkeyDef hotkey)
    {
        ushort keyCode = VirtualKeyMapper.ToVkCode(hotkey.Key);
        var inputs = new INPUT[2 + hotkey.Modifiers.Count * 2];
        int idx = 0;

        // press modifiers
        foreach (var mod in hotkey.Modifiers)
        {
            var modCode = VirtualKeyMapper.ToModifierVk(mod);
            inputs[idx++] = MakeKeyboardInput(modCode, false);
        }

        // press main key
        inputs[idx++] = MakeKeyboardInput(keyCode, false);

        // release main key
        inputs[idx++] = MakeKeyboardInput(keyCode, true);

        // release modifiers (reverse order)
        for (int i = hotkey.Modifiers.Count - 1; i >= 0; i--)
        {
            var modCode = VirtualKeyMapper.ToModifierVk(hotkey.Modifiers[i]);
            inputs[idx++] = MakeKeyboardInput(modCode, true);
        }

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    public static void SendRightClick()
    {
        var inputs = new INPUT[2];
        inputs[0] = MakeMouseInput(MOUSEEVENTF_RIGHTDOWN);
        inputs[1] = MakeMouseInput(MOUSEEVENTF_RIGHTUP);
        SendInput(2, inputs, Marshal.SizeOf<INPUT>());
    }

    private static INPUT MakeKeyboardInput(ushort vk, bool isKeyUp)
    {
        return new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = vk,
                    wScan = 0,
                    dwFlags = isKeyUp ? KEYEVENTF_KEYUP : 0,
                    time = 0,
                    dwExtraInfo = nint.Zero
                }
            }
        };
    }

    private static INPUT MakeMouseInput(uint flags)
    {
        return new INPUT
        {
            type = INPUT_MOUSE,
            u = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = 0,
                    dy = 0,
                    mouseData = 0,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = nint.Zero
                }
            }
        };
    }
}
