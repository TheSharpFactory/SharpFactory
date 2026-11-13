#region Usings
using Avalonia.Headless.XUnit;
using SF.App.Services;
using SF.App.Services.Updates;
using SF.App.ViewModels;
using SF.Engine.Settings;
using SF.Tests;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
#endregion

namespace SF.App.Tests;

/// <summary>
/// Automatic updates: reading GitHub's answer, the once-a-day rule, the checksum, the file swap (and putting the files
/// back when it fails), and the whole flow in the app. No test touches the network: a fake handler answers.
/// </summary>
public sealed class UpdateTests
{
    private const string _feed = "https://api.github.com/repos/TheSharpFactory/SharpFactory/releases/latest";
    private const string _downloads = "https://github.com/TheSharpFactory/SharpFactory/releases/download/v9.0.0.0/";

    private static string ComposeReleaseJson(string tag, string packageName, bool withChecksums, string body = "Better things.")
    {
        var assets = new StringBuilder();
        assets.Append($"{{\"name\":\"{packageName}\",\"browser_download_url\":\"{_downloads}{packageName}\"}}");
        assets.Append($",{{\"name\":\"sharpfactory-cli-win-x64.zip\",\"browser_download_url\":\"{_downloads}sharpfactory-cli-win-x64.zip\"}}");
        if(withChecksums)
        {
            assets.Append($",{{\"name\":\"SHA256SUMS.txt\",\"browser_download_url\":\"{_downloads}SHA256SUMS.txt\"}}");
            assets.Append($",{{\"name\":\"SHA256SUMS.txt.sig\",\"browser_download_url\":\"{_downloads}SHA256SUMS.txt.sig\"}}");
        }

        var bodyJson = System.Text.Json.JsonSerializer.Serialize(body);
        return $"{{\"tag_name\":\"{tag}\",\"html_url\":\"https://github.com/TheSharpFactory/SharpFactory/releases/tag/{tag}\",\"body\":{bodyJson},\"assets\":[{assets}]}}";
    }

    #region Reading the release
    [Fact]
    public void VersionsReadWithOrWithoutTheV()
    {
        Assert.True(UpdateChecker.TryParseVersion("v1.2.0.3", out var tagged));
        Assert.Equal(new Version(1, 2, 0, 3), tagged);
        Assert.True(UpdateChecker.TryParseVersion("1.2", out var shortVersion));
        Assert.Equal(new Version(1, 2, 0, 0), shortVersion);
        Assert.False(UpdateChecker.TryParseVersion("latest", out _));
        Assert.False(UpdateChecker.TryParseVersion(null, out _));
    }

    [Fact]
    public void EachSystemGetsItsOwnDownload()
    {
        Assert.Equal("TheSharpFactory-win-x64.zip", UpdateChecker.GetPackageName("win-x64"));
        Assert.Equal("TheSharpFactory-win-arm64.zip", UpdateChecker.GetPackageName("win-arm64"));
        Assert.Equal("TheSharpFactory-osx-arm64.zip", UpdateChecker.GetPackageName("osx-arm64"));
        Assert.Equal("TheSharpFactory-osx-x64.zip", UpdateChecker.GetPackageName("osx-x64"));
        Assert.Equal("TheSharpFactory-linux-x64.tar.gz", UpdateChecker.GetPackageName("linux-x64"));
        Assert.Null(UpdateChecker.GetPackageName("linux-arm64"));
    }

    [Fact]
    public void ANewerReleaseIsAvailableWithItsDownloadAndChecksums()
    {
        var result = UpdateChecker.ReadRelease(ComposeReleaseJson("v9.0.0.0", "TheSharpFactory-win-x64.zip", true), new Version(1, 2, 0, 3), "TheSharpFactory-win-x64.zip");

        Assert.Equal(UpdateCheckStatus.Available, result.Status);
        Assert.Equal(new Version(9, 0, 0, 0), result.Release.Version);
        Assert.Equal(new Uri(_downloads + "TheSharpFactory-win-x64.zip"), result.Release.PackageUri);
        Assert.Equal(new Uri(_downloads + "SHA256SUMS.txt"), result.Release.ChecksumsUri);
        Assert.Equal(new Uri(_downloads + "SHA256SUMS.txt.sig"), result.Release.SignatureUri);
        Assert.Equal("https://github.com/TheSharpFactory/SharpFactory/releases/tag/v9.0.0.0", result.Release.PageUri.ToString());
        Assert.True(result.Release.CanInstall);
    }

    [Fact]
    public void TheSameOrAnOlderReleaseIsUpToDate()
    {
        Assert.Equal(UpdateCheckStatus.UpToDate, UpdateChecker.ReadRelease(ComposeReleaseJson("v1.2.0.3", "a.zip", true), new Version(1, 2, 0, 3), "a.zip").Status);
        Assert.Equal(UpdateCheckStatus.UpToDate, UpdateChecker.ReadRelease(ComposeReleaseJson("v1.1.8.8", "a.zip", true), new Version(1, 2, 0, 3), "a.zip").Status);
    }

    [Fact]
    public void AReleaseWithoutChecksumsTheirSignatureOrThisSystemsDownloadCannotBeInstalledByTheApp()
    {
        var noChecksums = UpdateChecker.ReadRelease(ComposeReleaseJson("v9.0.0.0", "TheSharpFactory-win-x64.zip", false), new Version(1, 0), "TheSharpFactory-win-x64.zip");
        Assert.Equal(UpdateCheckStatus.Available, noChecksums.Status);
        Assert.False(noChecksums.Release.CanInstall);

        //an unsigned release (one made before signing, or by someone without the key) is offered as a page only
        var unsignedJson = ComposeReleaseJson("v9.0.0.0", "TheSharpFactory-win-x64.zip", true).Replace("SHA256SUMS.txt.sig", "notes.txt", StringComparison.Ordinal);
        var unsigned = UpdateChecker.ReadRelease(unsignedJson, new Version(1, 0), "TheSharpFactory-win-x64.zip");
        Assert.NotNull(unsigned.Release.ChecksumsUri);
        Assert.Null(unsigned.Release.SignatureUri);
        Assert.False(unsigned.Release.CanInstall);

        var noPackage = UpdateChecker.ReadRelease(ComposeReleaseJson("v9.0.0.0", "TheSharpFactory-win-x64.zip", true), new Version(1, 0), null);
        Assert.Null(noPackage.Release.PackageUri);
        Assert.False(noPackage.Release.CanInstall);
    }

    [Fact]
    public void OnlyHttpsDownloadsAreTaken()
    {
        var json = ComposeReleaseJson("v9.0.0.0", "a.zip", true).Replace("https://github.com/TheSharpFactory/SharpFactory/releases/download", "http://example.com/x", StringComparison.Ordinal);
        var result = UpdateChecker.ReadRelease(json, new Version(1, 0), "a.zip");

        Assert.Null(result.Release.PackageUri);
        Assert.Null(result.Release.ChecksumsUri);
        Assert.Null(result.Release.SignatureUri);
    }

    [Fact]
    public void AnAnswerThatIsNotAReleaseFails()
    {
        Assert.Equal(UpdateCheckStatus.Failed, UpdateChecker.ReadRelease("{\"message\":\"Not Found\"}", new Version(1, 0), "a.zip").Status);
        Assert.Equal(UpdateCheckStatus.Failed, UpdateChecker.ReadRelease("<html>", new Version(1, 0), "a.zip").Status);
        Assert.Equal(UpdateCheckStatus.Failed, UpdateChecker.ReadRelease("[]", new Version(1, 0), "a.zip").Status);
    }

