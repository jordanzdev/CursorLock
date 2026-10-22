using CursorLock.Core;
using CursorLock.Persistence;
using CursorLock.Updates;
using static CursorLock.Native.Shell32;
using static CursorLock.Native.User32;

namespace CursorLock.Ui.Settings;

/// <summary>
/// "Check for updates". This is the only code that uses the network or starts another process,
/// and it runs only when the user clicks the button: never automatically, never in the background.
/// It runs on the UI thread and blocks it for the second or two the requests take.
/// </summary>
internal static class UpdateCheck
{
    const int OpenSucceededAbove = 32; // ShellExecuteW returns a value greater than 32 on success

    public static void Run(nint owner, Action<string> reportProgress)
    {
        reportProgress(Strings.CheckingForUpdates);
        if (!GitHubReleases.TryGetLatestVersion(out Version latest))
        {
            Show(owner, Strings.UpdateCheckFailed, MB_ICONWARNING);
            return;
        }

        string current = AppVersion.CurrentText, available = AppVersion.ToText(latest);
        if (latest <= AppVersion.Current)
        {
            Show(owner, Strings.UpToDate(current), MB_ICONINFORMATION);
            return;
        }

        if (!InstallRegistration.IsRunningInstalledCopy())
        {
            // A portable copy is not managed by the MSI: let the user download it.
            if (Ask(owner, Strings.UpdateAvailablePortable(available, current)))
                Open(owner, GitHubReleases.LatestReleasePageUrl);
            return;
        }

        if (!Ask(owner, Strings.UpdateAvailable(available, current)))
            return;

        reportProgress(Strings.DownloadingUpdate);
        switch (UpdateDownloader.TryDownloadInstaller(latest, out string installerPath))
        {
            case DownloadResult.NetworkError:
                Show(owner, Strings.UpdateDownloadFailed, MB_ICONWARNING);
                return;
            case DownloadResult.IntegrityError:
                Show(owner, Strings.UpdateIntegrityFailed, MB_ICONWARNING);
                return;
        }

        if (!Open(owner, installerPath))
        {
            Show(owner, Strings.UpdateLaunchFailed, MB_ICONWARNING);
            return;
        }

        // The installer replaces the exe and offers to reopen CursorLock on its last page.
        App.Exit();
    }

    static bool Open(nint owner, string target) =>
        ShellExecuteW(owner, "open", target, null, null, SW_SHOWNORMAL) > OpenSucceededAbove;

    static bool Ask(nint owner, string question) =>
        MessageBoxW(owner, question, "CursorLock", MB_YESNO | MB_ICONQUESTION) == IDYES;

    static void Show(nint owner, string text, uint icon) =>
        MessageBoxW(owner, text, "CursorLock", MB_OK | icon);
}
