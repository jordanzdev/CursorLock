namespace CursorLock.Persistence;

/// <summary>"Start with Windows" through HKCU\Software\Microsoft\Windows\CurrentVersion\Run.</summary>
internal static class StartupRegistration
{
    const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ValueName = "CursorLock";

    static string? CurrentCommand =>
        Environment.ProcessPath is { Length: > 0 } path ? "\"" + path + "\"" : null;

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

    /// <summary>If startup is enabled but points to another path (the exe was moved), rewrites it.</summary>
    public static void RepairPath()
    {
        string? registered = ReadCommand();
        string? command = CurrentCommand;
        if (registered == null || command == null || string.Equals(registered, command, StringComparison.OrdinalIgnoreCase))
            return;

        using RegistryKeyHandle key = RegistryKeyHandle.OpenForWrite(RunKeyPath);
        key.SetString(ValueName, command);
    }

    /// <summary>Removes the startup entry if it points to this executable (used when uninstalling).</summary>
    public static void UnregisterCurrentExecutable()
    {
        string? registered = ReadCommand();
        if (registered == null || !string.Equals(registered, CurrentCommand, StringComparison.OrdinalIgnoreCase))
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