    [Fact]
    public void TheFeedCanBePointedElsewhere()
    {
        var previous = Environment.GetEnvironmentVariable(UpdateChecker.FeedVariable);
        try
        {
            Environment.SetEnvironmentVariable(UpdateChecker.FeedVariable, null);
            Assert.Equal(_feed, UpdateChecker.GetFeedUri().ToString());
            Environment.SetEnvironmentVariable(UpdateChecker.FeedVariable, "http://localhost:8123/latest.json");
            Assert.Equal("http://localhost:8123/latest.json", UpdateChecker.GetFeedUri().ToString());
        }
        finally
        {
            Environment.SetEnvironmentVariable(UpdateChecker.FeedVariable, previous);
        }
    }

    [Fact]
    public async Task TheRequestNamesTheAppAndItsVersion()
    {
        var handler = new FakeHttpHandler();
        handler.Add(_feed, ComposeReleaseJson("v9.0.0.0", "a.zip", true));
        var checker = new UpdateChecker(new HttpClient(handler), new Uri(_feed), new Version(1, 2, 0, 3), "a.zip");

        var result = await checker.CheckAsync(CancellationToken.None);

        Assert.Equal(UpdateCheckStatus.Available, result.Status);
        Assert.Equal("TheSharpFactory/1.2.0.3", handler.UserAgents[0]);
    }

    [Fact]
    public async Task AnErrorAnswerOrNoNetworkFailsQuietly()
    {
        var handler = new FakeHttpHandler { Status = HttpStatusCode.Forbidden };
        var forbidden = await new UpdateChecker(new HttpClient(handler), new Uri(_feed), new Version(1, 0), "a.zip").CheckAsync(CancellationToken.None);
        Assert.Equal(UpdateCheckStatus.Failed, forbidden.Status);
        Assert.Contains("403", forbidden.Problem, StringComparison.Ordinal);

        var offline = new FakeHttpHandler { ThrowNetworkError = true };
        var noNetwork = await new UpdateChecker(new HttpClient(offline), new Uri(_feed), new Version(1, 0), "a.zip").CheckAsync(CancellationToken.None);
        Assert.Equal(UpdateCheckStatus.Failed, noNetwork.Status);
        Assert.Contains("could not be reached", noNetwork.Problem, StringComparison.Ordinal);
    }
    [Fact]
    public async Task AConnectionLostWhileReadingTheAnswerFailsQuietly()
    {
        var handler = new FakeHttpHandler { DropWhileReading = _feed };

        var result = await new UpdateChecker(new HttpClient(handler), new Uri(_feed), new Version(1, 0), "a.zip").CheckAsync(CancellationToken.None);

        Assert.Equal(UpdateCheckStatus.Failed, result.Status);
        Assert.NotNull(result.Problem);
    }
    #endregion

    #region Once a day, and the release text
    [Fact]
    public void ACheckIsDueOnceADay()
    {
        var now = new DateTime(2026, 11, 12, 12, 0, 0, DateTimeKind.Utc);

        Assert.True(UpdateSchedule.IsCheckDue(null, now));
        Assert.False(UpdateSchedule.IsCheckDue(now.AddHours(-23), now));
        Assert.True(UpdateSchedule.IsCheckDue(now.AddHours(-24), now));
        Assert.True(UpdateSchedule.IsCheckDue(now.AddDays(-3), now));
        //the clock went back
        Assert.True(UpdateSchedule.IsCheckDue(now.AddHours(5), now));
    }

    [Fact]
    public void TheReleaseTextIsShortenedToPlainText()
    {
        var markdown = "The first release: a **desktop app** and a `sharpfactory` command line.\r\n\r\n### What it does\r\n\r\n- Reads a database.\r\n- Generates the data access.\r\n\r\n### Downloads\r\n\r\n| Download | What |\r\n|---|---|\r\n";

        var text = ReleaseNotes.Summarize(markdown, 700);

        Assert.Equal("The first release: a desktop app and a sharpfactory command line.\n\nWhat it does\n• Reads a database.\n• Generates the data access.", text);
        Assert.Null(ReleaseNotes.Summarize("  ", 700));

        var cut = ReleaseNotes.Summarize("First paragraph here.\n\nSecond paragraph here.\n\nThird paragraph here.", 40);
        Assert.Equal("First paragraph here.\n…", cut);
    }

    [Fact]
    public void TheLinesTheChangelogWasWrappedOverAreJoinedAgain()
    {
        //CHANGELOG.md wraps at 120 characters; the dialog wraps at its own width
        var markdown = "**Regenerate your code:** paged reads\r\nfailed. Sync to fix it.\r\n\r\n### What's new\r\n\r\n- **Automatic updates.** The app looks\r\n  for a newer release.\r\n- The command line comes\r\n  with the app. See [the docs](https://example.com).\r\n\r\n---\r\n";

        Assert.Equal("Regenerate your code: paged reads failed. Sync to fix it.\n\nWhat's new\n• Automatic updates. The app looks for a newer release.\n• The command line comes with the app. See the docs.", ReleaseNotes.Summarize(markdown, 1500));
    }
    #endregion

    #region Checksums and where the app runs from
    [Fact]
    public void TheChecksumIsFoundBySha256sumsFileName()
    {
        var hash = new string('a', 64);
        var other = new string('b', 64);
        var sums = $"{other}  sharpfactory-cli-win-x64.zip\n{hash}  TheSharpFactory-win-x64.zip\n{other} *TheSharpFactory-linux-x64.tar.gz\n";

        Assert.Equal(hash, UpdateInstaller.FindHash(sums, "TheSharpFactory-win-x64.zip"));
        Assert.Equal(other, UpdateInstaller.FindHash(sums, "TheSharpFactory-linux-x64.tar.gz"));
        Assert.Null(UpdateInstaller.FindHash(sums, "TheSharpFactory-osx-arm64.zip"));
        Assert.Null(UpdateInstaller.FindHash("short  TheSharpFactory-win-x64.zip", "TheSharpFactory-win-x64.zip"));
    }

    [Fact]
    public void AReleaseDownloadCanUpdateItselfButABuildFromTheSourceCannot()
    {
        using(var folder = new TestFolder())
        {
            //a program in a plain folder: the Windows or the Linux layout (macOS has a bundle, tested on its own)
            var platform = OperatingSystem.IsWindows() ? AppPlatform.Windows : AppPlatform.Linux;
            var program = Path.Combine(folder.Path, OperatingSystem.IsWindows() ? "TheSharpFactory.exe" : "TheSharpFactory");
            File.WriteAllText(program, "app");

            var release = InstallLocation.Find(program, platform);
            Assert.Null(release.Problem);
            Assert.Equal(folder.Path, release.Folder);
            Assert.Equal(Path.GetFileName(program), release.RelativeExecutable);

            File.WriteAllText(Path.Combine(folder.Path, "TheSharpFactory.dll"), "dll");
            Assert.NotNull(InstallLocation.Find(program, platform).Problem);
            Assert.NotNull(InstallLocation.Find(Path.Combine(folder.Path, "Other.exe"), platform).Problem);
            Assert.NotNull(InstallLocation.Find(null, platform).Problem);
        }
    }

