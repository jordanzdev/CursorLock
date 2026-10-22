using CursorLock.Core;

namespace CursorLock.Persistence;

/// <summary>
/// Loads and saves <see cref="Config"/> under HKCU\Software\CursorLock.
/// Missing or invalid values fall back to their defaults.
/// </summary>
internal static class ConfigStore
{
    const string KeyPath = @"Software\CursorLock";

    const string HotkeyModifiersValue = "HotkeyModifiers";
    const string HotkeyVirtualKeyValue = "HotkeyVk";
    const string TargetModeValue = "TargetMode";
    const string TargetDeviceValue = "TargetDevice";
    const string ReapplyIntervalValue = "ReapplyMs";
    const string SoundValue = "Sound";
    const string OsdValue = "Osd";
    const string LanguageValue = "Language";

    /// <summary>
    /// False until the settings are saved for the first time. All values are written together,
    /// so the hotkey value tells whether they ever were. (The key alone is not enough: the MSI
    /// creates its own Install subkey under it.)
    /// </summary>
    public static bool HasSavedSettings()
    {
        using RegistryKeyHandle key = RegistryKeyHandle.OpenForRead(KeyPath);
        return key.TryGetDword(HotkeyVirtualKeyValue, out _);
    }

    public static Config Load()
    {
        Config config = Config.Default;
        using RegistryKeyHandle key = RegistryKeyHandle.OpenForRead(KeyPath);
        if (!key.IsOpen)
            return config;

        config.Hotkey = ReadHotkey(key, config.Hotkey);
        ReadTarget(key, ref config);
        config.ReapplyIntervalMs = ReadDword(key, ReapplyIntervalValue, Config.IsValidReapplyInterval, config.ReapplyIntervalMs);
        config.SoundEnabled = ReadFlag(key, SoundValue, config.SoundEnabled);
        config.OsdEnabled = ReadFlag(key, OsdValue, config.OsdEnabled);
        config.Language = (UiLanguage)ReadDword(key, LanguageValue, IsDefinedLanguage, (uint)config.Language);
        return config;
    }

    public static bool Save(in Config config)
    {
        using RegistryKeyHandle key = RegistryKeyHandle.OpenForWrite(KeyPath);
        if (!key.IsOpen)
            return false;

        bool saved = key.SetDword(HotkeyModifiersValue, config.Hotkey.Modifiers);
        saved &= key.SetDword(HotkeyVirtualKeyValue, config.Hotkey.VirtualKey);
        saved &= key.SetDword(TargetModeValue, (uint)config.TargetMode);
        saved &= key.SetString(TargetDeviceValue, config.TargetDevice);
        saved &= key.SetDword(ReapplyIntervalValue, config.ReapplyIntervalMs);
        saved &= key.SetDword(SoundValue, ToDword(config.SoundEnabled));
        saved &= key.SetDword(OsdValue, ToDword(config.OsdEnabled));
        saved &= key.SetDword(LanguageValue, (uint)config.Language);
        return saved;
    }

    /// <summary>Removes every stored setting (used when uninstalling).</summary>
    public static bool Delete() => RegistryKeyHandle.DeleteTree(KeyPath);

    static Hotkey ReadHotkey(in RegistryKeyHandle key, Hotkey fallback)
    {
        if (!key.TryGetDword(HotkeyModifiersValue, out uint modifiers) ||
            !key.TryGetDword(HotkeyVirtualKeyValue, out uint virtualKey))
            return fallback;

        var hotkey = new Hotkey(modifiers, virtualKey);
        return hotkey.IsValid ? hotkey : fallback;
    }

    static void ReadTarget(in RegistryKeyHandle key, ref Config config)
    {
        config.TargetMode = (TargetMode)ReadDword(key, TargetModeValue, IsDefinedTargetMode, (uint)config.TargetMode);

        string? device = key.GetString(TargetDeviceValue);
        if (Config.IsValidDeviceName(device))
            config.TargetDevice = device!;

        if (config.TargetMode == TargetMode.Specific && config.TargetDevice.Length == 0)
            config.TargetMode = TargetMode.UnderCursor;
    }

    static uint ReadDword(in RegistryKeyHandle key, string name, Func<uint, bool> isValid, uint fallback) =>
        key.TryGetDword(name, out uint value) && isValid(value) ? value : fallback;

    static bool ReadFlag(in RegistryKeyHandle key, string name, bool fallback) =>
        ReadDword(key, name, static value => value <= 1, ToDword(fallback)) != 0;

    static bool IsDefinedTargetMode(uint value) => value <= (uint)TargetMode.Specific;

    static bool IsDefinedLanguage(uint value) => value <= (uint)UiLanguage.English;

    static uint ToDword(bool value) => value ? 1u : 0u;
}
