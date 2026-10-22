using System.Runtime.InteropServices;
using CursorLock.Native;
using static CursorLock.Native.Gdi32;
using static CursorLock.Native.User32;

namespace CursorLock.Ui;

/// <summary>
/// On-screen notice: a desktop popup that is click-through and never activates. Created the first
/// time it is needed and reused afterwards. It does not draw anything inside the game.
/// </summary>
internal static unsafe class OsdWindow
{
    const string ClassName = "CursorLockOsd";
    const nuint HideTimerId = 1;
    const uint VisibleDurationMs = 1000;
    const byte Opacity = 220;
    const int FontSizePoints = 16;
    const int PointsPerInch = 72;
    const int MarginDip = 24;
    const int HorizontalPaddingDip = 20;
    const int VerticalPaddingDip = 10;
    const uint ExStyle = WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TOPMOST;

    // COLORREF is 0x00BBGGRR
    const uint BackgroundColor = 0x00202020;
    const uint LockedTextColor = 0x005A5AFF;
    const uint FreeTextColor = 0x00E0E0E0;

    static bool s_isClassRegistered;
    static nint s_window;
    static nint s_font;
    static nint s_background;
    static uint s_fontDpi;
    static SIZE s_size;
    static bool s_showsLocked;

    public static void Show(bool locked, nint monitor, in RECT monitorBounds)
    {
        if (!EnsureWindow())
            return;

        uint dpi = Dpi.ForMonitor(monitor);
        if (!EnsureFont(dpi))
            return;

        s_showsLocked = locked;
        int margin = Dpi.Scale(MarginDip, dpi);
        SetWindowPos(s_window, HWND_TOPMOST, monitorBounds.Right - s_size.cx - margin, monitorBounds.Top + margin,
            s_size.cx, s_size.cy, SWP_NOACTIVATE);
        InvalidateRect(s_window, null, false);
        ShowWindow(s_window, SW_SHOWNOACTIVATE);
        UpdateWindow(s_window);
        SetTimer(s_window, HideTimerId, VisibleDurationMs, 0);
    }

    /// <summary>Frees the window and GDI objects. Also used to re-measure the text after a language change.</summary>
    public static void Destroy()
    {
        if (s_window != 0)
            DestroyWindow(s_window);
        s_window = 0;
        DeleteIfCreated(ref s_font);
        DeleteIfCreated(ref s_background);
        s_fontDpi = 0;
    }

    static bool EnsureWindow()
    {
        if (s_window != 0)
            return true;

        s_isClassRegistered = s_isClassRegistered || WindowClass.Register(ClassName, &WindowProc);
        if (!s_isClassRegistered)
            return false;

        s_window = CreateWindowExW(ExStyle, ClassName, "CursorLock OSD", WS_POPUP, 0, 0, 0, 0, 0, 0, AppInstance.Handle, 0);
        if (s_window == 0)
            return false;

        SetLayeredWindowAttributes(s_window, 0, Opacity, LWA_ALPHA);
        s_background = CreateSolidBrush(BackgroundColor);
        return true;
    }

    static bool EnsureFont(uint dpi)
    {
        if (s_font != 0 && s_fontDpi == dpi)
            return true;

        LOGFONTW font = SystemFont.GetMessageFont(dpi);
        font.lfHeight = -Kernel32.MulDiv(FontSizePoints, (int)dpi, PointsPerInch);
        font.lfWeight = FW_BOLD;
        font.lfQuality = ANTIALIASED_QUALITY;

        nint created = CreateFontIndirectW(&font);
        if (created == 0)
            return s_font != 0;

        DeleteIfCreated(ref s_font);
        s_font = created;
        s_fontDpi = dpi;
        s_size = MeasureWindow(dpi);
        return true;
    }

    /// <summary>One size that fits both texts, so the window does not jump between states.</summary>
    static SIZE MeasureWindow(uint dpi)
    {
        string lockedText = Strings.OsdLocked, freeText = Strings.OsdFree;
        SIZE locked = default, free = default;

        nint screen = GetDC(0);
        nint previousFont = SelectObject(screen, s_font);
        GetTextExtentPoint32W(screen, lockedText, lockedText.Length, &locked);
        GetTextExtentPoint32W(screen, freeText, freeText.Length, &free);
        SelectObject(screen, previousFont);
        ReleaseDC(0, screen);

        return new SIZE
        {
            cx = Math.Max(locked.cx, free.cx) + 2 * Dpi.Scale(HorizontalPaddingDip, dpi),
            cy = Math.Max(locked.cy, free.cy) + 2 * Dpi.Scale(VerticalPaddingDip, dpi),
        };
    }

    static void Paint(nint window)
    {
        PAINTSTRUCT paint;
        nint hdc = BeginPaint(window, &paint);
        if (hdc == 0)
            return;

        RECT client;
        GetClientRect(window, &client);
        FillRect(hdc, &client, s_background);
        SetBkMode(hdc, TRANSPARENT);
        SetTextColor(hdc, s_showsLocked ? LockedTextColor : FreeTextColor);
        nint previousFont = SelectObject(hdc, s_font);
        DrawTextW(hdc, s_showsLocked ? Strings.OsdLocked : Strings.OsdFree, -1, &client,
            DT_CENTER | DT_VCENTER | DT_SINGLELINE | DT_NOPREFIX);
        SelectObject(hdc, previousFont);
        EndPaint(window, &paint);
    }

    static void DeleteIfCreated(ref nint gdiObject)
    {
        if (gdiObject != 0)
            DeleteObject(gdiObject);
        gdiObject = 0;
    }

    [UnmanagedCallersOnly]
    static nint WindowProc(nint window, uint message, nint wParam, nint lParam)
    {
        try
        {
            switch (message)
            {
                case WM_PAINT:
                    Paint(window);
                    return 0;
                case WM_ERASEBKGND:
                    return 1;
                case WM_TIMER when (nuint)wParam == HideTimerId:
                    KillTimer(window, HideTimerId);
                    ShowWindow(window, SW_HIDE);
                    return 0;
                case WM_NCHITTEST:
                    return HTTRANSPARENT;
                case WM_MOUSEACTIVATE:
                    return MA_NOACTIVATE;
                case WM_DPICHANGED:
                    return 0; // Show() sizes the window for the target monitor
            }
            return DefWindowProcW(window, message, wParam, lParam);
        }
        catch
        {
            return 0; // an exception must never cross into native code
        }
    }
}