    [Fact]
    public void EachSystemHasItsOwnProgramName()
    {
        //paths with "/" work on every system, so all three layouts are checked everywhere
        var windows = InstallLocation.Find("/apps/sf/TheSharpFactory.exe", AppPlatform.Windows);
        Assert.Null(windows.Problem);
        Assert.Equal("TheSharpFactory.exe", windows.RelativeExecutable);
        Assert.NotNull(InstallLocation.Find("/apps/sf/TheSharpFactory", AppPlatform.Windows).Problem);

        var linux = InstallLocation.Find("/opt/sf/TheSharpFactory", AppPlatform.Linux);
        Assert.Null(linux.Problem);
        Assert.Equal("TheSharpFactory", linux.RelativeExecutable);
        Assert.NotNull(InstallLocation.Find("/opt/sf/TheSharpFactory.exe", AppPlatform.Linux).Problem);

        Assert.NotNull(InstallLocation.Find("/opt/sf/TheSharpFactory", AppPlatform.MacOS).Problem);
    }

    [Fact]
    public void OnMacOSTheWholeAppBundleIsUpdatedUnlessMacOSRunsATemporaryCopy()
    {
        var bundle = InstallLocation.Find("/Applications/TheSharpFactory.app/Contents/MacOS/TheSharpFactory", AppPlatform.MacOS);
        Assert.Null(bundle.Problem);
        Assert.Equal("Applications", Path.GetFileName(bundle.Folder));
        Assert.Equal("TheSharpFactory.app", Path.GetFileName(bundle.Bundle));
        Assert.Equal(Path.Combine("TheSharpFactory.app", "Contents", "MacOS", "TheSharpFactory"), bundle.RelativeExecutable);

        var startInfo = bundle.CreateStartInfo("/Users/me/Model.edmj");
        Assert.Equal("open", startInfo.FileName);
        Assert.Equal($"-n|{bundle.Bundle}|--args|/Users/me/Model.edmj", string.Join("|", startInfo.ArgumentList));

        Assert.NotNull(InstallLocation.Find("/private/var/folders/x/T/AppTranslocation/1234/d/TheSharpFactory.app/Contents/MacOS/TheSharpFactory", AppPlatform.MacOS).Problem);
        Assert.NotNull(InstallLocation.Find("/Applications/Other.app/Contents/MacOS/TheSharpFactory", AppPlatform.MacOS).Problem);
    }

    [Fact]
    public void TheProgramStartsWithTheModelFileThatWasOpen()
    {
        var location = new InstallLocation { Folder = "/opt/sf", Executable = "/opt/sf/TheSharpFactory" };

        var withFile = location.CreateStartInfo("/home/me/Model.edmj");
        Assert.Equal("/opt/sf/TheSharpFactory", withFile.FileName);
        Assert.Equal("/home/me/Model.edmj", Assert.Single(withFile.ArgumentList));
        Assert.False(withFile.UseShellExecute);
        Assert.Empty(location.CreateStartInfo(null).ArgumentList);
    }
    #endregion

    #region Installing
    [Fact]
    public async Task TheUpdateReplacesTheAppsFilesAndLeavesOthersAlone()
    {
        using(var folder = new TestFolder())
        {
            var install = CreateInstall(Path.Combine(folder.Path, "app"));
            var handler = new FakeHttpHandler();
            var release = PublishPackage(handler, folder.Path, "TheSharpFactory-test.zip", null);

            await CreateInstaller(handler, release, install, null, null).RunAsync();

            Assert.Equal("new app", File.ReadAllText(Path.Combine(install.Folder, "TheSharpFactory.exe")));
            Assert.Equal("new cli", File.ReadAllText(Path.Combine(install.Folder, "sharpfactory.exe")));
            Assert.True(File.Exists(Path.Combine(install.Folder, "licenses", "New.txt")));
            Assert.False(File.Exists(Path.Combine(install.Folder, "licenses", "Old.txt")));
            Assert.Equal("mine", File.ReadAllText(Path.Combine(install.Folder, "notes.txt")));
            Assert.Equal("old app", File.ReadAllText(Path.Combine(install.Folder, "TheSharpFactory.exe" + UpdateInstaller.OldSuffix)));
            Assert.False(Directory.Exists(Path.Combine(install.Folder, UpdateInstaller.StagingFolderName)));

            //the new version deletes what the old one left
            Assert.True(UpdateInstaller.TryCleanUp(install.Folder));
            Assert.Equal(new List<string> { "LICENSE", "TheSharpFactory.exe", "licenses", "notes.txt", "sharpfactory.exe" }, ListFolder(install.Folder));
        }
    }

    [Fact]
    public async Task ATarGzDownloadIsUnpackedToo()
    {
        using(var folder = new TestFolder())
        {
            var install = CreateInstall(Path.Combine(folder.Path, "app"));
            var handler = new FakeHttpHandler();
            var release = PublishPackage(handler, folder.Path, "TheSharpFactory-test.tar.gz", null);

            await CreateInstaller(handler, release, install, null, null).RunAsync();

            Assert.Equal("new app", File.ReadAllText(Path.Combine(install.Folder, "TheSharpFactory.exe")));
            Assert.Equal("new cli", File.ReadAllText(Path.Combine(install.Folder, "sharpfactory.exe")));
        }
    }

    [Fact]
    public async Task ADamagedDownloadChangesNothing()
    {
        using(var folder = new TestFolder())
        {
            var install = CreateInstall(Path.Combine(folder.Path, "app"));
            var handler = new FakeHttpHandler();
            var release = PublishPackage(handler, folder.Path, "TheSharpFactory-test.zip", new string('0', 64));
            var before = ListFolder(install.Folder);

            var error = await Assert.ThrowsAsync<UpdateException>(CreateInstaller(handler, release, install, null, null).RunAsync);

            Assert.Contains("damaged", error.Message, StringComparison.Ordinal);
            Assert.Equal(before, ListFolder(install.Folder));
            Assert.Equal("old app", File.ReadAllText(Path.Combine(install.Folder, "TheSharpFactory.exe")));
        }
    }

    [Fact]
    public async Task AReleaseNotSignedWithTheTrustedKeyChangesNothing()
    {
        using(var folder = new TestFolder())
        {
            var install = CreateInstall(Path.Combine(folder.Path, "app"));
            var handler = new FakeHttpHandler();
            var release = PublishPackage(handler, folder.Path, "TheSharpFactory-test.zip", null);
            var before = ListFolder(install.Folder);

            //signed, but with another key: what a release made by someone else looks like
            using(var otherKey = ECDsa.Create(ECCurve.NamedCurves.nistP256))
            {
                var installer = CreateInstaller(handler, release, install, null, null);
                installer.TrustedKey = otherKey.ExportSubjectPublicKeyInfoPem();
                var error = await Assert.ThrowsAsync<UpdateException>(installer.RunAsync);
                Assert.Contains("not signed by The Sharp Factory", error.Message, StringComparison.Ordinal);
            }

            //the checksums changed after they were signed (a package swapped for another, with its hash)
            var signedChecksums = handler.Get(_downloads + "SHA256SUMS.txt");
            handler.Add(_downloads + "SHA256SUMS.txt", Encoding.UTF8.GetString(signedChecksums).Replace("cccc", "dddd", StringComparison.Ordinal));
            var tampered = await Assert.ThrowsAsync<UpdateException>(CreateInstaller(handler, release, install, null, null).RunAsync);
            Assert.Contains("not signed by The Sharp Factory", tampered.Message, StringComparison.Ordinal);

            //no signature at all
            handler.Add(_downloads + "SHA256SUMS.txt", signedChecksums);
            handler.Add(_downloads + "SHA256SUMS.txt.sig", Array.Empty<byte>());
            var unsigned = await Assert.ThrowsAsync<UpdateException>(CreateInstaller(handler, release, install, null, null).RunAsync);
            Assert.Contains("not signed by The Sharp Factory", unsigned.Message, StringComparison.Ordinal);

            Assert.Equal(before, ListFolder(install.Folder));
            Assert.Equal("old app", File.ReadAllText(Path.Combine(install.Folder, "TheSharpFactory.exe")));
            //nothing past the checksums was downloaded
            Assert.DoesNotContain(release.PackageUri.ToString(), handler.Requests);
        }
    }

