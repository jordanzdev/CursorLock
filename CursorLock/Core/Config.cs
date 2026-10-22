namespace CursorLock.Core;

internal enum TargetMode : uint
{
    UnderCursor = 0,
    Primary = 1,
    Specific = 2,
}

/// <summary>User settings. <see cref="Current"/> is the single source of truth while the app runs.</summary>
internal struct Config
{
    public const uint DefaultReapplyIntervalMs = 250;
    public const uint MinReapplyIntervalMs = 100;
    public const uint MaxReapplyIntervalMs = 2000;
    const int MaxDeviceNameLength = 31; // MONITORINFOEXW.szDevice holds 32 chars including the null

    public Hotkey Hotkey;
    public TargetMode TargetMode;
    public string TargetDevice; // e.g. \\.\DISPLAY2, only used with TargetMode.Specific
    public uint ReapplyIntervalMs;
    public bool SoundEnabled;
    public bool OsdEnabled;
    public UiLanguage Language;

    public static Config Current = Default;

    public static Config Default => new()
    {
        Hotkey = Hotkey.Default,
        TargetMode = TargetMode.UnderCursor,
        TargetDevice = "",
        ReapplyIntervalMs = DefaultReapplyIntervalMs,
        SoundEnabled = true,
        OsdEnabled = false,
        Language = UiLanguage.Auto,
    };

    public static bool IsValidReapplyInterval(uint milliseconds) =>
        milliseconds is >= MinReapplyIntervalMs and <= MaxReapplyIntervalMs;

    public static bool IsValidDeviceName(string? device) =>
        device is { Length: > 0 and <= MaxDeviceNameLength };
}
