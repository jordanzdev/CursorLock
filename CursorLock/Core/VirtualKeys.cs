namespace CursorLock.Core;

/// <summary>Virtual-key codes used by the app and key classification helpers.</summary>
internal static class VirtualKeys
{
    public const uint Cancel = 0x03;
    public const uint Back = 0x08;
    public const uint Tab = 0x09;
    public const uint Return = 0x0D;
    public const uint Shift = 0x10;
    public const uint Control = 0x11;
    public const uint Menu = 0x12;
    public const uint Pause = 0x13;
    public const uint CapsLock = 0x14;
    public const uint Escape = 0x1B;
    public const uint Space = 0x20;
    public const uint PageUp = 0x21;
    public const uint PageDown = 0x22;
    public const uint End = 0x23;
    public const uint Home = 0x24;
    public const uint Left = 0x25;
    public const uint Up = 0x26;
    public const uint Right = 0x27;
    public const uint Down = 0x28;
    public const uint PrintScreen = 0x2C;
    public const uint Insert = 0x2D;
    public const uint Delete = 0x2E;
    public const uint Digit0 = 0x30;
    public const uint Digit9 = 0x39;
    public const uint A = 0x41;
    public const uint Z = 0x5A;
    public const uint LeftWindows = 0x5B;
    public const uint RightWindows = 0x5C;
    public const uint Apps = 0x5D;
    public const uint Numpad0 = 0x60;
    public const uint Numpad9 = 0x69;
    public const uint Multiply = 0x6A;
    public const uint Add = 0x6B;
    public const uint Subtract = 0x6D;
    public const uint Decimal = 0x6E;
    public const uint Divide = 0x6F;
    public const uint F1 = 0x70;
    public const uint F24 = 0x87;
    public const uint NumLock = 0x90;
    public const uint ScrollLock = 0x91;
    public const uint LeftShift = 0xA0;
    public const uint RightAlt = 0xA5;
    public const uint MaxValid = 0xFE;

    /// <summary>Shift, Ctrl, Alt or Windows keys: they cannot be the main key of a hotkey.</summary>
    public static bool IsModifier(uint key) =>
        key is Shift or Control or Menu or LeftWindows or RightWindows or (>= LeftShift and <= RightAlt);

    /// <summary>Keys sent with the E0 prefix; scan-code based APIs need the extended flag for them.</summary>
    public static bool IsExtended(uint key) =>
        key is (>= PageUp and <= Down) or PrintScreen or Insert or Delete or Apps or Divide or NumLock or Cancel;

    public static bool IsDigit(uint key) => key is >= Digit0 and <= Digit9;

    public static bool IsLetter(uint key) => key is >= A and <= Z;

    public static bool IsFunctionKey(uint key) => key is >= F1 and <= F24;

    public static bool IsNumpadDigit(uint key) => key is >= Numpad0 and <= Numpad9;
}