    [Fact]
    public void SignaturesMadeByOpensslAreRead()
    {
        //release.yml signs with "openssl dgst -sha256 -sign"; this one was made that way, with a throwaway key
        const string publicKey = "-----BEGIN PUBLIC KEY-----\n" +
            "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEYPpJaI3sxg4iuDWfwpTQRGsoCDMT\n" +
            "fHFQsLNeKArpRvvY/cFcE03Cu7lOl5ivHK8rtbZpYFFrKZ7ajYMHVp6saA==\n" +
            "-----END PUBLIC KEY-----\n";
        var signature = Convert.FromBase64String("MEQCIFW82IndbKZy1y4qFZgHww+eX5vs7xL0KFl4wlZsMKOWAiA55LERA7fAx5VvjLJK/IfkoTq/DZqWRL3CiiadkQ8aKA==");
        var checksums = Encoding.UTF8.GetBytes("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef  TheSharpFactory-win-x64.zip\n");

        Assert.True(UpdateSignature.IsValid(checksums, signature, publicKey));

        var changed = (byte[])checksums.Clone();
        changed[0] = (byte)'1';
        Assert.False(UpdateSignature.IsValid(changed, signature, publicKey));
        Assert.False(UpdateSignature.IsValid(checksums, Encoding.UTF8.GetBytes("not a signature"), publicKey));
        Assert.False(UpdateSignature.IsValid(checksums, null, publicKey));
    }

    [Fact]
    public void TheReleaseKeyIsBuiltIntoTheApp()
    {
        var releaseKey = UpdateSignature.ReleaseKey;
        using(var key = ECDsa.Create())
        {
            key.ImportFromPem(releaseKey);
            Assert.Equal(256, key.KeySize);
        }

        //the app trusts the release key unless a test says otherwise
        Assert.Equal(releaseKey, new UpdateInstaller(new HttpClient(new FakeHttpHandler()), new ReleaseInfo(), new InstallLocation(), new Version(1, 0), null, null).TrustedKey);
        //and a release signed with the test key is not one of ours
        Assert.False(UpdateSignature.IsValid(Encoding.UTF8.GetBytes("x"), TestSigning.Sign(Encoding.UTF8.GetBytes("x")), releaseKey));
    }

    [Fact]
    public async Task AConnectionLostDuringTheDownloadChangesNothing()
    {
        using(var folder = new TestFolder())
        {
            var install = CreateInstall(Path.Combine(folder.Path, "app"));
            var handler = new FakeHttpHandler();
            var release = PublishPackage(handler, folder.Path, "TheSharpFactory-test.zip", null);
            handler.DropWhileReading = release.PackageUri.ToString();
            var before = ListFolder(install.Folder);

            var error = await Assert.ThrowsAsync<UpdateException>(CreateInstaller(handler, release, install, null, null).RunAsync);

            Assert.StartsWith("The download failed", error.Message, StringComparison.Ordinal);
            Assert.EndsWith("Nothing was changed.", error.Message, StringComparison.Ordinal);
            Assert.Equal(before, ListFolder(install.Folder));
        }
    }

    [Fact]
    public async Task ADownloadWithoutTheProgramChangesNothing()
    {
        using(var folder = new TestFolder())
        {
            var install = CreateInstall(Path.Combine(folder.Path, "app"));
            install.Executable = Path.Combine(install.Folder, "Renamed.exe");
            var handler = new FakeHttpHandler();
            var release = PublishPackage(handler, folder.Path, "TheSharpFactory-test.zip", null);
            var before = ListFolder(install.Folder);

            var error = await Assert.ThrowsAsync<UpdateException>(CreateInstaller(handler, release, install, null, null).RunAsync);

            Assert.Contains("Renamed.exe", error.Message, StringComparison.Ordinal);
            Assert.Equal(before, ListFolder(install.Folder));
        }
    }

