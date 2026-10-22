using static CursorLock.Native.User32;

namespace CursorLock.Core;

/// <summary>A global shortcut: RegisterHotKey modifiers (MOD_*) plus a virtual-key code.</summary>
internal readonly record struct Hotkey(uint Modifiers, uint VirtualKey)
{
    const uint SupportedModifiers = MOD_ALT | MOD_CONTROL | MOD_SHIFT | MOD_WIN;

    public static Hotkey Default => new(MOD_CONTROL, VirtualKeys.Digit0);

    /// <summary>At least one modifier, only supported modifiers, and a non-modifier main key.</summary>
    public bool IsValid =>
        Modifiers != 0 &&
        (Modifiers & ~SupportedModifiers) == 0 &&
        VirtualKey is > 0 and <= VirtualKeys.MaxValid &&
        !VirtualKeys.IsModifier(VirtualKey);

    public bool HasModifier(uint modifier) => (Modifiers & modifier) != 0;
}
