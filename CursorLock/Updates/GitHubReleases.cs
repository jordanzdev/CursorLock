using CursorLock.Core;

namespace CursorLock.Updates;

/// <summary>
/// Release information from github.com. github.com/{repo}/releases/latest redirects to
/// .../releases/tag/v{version}, so the latest version is read from that redirect: no API calls,
/// no JSON and no rate limits.
/// </summary>
internal static class GitHubReleases
{
    const string Repository = "jordanzdev/CursorLock";
    const string ReleasesUrl = "https://github.com/" + Repository + "/releases";
    const string TagPathMarker = "/releases/tag/v";
    const int MaxDownloadBytes = 32 * 1024 * 1024;

    public const string LatestReleasePageUrl = ReleasesUrl + "/latest";

    public static bool TryGetLatestVersion(out Version latest)
    {
        latest = new Version(0, 0, 0);
        bool sent = HttpsRequest.TrySend(new Uri(LatestReleasePageUrl), followRedirects: false, out HttpsRequest response);
        using (response)
        {
            string? location = sent && response.IsRedirect ? response.GetLocationHeader() : null;
            int tag = location?.LastIndexOf(TagPathMarker, StringComparison.Ordinal) ?? -1;
            if (tag < 0 || !Version.TryParse(location![(tag + TagPathMarker.Length)..], out Version? version))
                return false;

            latest = AppVersion.Normalize(version);
            return true;
        }
    }

    /// <summary>Downloads a file attached to the release of <paramref name="version"/>.</summary>
    public static bool TryDownloadAsset(Version version, string fileName, out byte[] content)
    {
        content = [];
        var url = new Uri($"{ReleasesUrl}/download/v{AppVersion.ToText(version)}/{fileName}");
        bool sent = HttpsRequest.TrySend(url, followRedirects: true, out HttpsRequest response);
        using (response)
            return sent && response.IsSuccess && response.TryReadBody(MaxDownloadBytes, out content);
    }

    public static string InstallerFileName(Version version, string culture) =>
        $"CursorLock-{AppVersion.ToText(version)}-{culture}.msi";
}