    [Fact]
    public async Task AnUpdateInProgressIsLeftAloneByAnotherCopyOfTheApp()
    {
        using(var folder = new TestFolder())
        {
            var install = CreateInstall(Path.Combine(folder.Path, "app"));
            var staging = Path.Combine(install.Folder, UpdateInstaller.StagingFolderName);
            Directory.CreateDirectory(staging);
            File.WriteAllText(Path.Combine(staging, "TheSharpFactory-test.zip"), "half a download");
            File.WriteAllText(Path.Combine(install.Folder, "TheSharpFactory.exe" + UpdateInstaller.OldSuffix), "left over");
            var handler = new FakeHttpHandler();
            var release = PublishPackage(handler, folder.Path, "TheSharpFactory-test.zip", null);

            //the first copy is updating: it holds the lock
            using(new FileStream(Path.Combine(staging, UpdateInstaller.LockFileName), FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                Assert.True(UpdateInstaller.IsStagingInUse(staging));

                //a second copy starts: its clean-up deletes what is left over, not the download in progress
                UpdateInstaller.TryCleanUp(install.Folder);
                Assert.True(File.Exists(Path.Combine(staging, "TheSharpFactory-test.zip")));
                Assert.False(File.Exists(Path.Combine(install.Folder, "TheSharpFactory.exe" + UpdateInstaller.OldSuffix)));

                //and its own update stops without touching it
                var error = await Assert.ThrowsAsync<UpdateException>(CreateInstaller(handler, release, install, null, null).RunAsync);
                Assert.StartsWith("Another copy of the app is updating", error.Message, StringComparison.Ordinal);
                Assert.True(File.Exists(Path.Combine(staging, "TheSharpFactory-test.zip")));
                Assert.Empty(handler.Requests);
            }

            //once the first copy is done (or gone), what it left is cleaned up
            Assert.False(UpdateInstaller.IsStagingInUse(staging));
            Assert.True(UpdateInstaller.TryCleanUp(install.Folder));
            Assert.False(Directory.Exists(staging));
        }
    }

    [Fact]
    public void CancelAndReplacingTheFilesCannotBothWin()
    {
        using(var cancelledFirst = new UpdateCancellation())
        {
            Assert.True(cancelledFirst.TryCancel());
            Assert.True(cancelledFirst.Token.IsCancellationRequested);
            Assert.False(cancelledFirst.TryBeginReplacing());
            Assert.True(cancelledFirst.IsCancelled);
        }

        using(var replacingFirst = new UpdateCancellation())
        {
            Assert.True(replacingFirst.TryBeginReplacing());
            Assert.False(replacingFirst.TryCancel());
            Assert.False(replacingFirst.Token.IsCancellationRequested);
            Assert.False(replacingFirst.IsCancelled);
        }
    }

    [Fact]
    public async Task ACancelledUpdateStopsBeforeReplacingAnything()
    {
        using(var folder = new TestFolder())
        {
            var install = CreateInstall(Path.Combine(folder.Path, "app"));
            var handler = new FakeHttpHandler();
            var release = PublishPackage(handler, folder.Path, "TheSharpFactory-test.zip", null);
            var before = ListFolder(install.Folder);
            var steps = new RecordingProgress();

            using(var cancellation = new UpdateCancellation())
            {
                Assert.True(cancellation.TryCancel());
                await Assert.ThrowsAnyAsync<OperationCanceledException>(CreateInstaller(handler, release, install, steps, cancellation).RunAsync);
            }

            Assert.Equal(before, ListFolder(install.Folder));
            Assert.DoesNotContain(UpdateStage.Replacing, steps.Stages);
        }
    }

    [Fact]
    public async Task ReplacingTheFilesIsReportedOnceCancelNoLongerWorks()
    {
        using(var folder = new TestFolder())
        {
            var install = CreateInstall(Path.Combine(folder.Path, "app"));
            var handler = new FakeHttpHandler();
            var release = PublishPackage(handler, folder.Path, "TheSharpFactory-test.zip", null);
            var steps = new RecordingProgress();

            using(var cancellation = new UpdateCancellation())
            {
                await CreateInstaller(handler, release, install, steps, cancellation).RunAsync();

                Assert.False(cancellation.TryCancel());
            }

            Assert.Equal(UpdateStage.Replacing, steps.Stages[steps.Stages.Count - 1]);
            Assert.Equal(UpdateStage.Downloading, steps.Stages[0]);
            Assert.Equal("new app", File.ReadAllText(Path.Combine(install.Folder, "TheSharpFactory.exe")));
        }
    }

    [Fact]
    public async Task WhenAFileCannotBeReplacedTheOthersArePutBack()
    {
        using(var folder = new TestFolder())
        {
            var install = CreateInstall(Path.Combine(folder.Path, "app"));
            var handler = new FakeHttpHandler();
            var release = PublishPackage(handler, folder.Path, "TheSharpFactory-test.zip", null);

            using(BlockReplacingTheLicense(install.Folder))
            {
                var before = ListFolder(install.Folder);
                var error = await Assert.ThrowsAsync<UpdateException>(CreateInstaller(handler, release, install, null, null).RunAsync);
                Assert.Contains("left as it was", error.Message, StringComparison.Ordinal);
                Assert.Equal(before, ListFolder(install.Folder));
            }

            Assert.Equal("old app", File.ReadAllText(Path.Combine(install.Folder, "TheSharpFactory.exe")));
            Assert.Equal("old cli", File.ReadAllText(Path.Combine(install.Folder, "sharpfactory.exe")));
            Assert.True(File.Exists(Path.Combine(install.Folder, "licenses", "Old.txt")));
        }
    }

    [Fact]
    public async Task ARunningProgramIsReplacedByRenamingIt()
    {
        //what makes the update work on Windows: a running program cannot be overwritten or deleted, but it can be
        //renamed. Linux and macOS rename (and delete) a running program too
        using(var folder = new TestFolder())
        {
            var install = CreateInstall(Path.Combine(folder.Path, "app"));
            var program = Path.Combine(install.Folder, "TheSharpFactory.exe");
            var start = StartSomethingLong(program);
            var handler = new FakeHttpHandler();
            var release = PublishPackage(handler, folder.Path, "TheSharpFactory-test.zip", null);

            var running = Process.Start(start);
            try
            {
                if(OperatingSystem.IsWindows())
                {
                    var deleteError = Record.Exception(() => File.Delete(program));
                    Assert.True(deleteError is IOException || deleteError is UnauthorizedAccessException);
                }

                await CreateInstaller(handler, release, install, null, null).RunAsync();

                Assert.Equal("new app", File.ReadAllText(program));
                Assert.False(running.HasExited);
                //on Windows the old program cannot be deleted while it runs: the clean-up leaves it for the next start
                Assert.Equal(!OperatingSystem.IsWindows(), UpdateInstaller.TryCleanUp(install.Folder));
            }
            finally
            {
                running.Kill(true);
                running.WaitForExit();
                running.Dispose();
            }

            //Windows (or a virus scanner) can hold the program a moment after it exits: the clean-up tries again
            await UpdateInstaller.CleanUpAsync(install.Folder);
            Assert.False(File.Exists(program + UpdateInstaller.OldSuffix));
        }
    }
    #endregion

    #region In the app
    [AvaloniaFact]
    public void CheckForUpdatesSaysWhenThisIsTheLatestVersion()
    {
        using(var app = new AppHarness("1.2.0.3"))
        {
            var handler = new FakeHttpHandler();
            handler.Add(_feed, ComposeReleaseJson("v1.2.0.3", FindPackageName(), true));
            var updates = app.ViewModel.Updates;
            updates.Http = new HttpClient(handler);
            app.ViewModel.Navigate(app.ViewModel.About);
            app.Pump();

            app.Find<Avalonia.Controls.Button>("CheckForUpdatesButton").Command.Execute(null);
            app.PumpUntil(() => updates.IsIdle && updates.StatusText != null && updates.StatusText.StartsWith("You have", StringComparison.Ordinal));

            Assert.Equal("You have the latest version (1.2.0.3).", app.Find<Avalonia.Controls.TextBlock>("UpdateStatus").Text);
            Assert.False(app.Find<Avalonia.Controls.Button>("UpdateNowButton").IsVisible);
            Assert.Null(app.ViewModel.Dialog);
            Assert.NotNull(AppSettingsStore.Load().LastUpdateCheck);
            app.Screenshot("about-updates-latest");
        }
    }

    [AvaloniaFact]
    public void ANewerReleaseIsOfferedAndLaterLeavesTheAppAsItIs()
    {
        using(var app = new AppHarness("1.2.0.3"))
        {
            var handler = new FakeHttpHandler();
            handler.Add(_feed, ComposeReleaseJson("v9.0.0.0", FindPackageName(), true, "The **best** version.\n\n### Downloads\n\n| a | b |"));
            var updates = app.ViewModel.Updates;
            updates.Http = new HttpClient(handler);
            app.ViewModel.Navigate(app.ViewModel.About);

            var check = updates.CheckAsync(true);
            app.PumpUntil(() => app.ViewModel.Dialog != null);
            app.Screenshot("update-question");

            var dialog = app.ViewModel.Dialog;
            Assert.Equal("An update is available", dialog.Title);
            Assert.Contains("Version 9.0.0.0 is ready (you have 1.2.0.3)", dialog.Message, StringComparison.Ordinal);
            //the release notes are in their own box, not in the question
            Assert.Equal("The best version.", dialog.Details);
            Assert.DoesNotContain("best", dialog.Message, StringComparison.Ordinal);

            dialog.ChooseCommand.Execute(dialog.Buttons[0]);
            app.PumpUntil(() => check.IsCompleted);

            Assert.True(updates.HasUpdate);
            Assert.True(app.Find<Avalonia.Controls.Button>("UpdateNowButton").IsVisible);
            Assert.Equal("Update to 9.0.0.0", app.Find<Avalonia.Controls.Button>("UpdateNowButton").Content);
            Assert.Single(handler.Requests);
        }
    }

    [AvaloniaFact]
    public void AFailedCheckSaysSoOnlyWhenTheUserAsked()
    {
        using(var app = new AppHarness("1.2.0.3"))
        {
            var handler = new FakeHttpHandler { ThrowNetworkError = true };
            var updates = app.ViewModel.Updates;
            updates.Http = new HttpClient(handler);

            var automatic = updates.CheckIfDueAsync();
            app.PumpUntil(() => automatic.IsCompleted);
            Assert.Null(updates.StatusText);
            Assert.Null(app.ViewModel.Dialog);

            var asked = updates.CheckAsync(true);
            app.PumpUntil(() => asked.IsCompleted);
            Assert.StartsWith("Could not check for updates.", updates.StatusText, StringComparison.Ordinal);

            //a failed check does not count as the day's check
            Assert.Null(app.ViewModel.Settings.LastUpdateCheck);
        }
    }

    [AvaloniaFact]
    public void TheAutomaticCheckRunsOnceADayAndCanBeTurnedOff()
    {
        using(var app = new AppHarness("1.2.0.3"))
        {
            var handler = new FakeHttpHandler();
            handler.Add(_feed, ComposeReleaseJson("v1.2.0.3", FindPackageName(), true));
            var updates = app.ViewModel.Updates;
            updates.Http = new HttpClient(handler);

            var first = updates.CheckIfDueAsync();
            app.PumpUntil(() => first.IsCompleted);
            var second = updates.CheckIfDueAsync();
            app.PumpUntil(() => second.IsCompleted);
            Assert.Single(handler.Requests);

            app.ViewModel.Settings.LastUpdateCheck = DateTime.UtcNow.AddDays(-2);
            app.ViewModel.Navigate(app.ViewModel.About);
            app.Pump();
            app.Find<Avalonia.Controls.CheckBox>("CheckForUpdatesAutomatically").IsChecked = false;
            app.Pump();
            var off = updates.CheckIfDueAsync();
            app.PumpUntil(() => off.IsCompleted);

            Assert.Single(handler.Requests);
            Assert.False(AppSettingsStore.Load().CheckForUpdates);
        }
    }

    [AvaloniaFact]
    public void AnAutomaticCheckDoesNotInterruptADialog()
    {
        using(var app = new AppHarness("1.2.0.3"))
        {
            var handler = new FakeHttpHandler();
            handler.Add(_feed, ComposeReleaseJson("v9.0.0.0", FindPackageName(), true));
            var updates = app.ViewModel.Updates;
            updates.Http = new HttpClient(handler);

            var message = app.ViewModel.ShowMessageAsync("Something", "Already open.", false);
            var check = updates.CheckIfDueAsync();
            app.PumpUntil(() => check.IsCompleted);
            Assert.Equal("Something", app.ViewModel.Dialog.Title);
            Assert.True(updates.HasUpdate);

            app.ViewModel.Dialog.Cancel();
            app.PumpUntil(() => message.IsCompleted);

            //the next tick asks, without another request
            var next = updates.CheckIfDueAsync();
            app.PumpUntil(() => app.ViewModel.Dialog != null);
            Assert.Equal("An update is available", app.ViewModel.Dialog.Title);
            Assert.Equal("Version 9.0.0.0 is ready (you have 1.2.0.3). Update now downloads it, replaces this copy and restarts the app.", app.ViewModel.Dialog.Message);
            Assert.Equal("What's new", app.ViewModel.Dialog.DetailsTitle);
            Assert.Equal("Better things.", app.ViewModel.Dialog.Details);
            app.ViewModel.Dialog.Cancel();
            app.PumpUntil(() => next.IsCompleted);
            Assert.Single(handler.Requests);
        }
    }

    [AvaloniaFact]
    public void TheUpdateQuestionShowsTheReleaseNotesInTheirOwnBox()
    {
        using(var app = new AppHarness("1.2.0.8"))
        {
            //a real, long release text: the 1.2.0.8 section of CHANGELOG.md, wrapped at 120 characters
            var handler = new FakeHttpHandler();
            handler.Add(_feed, ComposeReleaseJson("v9.0.0.0", FindPackageName(), true, ReadChangelogSection("1.2.0.8")));
            var updates = app.ViewModel.Updates;
            updates.Http = new HttpClient(handler);
            app.ViewModel.Navigate(app.ViewModel.About);

            var check = updates.CheckAsync(true);
            app.PumpUntil(() => app.ViewModel.Dialog != null);
            var details = app.ViewModel.Dialog.Details;

            //the dialog's view is made on the next layout pass
            app.PumpUntil(() => app.Find<Avalonia.Controls.StackPanel>("DialogDetails") != null);
            Assert.True(app.Find<Avalonia.Controls.StackPanel>("DialogDetails").IsVisible);
            //no line ends where the changelog wrapped: each paragraph or list item is one line
            foreach(var line in details.Split('\n'))
                Assert.False(line.Length > 0 && char.IsLower(line[0]), "a wrapped line was not joined: " + line);

            Assert.DoesNotContain("**", details, StringComparison.Ordinal);
            //the Downloads section (a table of file names) is left out
            Assert.DoesNotContain("SHA256SUMS", details, StringComparison.Ordinal);
            Assert.DoesNotContain("|", details, StringComparison.Ordinal);
            app.Screenshot("about-update-available");

            app.ViewModel.Dialog.Cancel();
            app.PumpUntil(() => check.IsCompleted);
        }
    }

    /// <summary>
    /// One version's section of CHANGELOG.md, as release.yml puts it on the release page (without its heading).
    /// </summary>
    private static string ReadChangelogSection(string version)
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while(folder != null && !File.Exists(Path.Combine(folder.FullName, "CHANGELOG.md")))
            folder = folder.Parent;

        Assert.NotNull(folder);
        var text = File.ReadAllText(Path.Combine(folder.FullName, "CHANGELOG.md"));
        var start = text.IndexOf("## " + version, StringComparison.Ordinal);
        Assert.True(start >= 0, "CHANGELOG.md has no section " + version);
        start = text.IndexOf('\n', start) + 1;
        var end = text.IndexOf("\n## ", start, StringComparison.Ordinal);
        return end < 0 ? text.Substring(start) : text.Substring(start, end - start);
    }

