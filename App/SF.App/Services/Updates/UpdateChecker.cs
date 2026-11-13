#region Usings
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
#endregion

namespace SF.App.Services.Updates;

/// <summary>
/// Asks GitHub for the latest release of the app (drafts and pre-releases are never "latest") and compares its
/// version with this one. The request sends nothing but the app's name and version (the User-Agent GitHub requires).
/// </summary>
internal sealed class UpdateChecker
{
    /// <summary>
    /// Replaces the GitHub address (tests point it at a local server).
    /// </summary>
    public const string FeedVariable = "SHARPFACTORY_UPDATE_FEED";

    public const string ChecksumsFileName = "SHA256SUMS.txt";

    private const string _gitHubFeed = "https://api.github.com/repos/TheSharpFactory/SharpFactory/releases/latest";
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

    private readonly HttpClient _http;
    private readonly Uri _feed;
    private readonly Version _currentVersion;
    private readonly string _packageName;

    public UpdateChecker(HttpClient http, Uri feed, Version currentVersion, string packageName)
    {
        _http = http;
        _feed = feed;
        _currentVersion = currentVersion;
        _packageName = packageName;
    }

    /// <summary>
    /// The GitHub API address of the latest release, or the one SHARPFACTORY_UPDATE_FEED gives.
    /// </summary>
    public static Uri GetFeedUri()
    {
        var feed = Environment.GetEnvironmentVariable(FeedVariable);
        return new Uri(string.IsNullOrWhiteSpace(feed) ? _gitHubFeed : feed);
    }

    /// <summary>
    /// The name of the app download for an OS and processor (release.yml makes these), or null when there is none.
    /// </summary>
    public static string GetPackageName(string runtimeIdentifier)
    {
        switch(runtimeIdentifier)
        {
            case "win-x64":
            case "win-arm64":
            case "osx-x64":
            case "osx-arm64":
                return $"TheSharpFactory-{runtimeIdentifier}.zip";
            case "linux-x64":
                return "TheSharpFactory-linux-x64.tar.gz";
            default:
                return null;
        }
    }

    /// <summary>
    /// Reads "v1.2.0.3" or "1.2.0.3" as a four-part version ("1.2" is 1.2.0.0).
    /// </summary>
    public static bool TryParseVersion(string text, out Version version)
    {
        version = null;
        if(string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.Trim();
        if(trimmed.StartsWith('v') || trimmed.StartsWith('V'))
            trimmed = trimmed.Substring(1);

        if(!Version.TryParse(trimmed, out var parsed))
            return false;

        version = new Version(parsed.Major, parsed.Minor, Math.Max(parsed.Build, 0), Math.Max(parsed.Revision, 0));
        return true;
    }

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            using(var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                timeout.CancelAfter(_timeout);
                using(var request = CreateRequest(_feed, _currentVersion))
                {
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
                    using(var response = await _http.SendAsync(request, timeout.Token))
                    {
                        if(!response.IsSuccessStatusCode)
                            return Fail($"GitHub answered {(int)response.StatusCode} ({response.ReasonPhrase}).");

                        var json = await response.Content.ReadAsStringAsync(timeout.Token);
                        return ReadRelease(json, _currentVersion, _packageName);
                    }
                }
            }
        }
        catch(OperationCanceledException) when(!cancellationToken.IsCancellationRequested)
        {
            return Fail("GitHub did not answer in time.");
        }
        catch(HttpRequestException ex)
        {
            //also a connection lost while the answer was read: SendAsync reads it in full and reports that as this
            return Fail($"GitHub could not be reached ({ex.Message}).");
        }
    }

    /// <summary>
    /// A GET that names the app and its version: GitHub refuses requests without a User-Agent.
    /// </summary>
    public static HttpRequestMessage CreateRequest(Uri uri, Version currentVersion)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("TheSharpFactory", currentVersion.ToString()));
        return request;
    }

    /// <summary>
    /// Reads GitHub's release JSON: tag_name, html_url, body and the downloads (assets).
    /// </summary>
    public static UpdateCheckResult ReadRelease(string json, Version currentVersion, string packageName)
    {
        try
        {
            using(var document = JsonDocument.Parse(json))
            {
                var root = document.RootElement;
                if(root.ValueKind != JsonValueKind.Object || !TryParseVersion(GetString(root, "tag_name"), out var version))
                    return Fail("GitHub's answer has no release version.");

                var release = new ReleaseInfo
                {
                    Version = version,
                    Notes = GetString(root, "body"),
                    PageUri = GetUri(root, "html_url"),
                    PackageName = packageName,
                };

                if(root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
                {
                    foreach(var asset in assets.EnumerateArray())
                    {
                        var name = GetString(asset, "name");
                        if(packageName != null && string.Equals(name, packageName, StringComparison.Ordinal))
                            release.PackageUri = GetUri(asset, "browser_download_url");
                        else if(string.Equals(name, ChecksumsFileName, StringComparison.Ordinal))
                            release.ChecksumsUri = GetUri(asset, "browser_download_url");
                        else if(string.Equals(name, UpdateSignature.SignatureFileName, StringComparison.Ordinal))
                            release.SignatureUri = GetUri(asset, "browser_download_url");
                    }
                }

                var status = version > currentVersion ? UpdateCheckStatus.Available : UpdateCheckStatus.UpToDate;
                return new UpdateCheckResult { Status = status, Release = release };
            }
        }
        catch(JsonException)
        {
            return Fail("GitHub's answer could not be read.");
        }
    }

    private static string GetString(JsonElement element, string name)
    {
        if(element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            return value.GetString();

        return null;
    }

    /// <summary>
    /// An https address (http only for a local test server), or null.
    /// </summary>
    private static Uri GetUri(JsonElement element, string name)
    {
        var text = GetString(element, name);
        if(text == null || !Uri.TryCreate(text, UriKind.Absolute, out var uri))
            return null;

        if(string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) || uri.IsLoopback)
            return uri;

        return null;
    }

    private static UpdateCheckResult Fail(string problem)
    {
        return new UpdateCheckResult { Status = UpdateCheckStatus.Failed, Problem = problem };
    }
}
