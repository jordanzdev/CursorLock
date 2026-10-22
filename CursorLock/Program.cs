using CursorLock.Core;
using CursorLock.Persistence;

namespace CursorLock;

internal static class Program
{
    const string InstanceMutexName = @"Local\CursorLock";
    // Passed by the MSI; each one does its job and exits without starting the app.
    const string RemoveUserDataArgument = "--uninstall";
    const string CreateDesktopShortcutArgument = "--create-desktop-shortcut";

    [STAThread]
    static int Main(string[] args)
    {
        if (args is [RemoveUserDataArgument])
            return App.RemoveUserData();
        if (args is [CreateDesktopShortcutArgument])
            return App.CreateDesktopShortcut();

        using SingleInstance instance = SingleInstance.TryAcquire(InstanceMutexName);
        if (!instance.IsAcquired)
            return 0; // another instance is already running: exit silently

        // Whatever happens, never leave the cursor trapped.
        AppDomain.CurrentDomain.UnhandledException += static (_, _) => CursorLocker.ReleaseClip();
        try
        {
            bool startedWithWindows = args is [StartupRegistration.StartupArgument];
            return App.Run(showSettings: !startedWithWindows);
        }
        finally
        {
            CursorLocker.ReleaseClip();
            App.Shutdown();
        }
    }
}