    [AvaloniaFact]
    public void UpdateNowInstallsAndRestartsWithTheOpenModel()
    {
        using(var app = new AppHarness("1.2.0.3"))
        {
            var modelFile = app.CopyFixture();
            app.ViewModel.OpenFile(modelFile);
            var install = CreateInstall(Path.Combine(app.Folder, "install"));
            var handler = new FakeHttpHandler();
            handler.Add(_feed, ComposeReleaseJson("v9.0.0.0", FindPackageName(), true));
            PublishPackage(handler, app.Folder, FindPackageName(), null);
            var host = new FakeAppHost();
            app.ViewModel.Host = host;
            var updates = app.ViewModel.Updates;
            updates.Http = new HttpClient(handler);
            updates.Location = install;
            updates.TrustedKey = TestSigning.PublicKey;

            var check = updates.CheckAsync(true);
            app.PumpUntil(() => app.ViewModel.Dialog != null);
            app.ViewModel.Dialog.ChooseCommand.Execute(app.ViewModel.Dialog.Buttons[1]);
            app.PumpUntil(() => check.IsCompleted);

            Assert.Equal("new app", File.ReadAllText(Path.Combine(install.Folder, "TheSharpFactory.exe")));
            Assert.NotNull(host.Started);
            Assert.Equal(install.Executable, host.Started.FileName);
            Assert.Equal(modelFile, Assert.Single(host.Started.ArgumentList));
            Assert.Same(app.ViewModel.About, app.ViewModel.CurrentPage);
            Assert.Equal("Version 9.0.0.0 is installed. Restarting…", updates.StatusText);
            //the files were replaced: Cancel is gone
            Assert.True(updates.IsReplacing);
            Assert.False(updates.CanCancel);

            //the real host closes the app here; this one does not, so the window can close at the end of the test
            app.ViewModel.IsBusy = false;
        }
    }

