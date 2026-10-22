using CursorLock.Native;

namespace CursorLock.Ui;

internal static class Dpi
{
    public const uint Default = 96;

    /// <summary>Scales a value designed at 96 DPI.</summary>
    public static int Scale(int value, uint dpi) => Kernel32.MulDiv(value, (int)dpi, (int)Default);

    public static uint ForMonitor(nint monitor) =>
        Shcore.GetDpiForMonitor(monitor, Shcore.MDT_EFFECTIVE_DPI, out uint dpi, out _) == 0 && dpi != 0
            ? dpi
            : Default;
}
