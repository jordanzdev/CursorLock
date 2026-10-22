using CursorLock.Core;
using CursorLock.Native;
using static CursorLock.Native.Shell32;
using static CursorLock.Native.User32;

namespace CursorLock.Ui;

internal enum TrayEvent
{
    None,
    ContextMenuRequested,
    Activated,
}

/// <summary>Notification-area icon: state icon, tooltip and warning balloon.</summary>
internal static unsafe class TrayIcon
{
    public const uint CallbackMessage = WM_APP + 1;
    public const nuint AddRetryTimerId = 2;

    const uint IconId = 1;
    const uint AddRetryIntervalMs = 2000;
    const int MaxAddRetries = 30;

    static nint s_window;
    static bool s_isAdded;
    static int s_addRetries;
    static string s_tooltipFree = "";
    static string s_tooltipLocked = "";
    static string? s_pendingWarningTitle;
    static string? s_pendingWarningText;

    public static void Create(nint window)
    {
        s_window = window;
        RefreshTooltips();
        StatusIcons.Create(GetDpiForWindow(window));
        Add();
    }

    /// <summary>Rebuilds both tooltips (only when the hotkey or language changes), so toggling never allocates.</summary>
    public static void RefreshTooltips()
    {
        string hotkey = HotkeyText.Format(Config.Current.Hotkey);
        s_tooltipFree = Strings.TooltipFree(hotkey);
        s_tooltipLocked = Strings.TooltipLocked(hotkey);
        Refresh();
    }

    /// <summary>Shows the current lock state in the icon and tooltip. Allocation-free.</summary>
    public static void Refresh()
    {
        if (!s_isAdded)
            return;
        NOTIFYICONDATAW data = CreateStateData();
        Shell_NotifyIconW(NIM_MODIFY, &data);
    }

    /// <summary>Shows a warning balloon, or keeps it until the icon can be added.</summary>
    public static void ShowWarning(string title, string text)
    {
        if (!s_isAdded)
        {
            s_pendingWarningTitle = title;
            s_pendingWarningText = text;
            return;
        }
        s_pendingWarningTitle = s_pendingWarningText = null;

        NOTIFYICONDATAW data = CreateBaseData();
        data.uFlags = NIF_INFO;
        data.dwInfoFlags = NIIF_WARNING;
        CopyText(text, data.szInfo, InfoCapacity);
        CopyText(title, data.szInfoTitle, InfoTitleCapacity);
        Shell_NotifyIconW(NIM_MODIFY, &data);
    }

    /// <summary>NOTIFYICON_VERSION_4: LOWORD(lParam) is the event and wParam holds the anchor point.</summary>
    public static TrayEvent ParseCallback(nint wParam, nint lParam, out POINT anchor)
    {
        anchor = new POINT
        {
            X = (short)((long)wParam & 0xFFFF),
            Y = (short)(((long)wParam >> 16) & 0xFFFF),
        };
        return ((uint)lParam & 0xFFFF) switch
        {
            WM_CONTEXTMENU => TrayEvent.ContextMenuRequested,
            WM_LBUTTONDBLCLK or NIN_KEYSELECT => TrayEvent.Activated,
            _ => TrayEvent.None,
        };
    }

    /// <summary>Explorer restarted: the previous icon no longer exists.</summary>
    public static void OnTaskbarCreated()
    {
        s_isAdded = false;
        StatusIcons.Create(GetDpiForWindow(s_window));
        Add();
    }

    public static void OnDpiChanged(uint dpi)
    {
        StatusIcons.Create(dpi);
        Refresh();
    }

    public static void OnAddRetryTimer()
    {
        if (s_isAdded || ++s_addRetries > MaxAddRetries)
        {
            KillTimer(s_window, AddRetryTimerId);
            return;
        }
        Add();
    }

    public static void Remove()
    {
        if (s_window != 0)
            KillTimer(s_window, AddRetryTimerId);
        if (!s_isAdded)
            return;

        NOTIFYICONDATAW data = CreateBaseData();
        Shell_NotifyIconW(NIM_DELETE, &data);
        s_isAdded = false;
    }

    static void Add()
    {
        NOTIFYICONDATAW data = CreateStateData();
        s_isAdded = Shell_NotifyIconW(NIM_ADD, &data) || Shell_NotifyIconW(NIM_MODIFY, &data);
        if (!s_isAdded)
        {
            // Explorer is not ready yet (session start-up): retry for a while.
            SetTimer(s_window, AddRetryTimerId, AddRetryIntervalMs, 0);
            return;
        }

        KillTimer(s_window, AddRetryTimerId);
        s_addRetries = 0;
        data.uVersion = NOTIFYICON_VERSION_4;
        Shell_NotifyIconW(NIM_SETVERSION, &data);

        if (s_pendingWarningTitle != null && s_pendingWarningText != null)
            ShowWarning(s_pendingWarningTitle, s_pendingWarningText);
    }

    static NOTIFYICONDATAW CreateBaseData() => new()
    {
        cbSize = (uint)sizeof(NOTIFYICONDATAW),
        hWnd = s_window,
        uID = IconId,
    };

    static NOTIFYICONDATAW CreateStateData()
    {
        bool locked = CursorLocker.IsLocked;
        NOTIFYICONDATAW data = CreateBaseData();
        data.uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP | NIF_SHOWTIP;
        data.uCallbackMessage = CallbackMessage;
        data.hIcon = StatusIcons.Get(locked);
        CopyText(locked ? s_tooltipLocked : s_tooltipFree, data.szTip, TooltipCapacity);
        return data;
    }

    static void CopyText(string text, char* destination, int capacity)
    {
        int length = Math.Min(text.Length, capacity - 1);
        text.AsSpan(0, length).CopyTo(new Span<char>(destination, length));
        destination[length] = '\0';
    }
}