    [AvaloniaFact]
    public void CancelStopsTheDownloadAndChangesNothing()
    {
        using(var app = new AppHarness("1.2.0.3"))
        {
            var install = CreateInstall(Path.Combine(app.Folder, "install"));
            var handler = new FakeHttpHandler();
            handler.Add(_feed, ComposeReleaseJson("v9.0.0.0", FindPackageName(), true));
            PublishPackage(handler, app.Folder, FindPackageName(), null);
            var package = _downloads + FindPackageName();
            handler.HangOn = package;
            var host = new FakeAppHost();
            app.ViewModel.Host = host;
            var updates = app.ViewModel.Updates;
            updates.Http = new HttpClient(handler);
            updates.Location = install;
            updates.TrustedKey = TestSigning.PublicKey;

            var check = updates.CheckAsync(true);
            app.PumpUntil(() => app.ViewModel.Dialog != null);
            app.ViewModel.Dialog.ChooseCommand.Execute(app.ViewModel.Dialog.Buttons[1]);
            app.PumpUntil(() => updates.IsInstalling && handler.Requests.Contains(package));
            var cancel = app.Find<Avalonia.Controls.Button>("CancelUpdateButton");
            Assert.True(cancel.IsVisible);
            app.Screenshot("about-update-downloading");

            cancel.Command.Execute(null);
            app.PumpUntil(() => check.IsCompleted);

            Assert.Equal("The update was cancelled. You still have version 1.2.0.3.", updates.StatusText);
            Assert.False(updates.IsInstalling);
            Assert.False(app.ViewModel.IsBusy);
            Assert.Null(app.ViewModel.Dialog);
            Assert.Null(host.Started);
            Assert.Equal("old app", File.ReadAllText(Path.Combine(install.Folder, "TheSharpFactory.exe")));
            Assert.False(Directory.Exists(Path.Combine(install.Folder, UpdateInstaller.StagingFolderName)));
            Assert.False(app.Find<Avalonia.Controls.Button>("CancelUpdateButton").IsVisible);
        }
    }

    [AvaloniaFact]
    public void UnsavedChangesAreAskedAboutBeforeTheUpdate()
    {
        using(var app = new AppHarness("1.2.0.3"))
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            app.ViewModel.Document.MarkDirty();
            var install = CreateInstall(Path.Combine(app.Folder, "install"));
            var handler = new FakeHttpHandler();
            handler.Add(_feed, ComposeReleaseJson("v9.0.0.0", FindPackageName(), true));
            PublishPackage(handler, app.Folder, FindPackageName(), null);
            var host = new FakeAppHost();
            app.ViewModel.Host = host;
            var updates = app.ViewModel.Updates;
            updates.Http = new HttpClient(handler);
            updates.Location = install;
            updates.TrustedKey = TestSigning.PublicKey;

            var check = updates.CheckAsync(true);
            app.PumpUntil(() => app.ViewModel.Dialog != null);
            app.ViewModel.Dialog.ChooseCommand.Execute(app.ViewModel.Dialog.Buttons[1]);
            app.PumpUntil(() => app.ViewModel.Dialog != null && app.ViewModel.Dialog.Title == "Unsaved changes");
            app.ViewModel.Dialog.Cancel();
            app.PumpUntil(() => check.IsCompleted);

            Assert.Null(host.Started);
            Assert.Equal("old app", File.ReadAllText(Path.Combine(install.Folder, "TheSharpFactory.exe")));
            Assert.Single(handler.Requests);
            Assert.False(app.ViewModel.IsBusy);
        }
    }

    [AvaloniaFact]
    public void AFailedUpdateSaysWhyAndTheAppCarriesOn()
    {
        using(var app = new AppHarness("1.2.0.3"))
        {
            var install = CreateInstall(Path.Combine(app.Folder, "install"));
            var handler = new FakeHttpHandler();
            handler.Add(_feed, ComposeReleaseJson("v9.0.0.0", FindPackageName(), true));
            PublishPackage(handler, app.Folder, FindPackageName(), new string('0', 64));
            var host = new FakeAppHost();
            app.ViewModel.Host = host;
            var updates = app.ViewModel.Updates;
            updates.Http = new HttpClient(handler);
            updates.Location = install;
            updates.TrustedKey = TestSigning.PublicKey;

            var check = updates.CheckAsync(true);
            app.PumpUntil(() => app.ViewModel.Dialog != null);
            app.ViewModel.Dialog.ChooseCommand.Execute(app.ViewModel.Dialog.Buttons[1]);
            app.PumpUntil(() => app.ViewModel.Dialog != null && app.ViewModel.Dialog.Title == "Update failed");

            Assert.Contains("damaged", app.ViewModel.Dialog.Message, StringComparison.Ordinal);
            Assert.False(app.ViewModel.IsBusy);
            Assert.False(updates.IsInstalling);
            Assert.Equal("The update failed. You still have version 1.2.0.3.", updates.StatusText);
            Assert.Null(host.Started);
            app.ViewModel.Dialog.Cancel();
            app.PumpUntil(() => check.IsCompleted);
        }
    }

    [AvaloniaFact]
    public void WhereTheAppCannotUpdateItselfItOffersTheReleasePage()
    {
        using(var app = new AppHarness("1.2.0.3"))
        {
            var handler = new FakeHttpHandler();
            handler.Add(_feed, ComposeReleaseJson("v9.0.0.0", FindPackageName(), true));
            var host = new FakeAppHost();
            app.ViewModel.Host = host;
            var updates = app.ViewModel.Updates;
            updates.Http = new HttpClient(handler);
            updates.Location = new InstallLocation { Problem = "This copy of the app is not a release download, so it cannot update itself." };

            var check = updates.CheckAsync(true);
            app.PumpUntil(() => app.ViewModel.Dialog != null);
            app.ViewModel.Dialog.ChooseCommand.Execute(app.ViewModel.Dialog.Buttons[1]);
            app.PumpUntil(() => app.ViewModel.Dialog != null && app.ViewModel.Dialog.Title == "Update from GitHub");
            Assert.StartsWith("This copy of the app is not a release download", app.ViewModel.Dialog.Message, StringComparison.Ordinal);
            app.ViewModel.Dialog.ChooseCommand.Execute(app.ViewModel.Dialog.Buttons[1]);
            app.PumpUntil(() => check.IsCompleted);

            Assert.Equal("https://github.com/TheSharpFactory/SharpFactory/releases/tag/v9.0.0.0", host.Opened.ToString());
            Assert.Null(host.Started);
            Assert.Single(handler.Requests);
        }
    }
    #endregion

    #region Helpers
    private static string FindPackageName()
    {
        return UpdateChecker.GetPackageName(RuntimeInformation.RuntimeIdentifier);
    }

    /// <summary>
    /// An installer for a test release: it trusts the test key the test releases are signed with.
    /// </summary>
    private static UpdateInstaller CreateInstaller(FakeHttpHandler handler, ReleaseInfo release, InstallLocation install, IProgress<UpdateProgress> progress, UpdateCancellation cancellation)
    {
        return new UpdateInstaller(new HttpClient(handler), release, install, new Version(1, 0), progress, cancellation) { TrustedKey = TestSigning.PublicKey };
    }

    /// <summary>
    /// An unpacked "release" in a folder: the app, the command line, the licenses, and a file of the user's.
    /// </summary>
    private static InstallLocation CreateInstall(string folder)
    {
        Directory.CreateDirectory(Path.Combine(folder, "licenses"));
        File.WriteAllText(Path.Combine(folder, "TheSharpFactory.exe"), "old app");
        File.WriteAllText(Path.Combine(folder, "sharpfactory.exe"), "old cli");
        File.WriteAllText(Path.Combine(folder, "LICENSE"), "old license");
        File.WriteAllText(Path.Combine(folder, "licenses", "Old.txt"), "old");
        File.WriteAllText(Path.Combine(folder, "notes.txt"), "mine");
        return new InstallLocation { Folder = folder, Executable = Path.Combine(folder, "TheSharpFactory.exe") };
    }

    /// <summary>
    /// Builds a package (.zip or .tar.gz by its name) with the new files, and serves it, its SHA256SUMS.txt and
    /// that file's signature (by the test key); a wrong hash makes a damaged download.
    /// </summary>
    private static ReleaseInfo PublishPackage(FakeHttpHandler handler, string workFolder, string packageName, string wrongHash)
    {
        var source = Path.Combine(workFolder, "package-source-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(source, "licenses"));
        File.WriteAllText(Path.Combine(source, "TheSharpFactory.exe"), "new app");
        File.WriteAllText(Path.Combine(source, "sharpfactory.exe"), "new cli");
        File.WriteAllText(Path.Combine(source, "LICENSE"), "new license");
        File.WriteAllText(Path.Combine(source, "licenses", "New.txt"), "new");

        var package = Path.Combine(workFolder, "package-" + Guid.NewGuid().ToString("N"));
        if(packageName.EndsWith(".tar.gz", StringComparison.Ordinal))
        {
            using(var file = File.Create(package))
            {
                using(var gzip = new GZipStream(file, CompressionLevel.Fastest))
                {
                    TarFile.CreateFromDirectory(source, gzip, false);
                }
            }
        }
        else
        {
            ZipFile.CreateFromDirectory(source, package);
        }

        var bytes = File.ReadAllBytes(package);
        var hash = wrongHash ?? Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        handler.Add(_downloads + packageName, bytes);
        var checksums = Encoding.UTF8.GetBytes($"{new string('c', 64)}  sharpfactory-cli-win-x64.zip\n{hash}  {packageName}\n");
        handler.Add(_downloads + "SHA256SUMS.txt", checksums);
        handler.Add(_downloads + "SHA256SUMS.txt.sig", TestSigning.Sign(checksums));

        return new ReleaseInfo
        {
            Version = new Version(9, 0, 0, 0),
            PackageName = packageName,
            PackageUri = new Uri(_downloads + packageName),
            ChecksumsUri = new Uri(_downloads + "SHA256SUMS.txt"),
            SignatureUri = new Uri(_downloads + "SHA256SUMS.txt.sig"),
        };
    }

    /// <summary>
    /// Copies a system program that waits half a minute to <paramref name="program"/>: cmd.exe on Windows (pinging
    /// itself), sleep on Linux and macOS.
    /// </summary>
    private static ProcessStartInfo StartSomethingLong(string program)
    {
        if(OperatingSystem.IsWindows())
        {
            File.Copy(Path.Combine(Environment.SystemDirectory, "cmd.exe"), program, true);
            return new ProcessStartInfo(program, "/c ping -n 30 127.0.0.1 >nul") { UseShellExecute = false, CreateNoWindow = true };
        }

        File.Copy("/bin/sleep", program, true);
        File.SetUnixFileMode(program, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        return new ProcessStartInfo(program, "30") { UseShellExecute = false, CreateNoWindow = true };
    }

    /// <summary>
    /// Makes replacing LICENSE fail: Windows refuses to rename a file open without sharing; on Linux and macOS a left-over
    /// "LICENSE.sf-old" folder that cannot be emptied (it is read-only), which the swap deletes first.
    /// </summary>
    private static IDisposable BlockReplacingTheLicense(string folder)
    {
        if(OperatingSystem.IsWindows())
            return new FileStream(Path.Combine(folder, "LICENSE"), FileMode.Open, FileAccess.Read, FileShare.None);

        return new ReadOnlyFolder(Path.Combine(folder, "LICENSE" + UpdateInstaller.OldSuffix));
    }

    private static List<string> ListFolder(string folder)
    {
        var names = new List<string>();
        foreach(var entry in Directory.EnumerateFileSystemEntries(folder))
            names.Add(Path.GetFileName(entry));

        names.Sort(StringComparer.Ordinal);
        return names;
    }
    #endregion
}

/// <summary>
/// Answers HTTP requests from a table, without the network; records what was asked.
/// </summary>
internal sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly Dictionary<string, byte[]> _responses = new Dictionary<string, byte[]>(StringComparer.Ordinal);
    private readonly Lock _syncRoot = new Lock();

    public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
    public bool ThrowNetworkError { get; set; }
    public List<string> Requests { get; } = new List<string>();
    public List<string> UserAgents { get; } = new List<string>();

    public void Add(string uri, string text)
    {
        _responses[uri] = Encoding.UTF8.GetBytes(text);
    }

    public void Add(string uri, byte[] bytes)
    {
        _responses[uri] = bytes;
    }

    public byte[] Get(string uri)
    {
        return _responses[uri];
    }

    /// <summary>
    /// The answer to this address starts, then the connection drops while it is read.
    /// </summary>
    public string DropWhileReading { get; set; }

    /// <summary>
    /// The answer to this address never comes; only the request's cancellation ends the wait.
    /// </summary>
    public string HangOn { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri.ToString();
        lock(_syncRoot)
        {
            Requests.Add(uri);
            UserAgents.Add(request.Headers.UserAgent.ToString());
        }

        if(ThrowNetworkError)
            throw new HttpRequestException("No such host is known.");

        if(string.Equals(uri, HangOn, StringComparison.Ordinal))
            await Task.Delay(Timeout.Infinite, cancellationToken);

        if(Status != HttpStatusCode.OK)
            return new HttpResponseMessage(Status) { ReasonPhrase = Status.ToString() };

        if(string.Equals(uri, DropWhileReading, StringComparison.Ordinal))
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new DroppedContent() };

        if(!_responses.TryGetValue(uri, out var body))
            return new HttpResponseMessage(HttpStatusCode.NotFound);

        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(body) };
    }
}

