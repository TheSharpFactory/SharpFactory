#region Usings
using System;
using System.IO;
using Xunit;
#endregion

namespace SF.Tests;

/// <summary>
/// The release workflow publishes only when CHANGELOG.md has a section for the version in VERSION. Checked on every
/// test run, so a release without its changelog heading fails here, not when the release is tagged. VERSION is the
/// last published version; the changes since then are under "## Unreleased", which may only be the first section.
/// </summary>
public sealed class ReleaseFilesTests
{
    private const string _unreleased = "## Unreleased";

    [Theory]
    [InlineData("CHANGELOG.md")]
    [InlineData("RELEASE_NOTES.md")]
    public void TheNewestVersionSectionIsTheCurrentVersion(string fileName)
    {
        var root = FindRepositoryRoot();
        var version = File.ReadAllText(Path.Combine(root, "VERSION")).Trim();
        var lines = File.ReadAllLines(Path.Combine(root, fileName));

        //the newest section with a version is the version released last: "## 1.2.0.8 — 2026-11-13"
        var first = Array.Find(lines, IsVersionHeading);
        Assert.True(first != null, fileName + " has no '## <version>' section");
        Assert.True(first == "## " + version || first.StartsWith("## " + version + " ", StringComparison.Ordinal),
            $"VERSION is {version} but the newest {fileName} section is '{first}': a release renames '{_unreleased}' to '## {version} — <date>' and sets VERSION to it (release.yml publishes only a version that has its section)");
    }

    [Theory]
    [InlineData("CHANGELOG.md")]
    [InlineData("RELEASE_NOTES.md")]
    public void UnreleasedIsOnlyTheFirstSection(string fileName)
    {
        var lines = File.ReadAllLines(Path.Combine(FindRepositoryRoot(), fileName));
        var sections = 0;
        foreach(var line in lines)
        {
            if(!IsSectionHeading(line))
                continue;

            sections++;
            if(line.StartsWith(_unreleased, StringComparison.Ordinal))
                Assert.True(sections == 1, $"{fileName}: '{_unreleased}' is section {sections}; it can only be the first (the changes since the last release)");
        }
    }

    private static bool IsSectionHeading(string line)
    {
        return line.StartsWith("## ", StringComparison.Ordinal);
    }

    private static bool IsVersionHeading(string line)
    {
        return IsSectionHeading(line) && !line.StartsWith(_unreleased, StringComparison.Ordinal);
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
