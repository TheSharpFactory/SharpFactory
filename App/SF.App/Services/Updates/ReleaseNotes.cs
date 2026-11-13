#region Usings
using System;
using System.Text;
#endregion

namespace SF.App.Services.Updates;

/// <summary>
/// The release text (Markdown) as plain text for the question the app asks before it updates.
/// </summary>
internal static class ReleaseNotes
{
    /// <summary>
    /// The release text as plain text for the update question: what comes before its Downloads section, without the
    /// Markdown marks and tables, cut at a line end after <paramref name="maxLength"/> characters.
    /// </summary>
    public static string Summarize(string markdown, int maxLength)
    {
        if(string.IsNullOrWhiteSpace(markdown))
            return null;

        var text = new StringBuilder();
        var lastWasBlank = true;
        foreach(var rawLine in markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var line = rawLine.Trim();
            var isHeading = line.StartsWith('#');
            line = line.TrimStart('#').Trim().Replace("**", "", StringComparison.Ordinal).Replace("`", "", StringComparison.Ordinal);

            if(isHeading && string.Equals(line, "Downloads", StringComparison.OrdinalIgnoreCase))
                break;

            if(line.StartsWith('|'))
                continue;

            if(line.Length == 0)
            {
                if(!lastWasBlank)
                    text.Append('\n');

                lastWasBlank = true;
                continue;
            }

            if(text.Length + line.Length > maxLength && text.Length > 0)
            {
                text.Append('…');
                break;
            }

            text.Append(line).Append('\n');
            lastWasBlank = false;
        }

        var result = text.ToString().Trim();
        return result.Length == 0 ? null : result;
    }
}
