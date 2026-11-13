#region Usings
using System;
using System.IO;
using Xunit;
#endregion

namespace SF.Tests;

/// <summary>
/// The release workflow publishes only when CHANGELOG.md has a section for the version in VERSION. Checked on every
/// test run, so a version bump without its changelog heading fails here, not when the release is tagged.
/// </summary>
public sealed class ReleaseFilesTests
{
    [Fact]
    public void TheChangelogHasASectionForTheCurrentVersion()
    {
        var root = FindRepositoryRoot();
        var version = File.ReadAllText(Path.Combine(root, "VERSION")).Trim();
        var lines = File.ReadAllLines(Path.Combine(root, "CHANGELOG.md"));

        //the newest section is first, and it is the version being released: "## 1.2.0.8 — 2026-11-13"
        var first = Array.Find(lines, IsSectionHeading);
        Assert.True(first != null, "CHANGELOG.md has no '## <version>' section");
        Assert.True(first == "## " + version || first.StartsWith("## " + version + " ", StringComparison.Ordinal),
            $"VERSION is {version} but the newest CHANGELOG.md section is '{first}': rename that heading to '## {version}' (release.yml publishes only a version that has its section)");
    }

    private static bool IsSectionHeading(string line)
    {
        return line.StartsWith("## ", StringComparison.Ordinal);
    }

    /// <summary>
    /// The folder with VERSION and CHANGELOG.md, above the test's output folder.
    /// </summary>
    private static string FindRepositoryRoot()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while(folder != null)
        {
            if(File.Exists(Path.Combine(folder.FullName, "VERSION")) && File.Exists(Path.Combine(folder.FullName, "CHANGELOG.md")))
                return folder.FullName;

            folder = folder.Parent;
        }

        throw new InvalidOperationException("VERSION and CHANGELOG.md were not found above " + AppContext.BaseDirectory);
    }
}
