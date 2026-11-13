#region Usings
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
#endregion

namespace SF.App.Services.Updates;

/// <summary>
/// The release text (Markdown) as plain text for the question the app asks before it updates.
/// </summary>
internal static class ReleaseNotes
{
    private static readonly Regex _link = new Regex(@"\[([^\]]+)\]\([^)]*\)", RegexOptions.Compiled);

    /// <summary>
    /// The release text as plain text for the update question: what comes before its Downloads section. The lines
    /// a paragraph or a list item was wrapped over are joined again, list items start with "•", headings stand on
    /// their own line, and Markdown marks, links and tables are left out. Cut after the block that passes
    /// <paramref name="maxLength"/> characters.
    /// </summary>
    public static string Summarize(string markdown, int maxLength)
    {
        if(string.IsNullOrWhiteSpace(markdown))
            return null;

        var blocks = ReadBlocks(markdown);
        var text = new StringBuilder();
        for(var i = 0; i < blocks.Count; i++)
        {
            if(i > 0 && text.Length + blocks[i].Text.Length > maxLength)
            {
                text.Append("\n…");
                break;
            }

            if(i > 0)
                text.Append(IsTight(blocks[i - 1], blocks[i]) ? "\n" : "\n\n");

            text.Append(blocks[i].Text);
        }

        var result = text.ToString().Trim();
        return result.Length == 0 ? null : result;
    }

    /// <summary>
    /// A heading sits right on what it heads, and list items on each other; everything else has a blank line between.
    /// </summary>
    private static bool IsTight(NotesBlock previous, NotesBlock next)
    {
        return previous.IsHeading || (previous.IsListItem && next.IsListItem);
    }

    private static List<NotesBlock> ReadBlocks(string markdown)
    {
        var blocks = new List<NotesBlock>();
        NotesBlock current = null;
        foreach(var rawLine in markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var line = rawLine.Trim();
            if(line.StartsWith('#'))
            {
                var heading = CleanInline(line.TrimStart('#'));
                if(string.Equals(heading, "Downloads", StringComparison.OrdinalIgnoreCase))
                    break;

                current = null;
                if(heading.Length > 0)
                    blocks.Add(new NotesBlock { Text = heading, IsHeading = true });

                continue;
            }

            if(line.Length == 0 || line.StartsWith('|') || line == "---")
            {
                current = null;
                continue;
            }

            if(line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal))
            {
                current = new NotesBlock { Text = "• " + CleanInline(line.Substring(2)), IsListItem = true };
                blocks.Add(current);
                continue;
            }

            //a wrapped line: it continues the paragraph or list item above it
            if(current != null)
            {
                current.Text += " " + CleanInline(line);
                continue;
            }

            current = new NotesBlock { Text = CleanInline(line) };
            blocks.Add(current);
        }

        return blocks;
    }

    /// <summary>
    /// Bold and code marks out, a link as its text.
    /// </summary>
    private static string CleanInline(string text)
    {
        var plain = text.Replace("**", "", StringComparison.Ordinal).Replace("`", "", StringComparison.Ordinal);
        return _link.Replace(plain, "$1").Trim();
    }

    /// <summary>
    /// A paragraph, a list item or a heading of the release text.
    /// </summary>
    private sealed class NotesBlock
    {
        public string Text { get; set; }
        public bool IsHeading { get; set; }
        public bool IsListItem { get; set; }
    }
}
