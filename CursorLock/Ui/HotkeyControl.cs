using CursorLock.Core;
using static CursorLock.Native.Comctl32;
using static CursorLock.Native.User32;

namespace CursorLock.Ui;

/// <summary>
/// Wrapper for the native msctls_hotkey32 control, which captures shortcuts without any hook.
/// Its HOTKEYF_* flags differ from the MOD_* flags of RegisterHotKey, so they are mapped both ways.
/// </summary>
internal static class HotkeyControl
{
    const int ModifierShift = 8;
    const uint KeyMask = 0xFF;

    public static void Initialize(nint control, Hotkey hotkey)
    {
        // Single keys and Shift-only combinations are invalid; the control replaces them with Ctrl.
        SendMessageW(control, HKM_SETRULES, (nint)(HKCOMB_NONE | HKCOMB_S), (nint)HOTKEYF_CONTROL);
        SendMessageW(control, HKM_SETHOTKEY, Encode(hotkey), 0);
    }

    public static Hotkey Read(nint control) => Decode(SendMessageW(control, HKM_GETHOTKEY, 0, 0));

    /// <summary>MOD_* + key → HKM_SETHOTKEY value (low byte = key, high byte = HOTKEYF_*).</summary>
    static nint Encode(Hotkey hotkey)
    {
        uint flags = 0;
        if (hotkey.HasModifier(MOD_SHIFT)) flags |= HOTKEYF_SHIFT;
        if (hotkey.HasModifier(MOD_CONTROL)) flags |= HOTKEYF_CONTROL;
        if (hotkey.HasModifier(MOD_ALT)) flags |= HOTKEYF_ALT;
        if (VirtualKeys.IsExtended(hotkey.VirtualKey)) flags |= HOTKEYF_EXT; // so the control names it correctly
        return (nint)((flags << ModifierShift) | (hotkey.VirtualKey & KeyMask));
    }

    /// <summary>HKM_GETHOTKEY value → MOD_* + key.</summary>
    static Hotkey Decode(nint value)
    {
        uint key = (uint)value & KeyMask;
        uint flags = ((uint)value >> ModifierShift) & KeyMask;

        uint modifiers = 0;
        if ((flags & HOTKEYF_SHIFT) != 0) modifiers |= MOD_SHIFT;
        if ((flags & HOTKEYF_CONTROL) != 0) modifiers |= MOD_CONTROL;
        if ((flags & HOTKEYF_ALT) != 0) modifiers |= MOD_ALT;
        return new Hotkey(modifiers, key);
    }
}
