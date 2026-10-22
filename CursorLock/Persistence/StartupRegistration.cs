namespace CursorLock.Persistence;

/// <summary>"Start with Windows" through HKCU\Software\Microsoft\Windows\CurrentVersion\Run.</summary>
internal static class StartupRegistration
{
    /// <summary>Added to the startup entry, so a launch at sign-in starts silently in the tray.</summary>
    public const string StartupArgument = "--startup";

    const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ValueName = "CursorLock";

    static string? QuotedExecutablePath =>
        Environment.ProcessPath is { Length: > 0 } path ? "\"" + path + "\"" : null;

    static string? CurrentCommand =>
        QuotedExecutablePath is { } executable ? executable + " " + StartupArgument : null;

    public static bool IsEnabled() => ReadCommand() != null;

    /// <summary>Enables or disables startup. Does nothing when it is already in the requested state.</summary>
    public static bool TrySet(bool enabled)
    {
        if (IsEnabled() == enabled)
            return true;

        using RegistryKeyHandle key = RegistryKeyHandle.OpenForWrite(RunKeyPath);
        if (!enabled)
            return key.DeleteValue(ValueName);

        string? command = CurrentCommand;
        return command != null && key.SetString(ValueName, command);
    }

    /// <summary>
    /// If startup is enabled but its command differs from the current one (the exe was moved,
    /// or the entry was written by an older version without <see cref="StartupArgument"/>), rewrites it.
    /// </summary>
    public static void RepairCommand()
    {
        string? registered = ReadCommand();
        string? command = CurrentCommand;
        if (registered == null || command == null || string.Equals(registered, command, StringComparison.OrdinalIgnoreCase))
            return;

        using RegistryKeyHandle key = RegistryKeyHandle.OpenForWrite(RunKeyPath);
        key.SetString(ValueName, command);
    }

    /// <summary>Removes the startup entry if it launches this executable (used when uninstalling).</summary>
    public static void UnregisterCurrentExecutable()
    {
        string? registered = ReadCommand();
        string? executable = QuotedExecutablePath;
        if (registered == null || executable == null || !registered.StartsWith(executable, StringComparison.OrdinalIgnoreCase))
            return;

        using RegistryKeyHandle key = RegistryKeyHandle.OpenForWrite(RunKeyPath);
        key.DeleteValue(ValueName);
    }

    static string? ReadCommand()
    {
        using RegistryKeyHandle key = RegistryKeyHandle.OpenForRead(RunKeyPath);
        return key.GetString(ValueName);
    }
}
