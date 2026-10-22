using CursorLock.Native;
using static CursorLock.Native.Gdi32;
using static CursorLock.Native.User32;

namespace CursorLock.Ui;

/// <summary>
/// Tray icons drawn in memory (no files): grey circle = free, red circle = locked.
/// Created once and only recreated when the DPI changes or Explorer restarts.
/// </summary>
internal static unsafe class StatusIcons
{
    // 0xRRGGBB
    const uint FreeFill = 0x9E9E9E;
    const uint FreeEdge = 0x5A5A5A;
    const uint LockedFill = 0xE53935;
    const uint LockedEdge = 0x8E1B1B;
    const int FallbackSize = 16;

    static nint s_free;
    static nint s_locked;

    public static nint Get(bool locked) => locked ? s_locked : s_free;

    /// <summary>Creates both icons at the small-icon size for <paramref name="dpi"/>, replacing previous ones.</summary>
    public static void Create(uint dpi)
    {
        Destroy(); // the shell keeps its own copy of the icon currently shown
        int size = GetSystemMetricsForDpi(SM_CXSMICON, dpi);
        if (size <= 0)
            size = FallbackSize;
        s_free = CircleIcon.Create(size, FreeFill, FreeEdge);
        s_locked = CircleIcon.Create(size, LockedFill, LockedEdge);
    }

    public static void Destroy()
    {
        DestroyIfCreated(ref s_free);
        DestroyIfCreated(ref s_locked);
    }

    static void DestroyIfCreated(ref nint icon)
    {
        if (icon != 0)
            DestroyIcon(icon);
        icon = 0;
    }

    /// <summary>Anti-aliased circle with a border, in a 32 bpp DIB with straight (non-premultiplied) alpha.</summary>
    static class CircleIcon
    {
        public static nint Create(int size, uint fill, uint edge)
        {
            nint color = CreateColorBitmap(size, out uint* pixels);
            if (color == 0)
                return 0;

            DrawCircle(pixels, size, fill, edge);
            nint mask = CreateEmptyMask(size);

            var iconInfo = new ICONINFO { fIcon = 1, hbmMask = mask, hbmColor = color };
            nint icon = mask != 0 ? CreateIconIndirect(&iconInfo) : 0;

            // CreateIconIndirect copies the bitmaps.
            DeleteObject(color);
            if (mask != 0)
                DeleteObject(mask);
            return icon;
        }

        static nint CreateColorBitmap(int size, out uint* pixels)
        {
            var header = new BITMAPINFOHEADER
            {
                biSize = (uint)sizeof(BITMAPINFOHEADER),
                biWidth = size,
                biHeight = -size, // top-down
                biPlanes = 1,
                biBitCount = 32,
                biCompression = BI_RGB,
            };

            void* bits;
            nint bitmap = CreateDIBSection(0, &header, DIB_RGB_COLORS, &bits, 0, 0);
            pixels = (uint*)bits;
            return bits != null ? bitmap : 0;
        }

        static void DrawCircle(uint* pixels, int size, uint fill, uint edge)
        {
            float center = size / 2f;
            float outerRadius = center - 0.5f;
            float innerRadius = outerRadius - Math.Max(1f, size / 16f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - center;
                    float dy = y + 0.5f - center;
                    float distance = MathF.Sqrt(dx * dx + dy * dy);
                    pixels[y * size + x] = Shade(distance, outerRadius, innerRadius, fill, edge);
                }
            }
        }

        static uint Shade(float distance, float outerRadius, float innerRadius, uint fill, uint edge)
        {
            float coverage = Coverage(outerRadius, distance);
            if (coverage <= 0f)
                return 0;

            float fillShare = Coverage(innerRadius, distance) / coverage; // 0 = border, 1 = fill
            uint red = Mix(edge >> 16, fill >> 16, fillShare);
            uint green = Mix(edge >> 8, fill >> 8, fillShare);
            uint blue = Mix(edge, fill, fillShare);
            uint alpha = (uint)(coverage * 255f + 0.5f);
            return (alpha << 24) | (red << 16) | (green << 8) | blue;
        }

        static float Coverage(float radius, float distance) => Math.Clamp(radius - distance + 0.5f, 0f, 1f);

        static uint Mix(uint from, uint to, float amount)
        {
            float a = from & 0xFF, b = to & 0xFF;
            return (uint)(a + (b - a) * amount + 0.5f);
        }

        /// <summary>All-zero monochrome mask (rows aligned to 16 bits); the alpha channel rules.</summary>
        static nint CreateEmptyMask(int size)
        {
            int stride = ((size + 15) >> 4) << 1;
            int length = stride * size;
            byte* bits = stackalloc byte[length];
            new Span<byte>(bits, length).Clear();
            return CreateBitmap(size, size, 1, 1, bits);
        }
    }
}
