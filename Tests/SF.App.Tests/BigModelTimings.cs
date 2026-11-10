#region Usings
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using SF.App.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xunit;
#endregion

namespace SF.App.Tests;

/// <summary>
/// The app with a large model (the test database's: 185 tables, 1,216 generated files): how long what a user does
/// takes. Runs when SHARPFACTORY_TEST_EDMJ names that model; with SF_TIMINGS set to a file it writes the times there.
/// The limits are generous: they catch something becoming many times slower, not small changes.
/// </summary>
public sealed class BigModelTimings
{
    [AvaloniaFact]
    public async Task ALargeModelStaysResponsive()
    {
        var reference = Environment.GetEnvironmentVariable("SHARPFACTORY_TEST_EDMJ");
        if(string.IsNullOrWhiteSpace(reference) || !File.Exists(reference))
            Assert.Skip("SHARPFACTORY_TEST_EDMJ is not set to the test database's model file.");

        var times = new List<string>();
        using(var app = new AppHarness())
        {
            var file = Path.Combine(app.Folder, "big.edmj");
            File.Copy(reference, file);
            var watch = Stopwatch.StartNew();

            Assert.True(app.ViewModel.OpenFile(file));
            app.Pump();
            Record(times, "open the file and show the tree", watch, 3000);

            var page = app.ViewModel.Models;
            foreach(var text in new[] { "b", "be", "bet", "bett" })
            {
                page.SearchText = text;
                app.Pump();
                Record(times, $"search \"{text}\" ({page.MatchText})", watch, 1500);
            }

            page.ClearSearchCommand.Execute(null);
            app.Pump();
            Record(times, "clear the search", watch, 1500);

            var table = FirstTable(page);
            page.SelectedNode = table;
            app.Pump();
            Record(times, $"select a table ({page.CodeFiles.Count} code files)", watch, 1500);

            app.FindAll<TabControl>()[0].SelectedIndex = 1;
            app.Pump();
            Record(times, "show its generated code", watch, 1500);

            app.ViewModel.Navigate(app.ViewModel.Sync);
            app.Pump();
            Record(times, "open Sync", watch, 1500);

            var sync = app.ViewModel.Sync;
            sync.UseNewSolutionCommand.Execute(null);
            await sync.PreviewCommand.ExecuteAsync(null);
            app.Pump();
            Assert.True(sync.Report.Success, sync.Report.ErrorMessage);
            Record(times, $"preview ({sync.Report.FileChanges.Count} files)", watch, 15000);

            sync.SelectedFileChange = sync.FileChanges[sync.FileChanges.Count / 2];
            app.Pump();
            Record(times, "select a file in the middle", watch, 1500);

            sync.ChangeSearch = "Repository";
            app.Pump();
            Record(times, $"filter the files ({sync.FileChanges.Count} left)", watch, 1500);

            sync.ChangeSearch = null;
            app.Pump();
            Record(times, "clear the filter", watch, 1500);

            app.FindAll<TabControl>()[0].SelectedIndex = 1;
            app.Pump();
            Record(times, $"show the log ({sync.Progress.Entries.Count} lines)", watch, 1500);
        }

        var output = Environment.GetEnvironmentVariable("SF_TIMINGS");
        if(!string.IsNullOrWhiteSpace(output))
            File.WriteAllLines(output, times);
    }

    private static void Record(List<string> times, string what, Stopwatch watch, int limitMs)
    {
        var elapsed = watch.ElapsedMilliseconds;
        times.Add($"{elapsed,7} ms  {what}");
        Assert.True(elapsed < limitMs, $"{what}: {elapsed} ms (limit {limitMs} ms)");
        watch.Restart();
    }

    private static ModelTreeNode FirstTable(ModelsPageViewModel page)
    {
        foreach(var root in page.Roots)
        {
            foreach(var child in root.AllChildren)
            {
                if(child.Kind == ModelNodeKind.Table)
                    return child;

                foreach(var grandChild in child.AllChildren)
                {
                    if(grandChild.Kind == ModelNodeKind.Table)
                        return grandChild;
                }
            }
        }

        throw new InvalidOperationException("No table in the model.");
    }
}
