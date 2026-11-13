#region Usings
using System;
#endregion

namespace SF.App.Services.Updates;

/// <summary>
/// The latest release on GitHub, and the download for this machine.
/// </summary>
internal sealed class ReleaseInfo
{
    public Version Version { get; set; }

    /// <summary>
    /// The release text (the version's CHANGELOG section, Markdown).
    /// </summary>
    public string Notes { get; set; }

    /// <summary>
    /// The release on GitHub, opened in the browser when the app cannot update itself.
    /// </summary>
    public Uri PageUri { get; set; }

    /// <summary>
    /// The app download for this OS and processor: "TheSharpFactory-win-x64.zip".
    /// </summary>
    public string PackageName { get; set; }

    /// <summary>
    /// Null when the release has no download for this machine.
    /// </summary>
    public Uri PackageUri { get; set; }

    /// <summary>
    /// SHA256SUMS.txt: the SHA-256 of every download. Null when the release has none.
    /// </summary>
    public Uri ChecksumsUri { get; set; }

    /// <summary>
    /// SHA256SUMS.txt.sig: SHA256SUMS.txt signed with The Sharp Factory's key. Null when the release has none.
    /// </summary>
    public Uri SignatureUri { get; set; }

    /// <summary>
    /// The app can download and check this release itself.
    /// </summary>
    public bool CanInstall
    {
        get
        {
            return PackageUri != null && ChecksumsUri != null && SignatureUri != null;
        }
    }
}

internal enum UpdateCheckStatus
{
    UpToDate,
    Available,
    Failed,
}

/// <summary>
/// What a check for a newer release found.
/// </summary>
internal sealed class UpdateCheckResult
{
    public UpdateCheckStatus Status { get; set; }

    /// <summary>
    /// The latest release (set when the check succeeded).
    /// </summary>
    public ReleaseInfo Release { get; set; }

    /// <summary>
    /// Why the check failed, for the user.
    /// </summary>
    public string Problem { get; set; }
}

/// <summary>
/// An update step that failed, with a message for the user.
/// </summary>
internal sealed class UpdateException : Exception
{
    public UpdateException(string message) : base(message)
    {
    }

    public UpdateException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
