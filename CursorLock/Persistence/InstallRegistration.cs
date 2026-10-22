namespace CursorLock.Persistence;

/// <summary>What the MSI records under HKCU\Software\CursorLock\Install.</summary>
internal static class InstallRegistration
{
    const string KeyPath = @"Software\CursorLock\Install";
    const string ExecutableValue = "Executable";

    /// <summary>True when this process is the copy installed by the MSI (not a portable copy).</summary>
    public static bool IsRunningInstalledCopy()
    {
        using RegistryKeyHandle key = RegistryKeyHandle.OpenForRead(KeyPath);
        string? installed = key.GetString(ExecutableValue);
        return installed != null && string.Equals(installed, Environment.ProcessPath, StringComparison.OrdinalIgnoreCase);
    }
}
