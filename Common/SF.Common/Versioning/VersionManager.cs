#region Usings
using System;
#endregion

namespace SF.Common.Versioning;

internal sealed class VersionManager
{
    private const string _version = "1.0.7.13";
    public static string CurrentVersionString { get; } = _version;
    public static string CacheKiller { get; } = "v=" + _version;

    public static Version CurrentVersion { get; } = new Version(_version);
}
