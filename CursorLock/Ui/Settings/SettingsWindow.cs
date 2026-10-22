using System.Runtime.InteropServices;
using CursorLock.Core;
using CursorLock.Native;
using CursorLock.Persistence;
using static CursorLock.Native.Comctl32;
using static CursorLock.Native.User32;

namespace CursorLock.Ui.Settings;

/// <summary>
/// Plain Win32 settings window. Created when opened and destroyed when closed, so no controls stay
/// in memory. While it is active the global hotkey is suspended, so it can be typed into the control.
/// </summary>
internal static unsafe class SettingsWindow
{
    const string ClassName = "CursorLockSettings";
    const uint Style = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU;
    const uint ExStyle = WS_EX_DLGMODALFRAME | WS_EX_CONTROLPARENT;
    const int InitialSize = 100;

    static bool s_isClassRegistered;
    static bool s_areCommonControlsReady;
    static nint s_window;
    static nint s_font;
    static nint s_lastFocus;
    static WindowIcons s_icons;

    /// <summary>Handle of the open window, or 0. The message loop passes it to IsDialogMessageW.</summary>
    public static nint Handle => s_window;

    public static void Show()
    {
        if (s_window != 0)
        {
            ShowWindow(s_window, SW_SHOWNORMAL);
            SetForegroundWindow(s_window);
            return;
        }
        Create();
    }

    /// <summary>Closes without asking (used on exit).</summary>
    public static void Close()
    {
        if (s_window != 0)
            DestroyWindow(s_window);
    }

    static void Create()
    {
        EnsureCommonControls();
        s_isClassRegistered = s_isClassRegistered || WindowClass.Register(ClassName, &WindowProc, COLOR_BTNFACE + 1);
        if (!s_isClassRegistered)
            return;

        // Created on the cursor's monitor, so GetDpiForWindow returns that monitor's DPI.
        MONITORINFOEXW monitor;
        Monitors.TryGetUnderCursor(&monitor, out _);
        RECT workArea = monitor.rcWork;

        nint window = CreateWindowExW(ExStyle, ClassName, Strings.SettingsTitle, Style,
            workArea.Left, workArea.Top, InitialSize, InitialSize, 0, 0, AppInstance.Handle, 0);
        if (window == 0)
            return;

        s_window = window;
        s_lastFocus = 0;
        uint dpi = GetDpiForWindow(window);
        CenterInWorkArea(window, workArea, dpi);
        SettingsControls.Create(window);
        ApplyDpi(window, dpi);
        SettingsForm.Populate(window, Config.Current, StartupRegistration.IsEnabled());

        ShowWindow(window, SW_SHOWNORMAL);
        SetForegroundWindow(window);
        SettingsControls.Focus(window, SettingsControls.HotkeyId);
    }

    static void EnsureCommonControls()
    {
        if (s_areCommonControlsReady)
            return;
        var controls = new INITCOMMONCONTROLSEX
        {
            dwSize = (uint)sizeof(INITCOMMONCONTROLSEX),
            dwICC = ICC_HOTKEY_CLASS | ICC_STANDARD_CLASSES,
        };
        s_areCommonControlsReady = InitCommonControlsEx(&controls);
    }

    static void CenterInWorkArea(nint window, in RECT workArea, uint dpi)
    {
        RECT frame = new()
        {
            Right = Dpi.Scale(SettingsControls.ClientWidth, dpi),
            Bottom = Dpi.Scale(SettingsControls.ClientHeight, dpi),
        };
        AdjustWindowRectExForDpi(&frame, Style, false, ExStyle, dpi);
        SetWindowPos(window, 0,
            workArea.Left + (workArea.Width - frame.Width) / 2,
            workArea.Top + (workArea.Height - frame.Height) / 2,
            frame.Width, frame.Height, SWP_NOZORDER | SWP_NOACTIVATE);
    }

    /// <summary>Icons, system font and control positions for <paramref name="dpi"/>. Also runs on WM_DPICHANGED.</summary>
    static void ApplyDpi(nint window, uint dpi)
    {
        s_icons.Apply(window, dpi);
        nint previousFont = s_font;
        s_font = SystemFont.CreateMessageFont(dpi);
        SettingsControls.ApplyLayout(window, dpi, s_font);
        if (previousFont != 0)
            Gdi32.DeleteObject(previousFont);
    }

