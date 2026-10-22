using CursorLock.Native;
using static CursorLock.Native.User32;

namespace CursorLock.Ui;

/// <summary>The Windows message font (the one dialogs use), scaled for a given DPI.</summary>
internal static unsafe class SystemFont
{
    const string FallbackFace = "Segoe UI";
    const int FallbackSize = 12;
    const int FaceNameCapacity = 32;

    public static LOGFONTW GetMessageFont(uint dpi)
    {
        NONCLIENTMETRICSW metrics = default;
        metrics.cbSize = (uint)sizeof(NONCLIENTMETRICSW);
        if (SystemParametersInfoForDpi(SPI_GETNONCLIENTMETRICS, metrics.cbSize, &metrics, 0, dpi))
            return metrics.lfMessageFont;

        LOGFONTW font = default;
        font.lfHeight = -Dpi.Scale(FallbackSize, dpi);
        FallbackFace.AsSpan().CopyTo(new Span<char>(font.lfFaceName, FaceNameCapacity - 1));
        return font;
    }

    public static nint CreateMessageFont(uint dpi)
    {
        LOGFONTW font = GetMessageFont(dpi);
        return Gdi32.CreateFontIndirectW(&font);
    }
}
