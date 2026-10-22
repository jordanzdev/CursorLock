using CursorLock.Core;
using CursorLock.Native;
using static CursorLock.Native.User32;

namespace CursorLock.Ui;

internal enum TrayCommand
{
    None = 0,
    ToggleLock = 1,
    OpenSettings = 2,
    Exit = 3,
}

/// <summary>Context menu of the tray icon. It only presents the choice; the caller executes it.</summary>
internal static class TrayMenu
{
    public static TrayCommand Show(nint owner, POINT anchor, bool isLocked, Hotkey hotkey)
    {
        nint menu = CreatePopupMenu();
        if (menu == 0)
            return TrayCommand.None;

        try
        {
            string toggleText = (isLocked ? Strings.MenuUnlock : Strings.MenuLock) + "\t" + HotkeyText.Format(hotkey);
            AppendMenuW(menu, MF_STRING, (nuint)TrayCommand.ToggleLock, toggleText);
            AppendMenuW(menu, MF_STRING, (nuint)TrayCommand.OpenSettings, Strings.MenuSettings);
            AppendMenuW(menu, MF_SEPARATOR, 0, null);
            AppendMenuW(menu, MF_STRING, (nuint)TrayCommand.Exit, Strings.MenuExit);
            SetMenuDefaultItem(menu, (uint)TrayCommand.OpenSettings, 0);

            // Without these two calls the menu does not close when clicking elsewhere.
            SetForegroundWindow(owner);
            int command = TrackPopupMenu(menu, TPM_RIGHTBUTTON | TPM_RETURNCMD | TPM_NONOTIFY, anchor.X, anchor.Y, 0, owner, 0);
            PostMessageW(owner, WM_NULL, 0, 0);
            return (TrayCommand)command;
        }
        finally
        {
            DestroyMenu(menu);
        }
    }
}
