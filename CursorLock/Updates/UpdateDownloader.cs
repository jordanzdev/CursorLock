using System.Text;
using CursorLock.Native;

namespace CursorLock.Updates;

internal enum DownloadResult
{
    Ready,
    NetworkError,
    IntegrityError,
}

/// <summary>
/// Downloads the MSI of a release and checks it against the release's SHA256SUMS.txt before it
/// is ever run. The file is saved under %TEMP%\CursorLock-update. The installer itself asks for
/// the language, preselecting the one CursorLock uses.
/// </summary>
internal static unsafe class UpdateDownloader
{
    const string ChecksumsFileName = "SHA256SUMS.txt";
    const string DownloadFolderName = "CursorLock-update";

    public static DownloadResult TryDownloadInstaller(Version version, out string installerPath)
    {
        installerPath = "";
        string fileName = GitHubReleases.InstallerFileName(version);

        if (!GitHubReleases.TryDownloadAsset(version, ChecksumsFileName, out byte[] checksums) ||
            !GitHubReleases.TryDownloadAsset(version, fileName, out byte[] installer))
            return DownloadResult.NetworkError;

        string? expected = FindChecksum(Encoding.ASCII.GetString(checksums), fileName);
        if (expected == null || !string.Equals(Sha256Hex(installer), expected, StringComparison.OrdinalIgnoreCase))
            return DownloadResult.IntegrityError;

        string folder = Path.Combine(Path.GetTempPath(), DownloadFolderName);
        Directory.CreateDirectory(folder);
        installerPath = Path.Combine(folder, fileName);
        File.WriteAllBytes(installerPath, installer);
        return DownloadResult.Ready;
    }

    /// <summary>Finds the hash in "hash  name" (or "hash *name") lines, as written by sha256sum.</summary>
    static string? FindChecksum(string checksums, string fileName)
    {
        foreach (string line in checksums.Split('\n'))
        {
            string[] parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && parts[1].TrimStart('*') == fileName)
                return parts[0];
        }
        return null;
    }

    static string Sha256Hex(byte[] data)
    {
        byte* hash = stackalloc byte[BCrypt.Sha256Length];
        fixed (byte* input = data)
        {
            if (BCrypt.BCryptHash(BCrypt.BCRYPT_SHA256_ALG_HANDLE, null, 0, input, (uint)data.Length, hash, BCrypt.Sha256Length) != 0)
                return "";
        }
        return Convert.ToHexString(new ReadOnlySpan<byte>(hash, BCrypt.Sha256Length));
    }
}