    static void Save(nint window)
    {
        switch (SettingsForm.TryRead(window, Config.Current, out Config next, out bool startWithWindows))
        {
            case SettingsFormError.InvalidHotkey:
                ShowWarning(window, Strings.ErrorInvalidHotkey);
                SettingsControls.Focus(window, SettingsControls.HotkeyId);
                return;
            case SettingsFormError.InvalidReapplyInterval:
                ShowWarning(window, Strings.ErrorInvalidReapplyInterval);
                SettingsControls.FocusAndSelectAll(window, SettingsControls.ReapplyIntervalId);
                return;
        }

        Hotkey previousHotkey = Config.Current.Hotkey;
        switch (App.ApplySettings(next, startWithWindows, out int hotkeyError))
        {
            case ApplySettingsResult.HotkeyUnavailable:
                ShowWarning(window, Strings.ErrorHotkeyUnavailable(
                    HotkeyText.Format(next.Hotkey), HotkeyText.Format(previousHotkey), hotkeyError));
                SettingsControls.Focus(window, SettingsControls.HotkeyId);
                return;
            case ApplySettingsResult.AppliedButNotPersisted:
                ShowWarning(window, Strings.ErrorNotPersisted);
                break;
        }
        DestroyWindow(window);
    }

    static void ConfirmClose(nint window)
    {
        switch (CloseConfirmation.Ask(window))
        {
            case CloseChoice.MinimizeToTray:
                DestroyWindow(window); // the app keeps running in the tray
                break;
            case CloseChoice.ExitApplication:
                App.Exit();
                break;
        }
    }

    /// <summary>
    /// This is not a real dialog, so focus is saved and restored by hand. The global hotkey is
    /// suspended while the window is active and resumed when it is not.
    /// </summary>
    static bool OnActivate(bool isActive)
    {
        if (!isActive)
        {
            s_lastFocus = GetFocus();
            GlobalHotkey.Resume();
            return true;
        }

        GlobalHotkey.Suspend();
        if (s_lastFocus == 0)
            return false;
        SetFocus(s_lastFocus);
        return true;
    }

    static void OnDpiChanged(nint window, uint dpi, RECT* suggestedBounds)
    {
        SetWindowPos(window, 0, suggestedBounds->Left, suggestedBounds->Top,
            suggestedBounds->Width, suggestedBounds->Height, SWP_NOZORDER | SWP_NOACTIVATE);
        ApplyDpi(window, dpi);
    }

    /// <summary>The window and its controls are gone by WM_NCDESTROY, so the icons and the font can be freed.</summary>
    static void OnFinalDestroy()
    {
        s_window = 0;
        s_lastFocus = 0;
        s_icons.Release();
        SettingsForm.Clear();
        if (s_font != 0)
            Gdi32.DeleteObject(s_font);
        s_font = 0;
        GlobalHotkey.Resume();
    }

    static void ShowWarning(nint window, string text) =>
        MessageBoxW(window, text, "CursorLock", MB_OK | MB_ICONWARNING);

    [UnmanagedCallersOnly]
    static nint WindowProc(nint window, uint message, nint wParam, nint lParam)
    {
        try
        {
            switch (message)
            {
                case WM_ACTIVATE:
                    if (OnActivate(((int)wParam & 0xFFFF) != WA_INACTIVE))
                        return 0;
                    break;

                case WM_COMMAND:
                    int id = (int)wParam & 0xFFFF;
                    int notification = ((int)wParam >> 16) & 0xFFFF;
                    if (notification == BN_CLICKED && id == SettingsControls.SaveId)
                        Save(window);
                    else if (notification == BN_CLICKED && id == SettingsControls.CancelId)
                        DestroyWindow(window); // Cancel and Esc close without asking
                    return 0;

                case DM_GETDEFID:
                    // IsDialogMessageW asks for the default button when Enter is pressed.
                    return (DC_HASDEFID << 16) | SettingsControls.SaveId;

                case WM_DPICHANGED:
                    OnDpiChanged(window, (uint)wParam & 0xFFFF, (RECT*)lParam);
                    return 0;

                case WM_CLOSE:
                    ConfirmClose(window); // X button, Alt+F4 or system menu
                    return 0;

                case WM_NCDESTROY:
                    OnFinalDestroy();
                    break;
            }
            return DefWindowProcW(window, message, wParam, lParam);
        }
        catch
        {
            return 0; // an exception must never cross into native code
        }
    }
}
