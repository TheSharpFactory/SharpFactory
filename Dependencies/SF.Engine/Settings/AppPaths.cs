#region Usings
using System;
using System.IO;
#endregion

namespace SF.Engine.Settings;

/// <summary>
/// Per-user folders, the same on Windows, macOS and Linux (each OS's application-data folder).
/// The environment variable SHARPFACTORY_HOME replaces the base folder (tests use it to stay isolated).
/// </summary>
public static class AppPaths
{
    private const string _homeVariable = "SHARPFACTORY_HOME";
    private const string _appFolderName = "TheSharpFactory";

    /// <summary>
    /// Settings: recent files, Sync targets, theme.
    /// </summary>
    public static string ConfigFolder
    {
        get
        {
            return Path.Combine(GetBase(Environment.SpecialFolder.ApplicationData), "Config");
        }
    }

    /// <summary>
    /// Error logs.
    /// </summary>
    public static string LogsFolder
    {
        get
        {
            return Path.Combine(GetBase(Environment.SpecialFolder.LocalApplicationData), "Logs");
        }
    }

    public static string SettingsFile
    {
        get
        {
            return Path.Combine(ConfigFolder, "settings.json");
        }
    }

    private static string GetBase(Environment.SpecialFolder folder)
    {
        var home = Environment.GetEnvironmentVariable(_homeVariable);
        if(!string.IsNullOrWhiteSpace(home))
            return home;

        return Path.Combine(Environment.GetFolderPath(folder, Environment.SpecialFolderOption.Create), _appFolderName);
    }
}
