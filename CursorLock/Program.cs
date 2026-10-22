using CursorLock.Core;

namespace CursorLock;

internal static class Program
{
    const string InstanceMutexName = @"Local\CursorLock";
    const string RemoveUserDataArgument = "--uninstall"; // passed by the MSI uninstaller

    [STAThread]
    static int Main(string[] args)
    {
        if (args is [RemoveUserDataArgument])
            return App.RemoveUserData();

        using SingleInstance instance = SingleInstance.TryAcquire(InstanceMutexName);
        if (!instance.IsAcquired)
            return 0; // another instance is already running: exit silently

        // Whatever happens, never leave the cursor trapped.
        AppDomain.CurrentDomain.UnhandledException += static (_, _) => CursorLocker.ReleaseClip();
        try
        {
            return App.Run();
        }
        finally
        {
            CursorLocker.ReleaseClip();
            App.Shutdown();
        }
    }
}
