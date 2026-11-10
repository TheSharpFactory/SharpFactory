#region Usings
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using SF.App.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
#endregion

namespace SF.App.Views;

/// <summary>
/// Tints the inserted and deleted lines of a diff shown in the code editor.
/// </summary>
internal sealed class DiffLineBackground : IBackgroundRenderer
{
    public List<DiffLineViewModel> Lines { get; set; } = new List<DiffLineViewModel>();
    public bool IsTinted { get; set; }

    public KnownLayer Layer
    {
        get
        {
            return KnownLayer.Background;
        }
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if(!IsTinted || !textView.VisualLinesValid)
            return;

        var inserted = DiffBrushes.Find("InsertedBackground");
        var deleted = DiffBrushes.Find("DeletedBackground");
        foreach(var visualLine in textView.VisualLines)
        {
            var index = visualLine.FirstDocumentLine.LineNumber - 1;
            if(index < 0 || index >= Lines.Count)
                continue;

            var brush = DiffBrushes.ForKind(Lines[index].Kind, inserted, deleted);
            if(brush == null)
                continue;

            var top = visualLine.VisualTop - textView.ScrollOffset.Y;
            drawingContext.FillRectangle(brush, new Rect(0, top, textView.Bounds.Width, visualLine.Height));
        }
    }
}

/// <summary>
/// The diff gutter: the line's number in the new file and a + or − sign, tinted like the line.
/// </summary>
internal sealed class DiffMargin : AbstractMargin
{
    private const double _width = 92;
    private static readonly Typeface _typeface = new Typeface("Cascadia Mono,Consolas,Menlo,DejaVu Sans Mono,monospace");

    public List<DiffLineViewModel> Lines { get; set; } = new List<DiffLineViewModel>();
    public bool IsTinted { get; set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(_width, 0);
    }

    protected override void OnTextViewChanged(TextView oldTextView, TextView newTextView)
    {
        if(oldTextView != null)
        {
            oldTextView.VisualLinesChanged -= OnVisualLinesChanged;
            oldTextView.ScrollOffsetChanged -= OnVisualLinesChanged;
        }

        base.OnTextViewChanged(oldTextView, newTextView);

        if(newTextView != null)
        {
            newTextView.VisualLinesChanged += OnVisualLinesChanged;
            newTextView.ScrollOffsetChanged += OnVisualLinesChanged;
        }

        InvalidateVisual();
    }

    private void OnVisualLinesChanged(object sender, EventArgs e)
    {
        InvalidateVisual();
    }

    private static void DrawRight(DrawingContext context, string text, double right, double y, double fontSize, IBrush brush)
    {
        if(string.IsNullOrEmpty(text))
            return;

        var formatted = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeface, fontSize, brush);
        context.DrawText(formatted, new Point(right - formatted.Width, y));
    }

    public override void Render(DrawingContext context)
    {
        var textView = TextView;
        if(textView == null || !textView.VisualLinesValid)
            return;

        var subtle = DiffBrushes.Find("SubtleText") ?? Brushes.Gray;
        var inserted = DiffBrushes.Find("InsertedBackground");
        var deleted = DiffBrushes.Find("DeletedBackground");
        var fontSize = 12.0;

        //the numbers sit on the first row of a wrapped line, whatever the line's height
        var probe = new FormattedText("0", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeface, fontSize, subtle);
        var offset = (textView.DefaultLineHeight - probe.Height) / 2;
        foreach(var visualLine in textView.VisualLines)
        {
            var index = visualLine.FirstDocumentLine.LineNumber - 1;
            if(index < 0 || index >= Lines.Count)
                continue;

            var line = Lines[index];
            var top = visualLine.VisualTop - textView.ScrollOffset.Y;
            var sign = "";
            if(IsTinted)
            {
                var tint = DiffBrushes.ForKind(line.Kind, inserted, deleted);
                if(tint != null)
                    context.FillRectangle(tint, new Rect(0, top, _width, visualLine.Height));

                sign = DiffBrushes.Sign(line.Kind);
            }

            var y = top + offset;

            if(IsTinted)
            {
                //old number, new number, sign
                DrawRight(context, line.OldNumber, 34, y, fontSize, subtle);
                DrawRight(context, line.Number, 68, y, fontSize, subtle);
            }
            else
            {
                DrawRight(context, line.Number ?? line.OldNumber, 68, y, fontSize, subtle);
            }

            if(sign.Length > 0)
            {
                var signText = new FormattedText(sign, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeface, fontSize + 1, subtle);
                context.DrawText(signText, new Point(78, y - 1));
            }
        }
    }
}

/// <summary>
/// The theme brushes the diff uses (the app is dark only).
/// </summary>
internal static class DiffBrushes
{
    public static IBrush Find(string key)
    {
        var application = Application.Current;
        if(application == null)
            return null;

        if(application.TryGetResource(key, application.ActualThemeVariant, out var value))
            return value as IBrush;

        return null;
    }

    public static IBrush ForKind(string kind, IBrush inserted, IBrush deleted)
    {
        if(string.Equals(kind, "Inserted", StringComparison.Ordinal))
            return inserted;

        if(string.Equals(kind, "Deleted", StringComparison.Ordinal))
            return deleted;

        return null;
    }

    public static string Sign(string kind)
    {
        if(string.Equals(kind, "Inserted", StringComparison.Ordinal))
            return "+";

        if(string.Equals(kind, "Deleted", StringComparison.Ordinal))
            return "-";

        return "";
    }
}
