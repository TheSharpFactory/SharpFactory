#region Usings
using Avalonia.Controls;
using Avalonia.Input.Platform;
using SF.Engine.Settings;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#endregion

namespace SF.App.Views;

/// <summary>
/// Remembers where the user dragged a split pane (the width of the grid's first column) in the app settings.
/// </summary>
internal static class PaneWidths
{
    public const string ModelsTree = "Models.Tree";
    public const string SyncFiles = "Sync.Files";

    /// <summary>
    /// Puts the first column back to the remembered width, when there is one and it still fits the column's limits.
    /// </summary>
    public static void Restore(Grid grid, string key, AppSettings settings)
    {
        if(grid == null || settings?.PaneWidths == null || grid.ColumnDefinitions.Count == 0)
            return;

        if(!settings.PaneWidths.TryGetValue(key, out var width))
            return;

        var column = grid.ColumnDefinitions[0];
        if(width < column.MinWidth || width > column.MaxWidth)
            return;

        column.Width = new GridLength(width);
    }

    /// <summary>
    /// Keeps the first column's width after a drag, and saves the settings.
    /// </summary>
    public static void Remember(Grid grid, string key, AppSettings settings)
    {
        if(grid == null || settings == null || grid.ColumnDefinitions.Count == 0)
            return;

        var width = grid.ColumnDefinitions[0].ActualWidth;
        if(width <= 0)
            return;

        settings.PaneWidths ??= new Dictionary<string, double>();
        settings.PaneWidths[key] = Math.Round(width);
        AppSettingsStore.Save(settings);
    }
}

/// <summary>
/// Copies a log to the clipboard and says so on the button for two seconds.
/// </summary>
internal static class LogCopier
{
    public static async Task CopyAsync(Control source, TextBlock label, string text)
    {
        var clipboard = TopLevel.GetTopLevel(source)?.Clipboard;
        if(clipboard == null || string.IsNullOrEmpty(text))
            return;

        await ClipboardExtensions.SetTextAsync(clipboard, text);
        if(label == null)
            return;

        label.Text = "Copied";
        await Task.Delay(2000);
        label.Text = "Copy";
    }
}
