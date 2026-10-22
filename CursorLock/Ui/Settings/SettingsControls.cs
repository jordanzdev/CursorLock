using static CursorLock.Native.Comctl32;
using static CursorLock.Native.User32;

namespace CursorLock.Ui.Settings;

/// <summary>Child controls of the settings window: ids, creation, DPI-aware layout and small accessors.</summary>
internal static unsafe class SettingsControls
{
    public const int ClientWidth = 372;
    public const int ClientHeight = 264;

    public const int HotkeyId = 100;
    public const int MonitorId = 101;
    public const int ReapplyIntervalId = 102;
    public const int StartWithWindowsId = 103;
    public const int SoundId = 104;
    public const int OsdId = 105;
    public const int LanguageId = 106;
    public const int CheckForUpdatesId = 107;
    public const int SaveId = IDOK;
    public const int CancelId = IDCANCEL;

    const int HotkeyLabelId = 200;
    const int MonitorLabelId = 201;
    const int ReapplyIntervalLabelId = 202;
    const int ReapplyRangeLabelId = 203;
    const int LanguageLabelId = 204;

    const int ReapplyIntervalMaxDigits = 4;
    const string ReapplyRangeText = "(100–2000)";

    // id, x, y, width, height at 96 DPI. Combo box heights include the drop-down list.
    static ReadOnlySpan<short> Layout =>
    [
        HotkeyLabelId, 12, 15, 124, 20,
        HotkeyId, 140, 12, 220, 23,
        MonitorLabelId, 12, 47, 124, 20,
        MonitorId, 140, 44, 220, 200,
        ReapplyIntervalLabelId, 12, 79, 124, 20,
        ReapplyIntervalId, 140, 76, 64, 23,
        ReapplyRangeLabelId, 210, 79, 150, 20,
        LanguageLabelId, 12, 111, 124, 20,
        LanguageId, 140, 108, 220, 200,
        StartWithWindowsId, 12, 144, 348, 20,
        SoundId, 12, 168, 348, 20,
        OsdId, 12, 192, 348, 20,
        CheckForUpdatesId, 12, 226, 160, 26,
        SaveId, 192, 226, 80, 26,
        CancelId, 280, 226, 80, 26,
    ];

    /// <summary>Creates every control. Creation order is the Tab order.</summary>
    public static void Create(nint window)
    {
        CreateLabel(window, HotkeyLabelId, Strings.LabelHotkey);
        CreateChild(window, HotkeyId, HOTKEY_CLASSW, null, WS_TABSTOP, WS_EX_CLIENTEDGE);
        CreateLabel(window, MonitorLabelId, Strings.LabelMonitor);
        CreateComboBox(window, MonitorId);
        CreateLabel(window, ReapplyIntervalLabelId, Strings.LabelReapplyInterval);
        CreateChild(window, ReapplyIntervalId, "EDIT", null, WS_TABSTOP | ES_NUMBER | ES_AUTOHSCROLL, WS_EX_CLIENTEDGE);
        CreateLabel(window, ReapplyRangeLabelId, ReapplyRangeText);
        CreateLabel(window, LanguageLabelId, Strings.LabelLanguage);
        CreateComboBox(window, LanguageId);
        CreateCheckBox(window, StartWithWindowsId, Strings.CheckStartWithWindows);
        CreateCheckBox(window, SoundId, Strings.CheckSound);
        CreateCheckBox(window, OsdId, Strings.CheckOsd);
        CreateChild(window, CheckForUpdatesId, "BUTTON", Strings.ButtonCheckForUpdates, WS_TABSTOP | BS_PUSHBUTTON, 0);
        CreateChild(window, SaveId, "BUTTON", Strings.ButtonSave, WS_TABSTOP | BS_DEFPUSHBUTTON, 0);
        CreateChild(window, CancelId, "BUTTON", Strings.ButtonCancel, WS_TABSTOP | BS_PUSHBUTTON, 0);

        SendMessageW(Get(window, ReapplyIntervalId), EM_SETLIMITTEXT, ReapplyIntervalMaxDigits, 0);
    }

    /// <summary>Applies <paramref name="font"/> and positions scaled to <paramref name="dpi"/>.</summary>
    public static void ApplyLayout(nint window, uint dpi, nint font)
    {
        ReadOnlySpan<short> layout = Layout;
        for (int i = 0; i + 4 < layout.Length; i += 5)
        {
            nint control = Get(window, layout[i]);
            if (font != 0)
                SendMessageW(control, WM_SETFONT, font, 1);
            SetWindowPos(control, 0,
                Dpi.Scale(layout[i + 1], dpi), Dpi.Scale(layout[i + 2], dpi),
                Dpi.Scale(layout[i + 3], dpi), Dpi.Scale(layout[i + 4], dpi),
                SWP_NOZORDER | SWP_NOACTIVATE);
        }
    }

    public static nint Get(nint window, int id) => GetDlgItem(window, id);

    public static void Focus(nint window, int id) => SetFocus(Get(window, id));

    public static void FocusAndSelectAll(nint window, int id)
    {
        nint control = Get(window, id);
        SetFocus(control);
        SendMessageW(control, EM_SETSEL, 0, -1);
    }

    public static bool IsChecked(nint window, int id) =>
        SendMessageW(Get(window, id), BM_GETCHECK, 0, 0) == BST_CHECKED;

    public static void SetChecked(nint window, int id, bool isChecked) =>
        SendMessageW(Get(window, id), BM_SETCHECK, isChecked ? BST_CHECKED : BST_UNCHECKED, 0);

    public static bool TryGetNumber(nint window, int id, out uint value)
    {
        int translated;
        value = GetDlgItemInt(window, id, &translated, false);
        return translated != 0;
    }

    public static void SetNumber(nint window, int id, uint value) => SetDlgItemInt(window, id, value, false);

    public static void AddComboItem(nint window, int id, string text)
    {
        fixed (char* chars = text)
            SendMessageW(Get(window, id), CB_ADDSTRING, 0, (nint)chars);
    }

    public static int GetComboSelection(nint window, int id) => (int)SendMessageW(Get(window, id), CB_GETCURSEL, 0, 0);

    public static void SetComboSelection(nint window, int id, int index) => SendMessageW(Get(window, id), CB_SETCURSEL, index, 0);

    static void CreateLabel(nint window, int id, string text) => CreateChild(window, id, "STATIC", text, SS_LEFT, 0);

    static void CreateCheckBox(nint window, int id, string text) =>
        CreateChild(window, id, "BUTTON", text, WS_TABSTOP | BS_AUTOCHECKBOX, 0);

    static void CreateComboBox(nint window, int id) =>
        CreateChild(window, id, "COMBOBOX", null, WS_TABSTOP | WS_VSCROLL | CBS_DROPDOWNLIST, 0);

    static void CreateChild(nint window, int id, string className, string? text, uint style, uint exStyle) =>
        CreateWindowExW(exStyle, className, text, WS_CHILD | WS_VISIBLE | style, 0, 0, 0, 0, window, id, AppInstance.Handle, 0);
}