/// <summary>
/// An answer whose connection is lost after a few bytes, as HttpClient reports it (an IOException).
/// </summary>
internal sealed class DroppedContent : HttpContent
{
    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
    {
        await stream.WriteAsync(Encoding.UTF8.GetBytes("{\"tag_"));
        throw new IOException("The response ended prematurely.");
    }

    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return false;
    }
}

/// <summary>
/// Records the address the app would open and the program it would start, instead of doing either.
/// </summary>
internal sealed class FakeAppHost : IAppHost
{
    public Uri Opened { get; private set; }
    public ProcessStartInfo Started { get; private set; }

    public Task OpenUriAsync(Uri uri)
    {
        Opened = uri;
        return Task.CompletedTask;
    }

    public bool StartAndExit(ProcessStartInfo startInfo)
    {
        Started = startInfo;
        return true;
    }
}

/// <summary>
/// Keeps every stage reported, synchronously (Progress&lt;T&gt; would post them later).
/// </summary>
internal sealed class RecordingProgress : IProgress<UpdateProgress>
{
    private readonly Lock _syncRoot = new Lock();

    public List<UpdateStage> Stages { get; } = new List<UpdateStage>();

    public void Report(UpdateProgress value)
    {
        lock(_syncRoot)
        {
            Stages.Add(value.Stage);
        }
    }
}

/// <summary>
/// A folder with a file in it that cannot be deleted while the folder is read-only (Linux, macOS); writable again when
/// disposed, so the test's folder can be deleted.
/// </summary>
[System.Runtime.Versioning.UnsupportedOSPlatform("windows")]
internal sealed class ReadOnlyFolder : IDisposable
{
    private readonly string _path;

    public ReadOnlyFolder(string path)
    {
        _path = path;
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, "left over"), "left over");
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserExecute);
    }

    public void Dispose()
    {
        File.SetUnixFileMode(_path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }
}

/// <summary>
/// The key the test releases are signed with, as release.yml signs the real ones (ECDSA P-256, SHA-256, DER).
/// </summary>
internal static class TestSigning
{
    private static readonly ECDsa _key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

    public static string PublicKey
    {
        get
        {
            return _key.ExportSubjectPublicKeyInfoPem();
        }
    }

    public static byte[] Sign(byte[] data)
    {
        return _key.SignData(data, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
    }
}
