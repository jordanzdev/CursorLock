using CursorLock.Core;
using CursorLock.Native;
using CursorLock.Persistence;
using CursorLock.Ui;
using CursorLock.Ui.Settings;
using static CursorLock.Native.User32;

namespace CursorLock;

internal enum ApplySettingsResult
{
    Applied,
    AppliedButNotPersisted,
    HotkeyUnavailable,
}

/// <summary>Application lifetime and the use cases that span several components.</summary>
internal static unsafe class App
{
    static bool s_isShutDown;

    public static int Run()
    {
        bool isFirstRun = !ConfigStore.HasSavedSettings();
        Config.Current = ConfigStore.Load();
        Strings.Apply(Config.Current.Language);
        StartupRegistration.RepairPath();

        if (!MainWindow.Create())
            return 1;

        nint window = MainWindow.Handle;
        CursorLocker.Initialize(window);
        CursorLocker.StateChanged += LockFeedback.OnLockStateChanged;
        GlobalHotkey.Initialize(window);
        TrayIcon.Create(window);

        if (!GlobalHotkey.Register())
            TrayIcon.ShowWarning(Strings.HotkeyUnavailableTitle, Strings.HotkeyUnavailableText(HotkeyText.Format(Config.Current.Hotkey)));

        if (isFirstRun)
            Welcome();

        RunMessageLoop();
        Shutdown();
        return 0;
    }

    public static void Exit() => MainWindow.Close();

    /// <summary>
    /// Run by the MSI on uninstall (not on upgrade): removes what the app itself wrote,
    /// which Windows Installer does not know about.
    /// </summary>
    public static int RemoveUserData()
    {
        StartupRegistration.UnregisterCurrentExecutable();
        return ConfigStore.Delete() ? 0 : 1;
    }

    public static void OnTrayEvent(TrayEvent trayEvent, POINT anchor)
    {
        switch (trayEvent)
        {
            case TrayEvent.ContextMenuRequested:
                Execute(TrayMenu.Show(MainWindow.Handle, anchor, CursorLocker.IsLocked, Config.Current.Hotkey));
                break;
            case TrayEvent.Activated:
                SettingsWindow.Show();
                break;
        }
    }

    /// <summary>Applies new settings without restarting. The hotkey is checked first, so a busy one changes nothing.</summary>
    public static ApplySettingsResult ApplySettings(in Config next, bool startWithWindows, out int hotkeyError)
    {
        hotkeyError = 0;
        Config previous = Config.Current;
        if (next.Hotkey != previous.Hotkey && !GlobalHotkey.TryReplace(next.Hotkey, out hotkeyError))
            return ApplySettingsResult.HotkeyUnavailable;

        Config.Current = next;
        bool isPersisted = ConfigStore.Save(next);
        isPersisted &= StartupRegistration.TrySet(startWithWindows);

        Strings.Apply(next.Language);
        TrayIcon.RefreshTooltips();
        CursorLocker.OnReapplyIntervalChanged();
        if (!next.OsdEnabled || next.Language != previous.Language)
            OsdWindow.Destroy(); // recreated on demand, measured for the new language

        return isPersisted ? ApplySettingsResult.Applied : ApplySettingsResult.AppliedButNotPersisted;
    }

    /// <summary>Releases the cursor first, then every resource. Idempotent.</summary>
    public static void Shutdown()
    {
        if (s_isShutDown)
            return;
        s_isShutDown = true;

        CursorLocker.Release();
        GlobalHotkey.Shutdown();
        SettingsWindow.Close();
        OsdWindow.Destroy();
        TrayIcon.Remove();
        StatusIcons.Destroy();
    }

    /// <summary>
    /// First run: show the settings instead of starting silently in the tray, so the user sees
    /// the app and its hotkey. The defaults are saved right away, so this happens only once.
    /// </summary>
    static void Welcome()
    {
        ConfigStore.Save(Config.Current);
        SettingsWindow.Show();
    }

    static void Execute(TrayCommand command)
    {
        switch (command)
        {
            case TrayCommand.ToggleLock:
                CursorLocker.Toggle();
                break;
            case TrayCommand.OpenSettings:
                SettingsWindow.Show();
                break;
            case TrayCommand.Exit:
                Exit();
                break;
        }
    }

    /// <summary>The only thread sleeps in GetMessageW: 0 % CPU while idle. No allocations per message.</summary>
    static void RunMessageLoop()
    {
        MSG message;
        while (GetMessageW(&message, 0, 0, 0) > 0)
        {
            nint settings = SettingsWindow.Handle;
            if (settings != 0 && IsDialogMessageW(settings, &message))
                continue; // Tab, Enter and Esc navigation in the settings window
            TranslateMessage(&message);
            DispatchMessageW(&message);
        }
    }
}
