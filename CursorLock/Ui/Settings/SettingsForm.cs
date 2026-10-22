using CursorLock.Core;
using static CursorLock.Ui.Settings.SettingsControls;

namespace CursorLock.Ui.Settings;

internal enum SettingsFormError
{
    None,
    InvalidHotkey,
    InvalidReapplyInterval,
}

/// <summary>Moves values between <see cref="Config"/> and the settings controls, and validates the input.</summary>
internal static class SettingsForm
{
    const int UnderCursorIndex = 0;
    const int PrimaryIndex = 1;
    const int FirstMonitorIndex = 2;

    static List<string> s_monitorDevices = []; // combo index - FirstMonitorIndex → device name

    public static void Populate(nint window, in Config config, bool startWithWindows)
    {
        HotkeyControl.Initialize(Get(window, HotkeyId), config.Hotkey);
        PopulateMonitors(window, config);
        PopulateLanguages(window, config.Language);
        SetNumber(window, ReapplyIntervalId, config.ReapplyIntervalMs);
        SetChecked(window, StartWithWindowsId, startWithWindows);
        SetChecked(window, SoundId, config.SoundEnabled);
        SetChecked(window, OsdId, config.OsdEnabled);
    }

    /// <summary>Reads and validates the form. <paramref name="next"/> starts as a copy of <paramref name="current"/>.</summary>
    public static SettingsFormError TryRead(nint window, in Config current, out Config next, out bool startWithWindows)
    {
        next = current;
        startWithWindows = IsChecked(window, StartWithWindowsId);

        Hotkey hotkey = HotkeyControl.Read(Get(window, HotkeyId));
        if (!hotkey.IsValid)
            return SettingsFormError.InvalidHotkey;

        if (!TryGetNumber(window, ReapplyIntervalId, out uint interval) || !Config.IsValidReapplyInterval(interval))
            return SettingsFormError.InvalidReapplyInterval;

        next.Hotkey = hotkey;
        next.ReapplyIntervalMs = interval;
        next.SoundEnabled = IsChecked(window, SoundId);
        next.OsdEnabled = IsChecked(window, OsdId);
        next.Language = ReadLanguage(window);
        ReadTarget(window, ref next);
        return SettingsFormError.None;
    }

    public static void Clear() => s_monitorDevices = [];

    static void PopulateMonitors(nint window, in Config config)
    {
        AddComboItem(window, MonitorId, Strings.MonitorUnderCursor);
        AddComboItem(window, MonitorId, Strings.MonitorPrimary);

        s_monitorDevices = [];
        foreach (MonitorEntry monitor in Monitors.ListAll())
        {
            s_monitorDevices.Add(monitor.Device);
            AddComboItem(window, MonitorId, $"{Monitors.ShortName(monitor.Device)} — {monitor.Width}×{monitor.Height}");
        }

        SetComboSelection(window, MonitorId, config.TargetMode switch
        {
            TargetMode.Primary => PrimaryIndex,
            TargetMode.Specific => FirstMonitorIndex + IndexOfDevice(window, config.TargetDevice),
            _ => UnderCursorIndex,
        });
    }

    /// <summary>Index of the device in the list; a saved but disconnected monitor is added so it is not lost on save.</summary>
    static int IndexOfDevice(nint window, string device)
    {
        int index = s_monitorDevices.FindIndex(known => string.Equals(known, device, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
            return index;

        s_monitorDevices.Add(device);
        AddComboItem(window, MonitorId, $"{Monitors.ShortName(device)} — {Strings.MonitorDisconnected}");
        return s_monitorDevices.Count - 1;
    }

    static void PopulateLanguages(nint window, UiLanguage language)
    {
        // The combo index matches the UiLanguage value.
        AddComboItem(window, LanguageId, Strings.LanguageAuto);
        AddComboItem(window, LanguageId, Strings.LanguageSpanish);
        AddComboItem(window, LanguageId, Strings.LanguageEnglish);
        SetComboSelection(window, LanguageId, (int)language);
    }

    static UiLanguage ReadLanguage(nint window)
    {
        int index = GetComboSelection(window, LanguageId);
        return index is >= (int)UiLanguage.Auto and <= (int)UiLanguage.English ? (UiLanguage)index : UiLanguage.Auto;
    }

    static void ReadTarget(nint window, ref Config config)
    {
        int index = GetComboSelection(window, MonitorId);
        int deviceIndex = index - FirstMonitorIndex;
        if (deviceIndex >= 0 && deviceIndex < s_monitorDevices.Count)
        {
            config.TargetMode = TargetMode.Specific;
            config.TargetDevice = s_monitorDevices[deviceIndex];
            return;
        }

        config.TargetMode = index == PrimaryIndex ? TargetMode.Primary : TargetMode.UnderCursor;
        config.TargetDevice = "";
    }
}
