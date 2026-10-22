using CursorLock.Core;
using static CursorLock.Native.User32;

namespace CursorLock.Ui;

/// <summary>Human-readable hotkey text, e.g. "Ctrl+0" or "Ctrl+Shift+F5".</summary>
internal static unsafe class HotkeyText
{
    const int KeyNameBufferLength = 64;
    const int ScanCodeShift = 16;
    const int ExtendedKeyFlag = 1 << 24;

    public static string Format(Hotkey hotkey)
    {
        string text = "";
        if (hotkey.HasModifier(MOD_CONTROL)) text += "Ctrl+";
        if (hotkey.HasModifier(MOD_ALT)) text += "Alt+";
        if (hotkey.HasModifier(MOD_SHIFT)) text += Strings.ModifierShift + "+";
        if (hotkey.HasModifier(MOD_WIN)) text += "Win+";
        return text + KeyName(hotkey.VirtualKey);
    }

    static string KeyName(uint key)
    {
        if (VirtualKeys.IsDigit(key) || VirtualKeys.IsLetter(key))
            return ((char)key).ToString();
        if (VirtualKeys.IsFunctionKey(key))
            return "F" + (key - VirtualKeys.F1 + 1).ToString();
        if (VirtualKeys.IsNumpadDigit(key))
            return "Num " + (key - VirtualKeys.Numpad0).ToString();

        return Strings.KeyName(key) ?? KeyboardLayoutName(key) ?? "0x" + key.ToString("X2");
    }

    /// <summary>Name from the active keyboard layout, for OEM keys such as "´" or "+".</summary>
    static string? KeyboardLayoutName(uint key)
    {
        uint scanCode = MapVirtualKeyW(key, MAPVK_VK_TO_VSC);
        if (scanCode == 0)
            return null;

        char* buffer = stackalloc char[KeyNameBufferLength];
        int lParam = (int)(scanCode << ScanCodeShift) | (VirtualKeys.IsExtended(key) ? ExtendedKeyFlag : 0);
        int length = GetKeyNameTextW(lParam, buffer, KeyNameBufferLength);
        return length > 0 ? new string(buffer, 0, length) : null;
    }
}
