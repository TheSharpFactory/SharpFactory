#region Usings
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using SF.App.ViewModels;
using SF.App.Views;
using SF.Engine.Sync;
using System;
using System.Threading.Tasks;
using Xunit;
#endregion

namespace SF.App.Tests;

/// <summary>
/// The UI review fixes of 1.2.0.1: grey zero counts on Sync, an amber sign for warnings, a status line that does not
/// outlive the change it reports, and the style cleanups (the same colours as before).
/// </summary>
public sealed class UiReviewFixesTests
{
    private static Color Resource(string key)
    {
        Assert.True(Application.Current.TryGetResource(key, Application.Current.ActualThemeVariant, out var value), key);
        return ((SolidColorBrush)value).Color;
    }

    private static Color BackgroundOf(Border border)
    {
        return ((ISolidColorBrush)border.Background).Color;
    }

    private static Color ForegroundOf(Border border)
    {
        return ((ISolidColorBrush)((TextBlock)border.Child).Foreground).Color;
    }

    private static SyncReport PreviewWithOneAddedFile()
    {
        var report = new SyncReport { Success = true, IsPreview = true, UnchangedFileCount = 3 };
        report.FileChanges.Add(new SyncFileChange { FullPath = @"C:\Out\A.cs", RelativePath = @"Data\A.cs", Kind = SyncFileChangeKind.Added, NewContent = "class A { }" });
        return report;
    }

    [AvaloniaFact]
    public void AZeroCountOnSyncStaysGrey()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            app.ViewModel.Navigate(app.ViewModel.Sync);
            var page = app.ViewModel.Sync;
            //the solution search picks a target when it ends, and a new target clears the report: it ends first
            app.PumpUntil(() => !page.IsScanning);
            page.Report = PreviewWithOneAddedFile();
            app.Pump();

            Assert.True(page.HasAdded);
            Assert.False(page.HasChanged);
            Assert.False(page.HasDeleted);

            //one added file: that count is green, the zero counts are plain pills
            var added = app.Find<Border>("AddedCount");
            var changed = app.Find<Border>("ChangedCount");
            var deleted = app.Find<Border>("DeletedCount");
            Assert.Equal(Resource("InsertedBackground"), BackgroundOf(added));
            Assert.Equal(Resource("SuccessText"), ForegroundOf(added));
            Assert.Equal(Resource("BadgeBackground"), BackgroundOf(changed));
            Assert.Equal(Resource("BadgeBackground"), BackgroundOf(deleted));
            Assert.NotEqual(Resource("ErrorText"), ForegroundOf(deleted));
            Assert.Equal("−0 deleted", ((TextBlock)deleted.Child).Text);

            //no report: nothing is coloured
            page.Report = null;
            Assert.False(page.HasAdded);
        }
    }

    [AvaloniaFact]
    public async Task AWarningShowsAnAmberSignAndAnErrorARedOne()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());

            //Delete model asks a warning question
            var delete = app.ViewModel.DeleteSelectedModelAsync();
            app.Pump();
            Assert.True(app.ViewModel.Dialog.HasWarningSign);
            Assert.False(app.ViewModel.Dialog.HasErrorSign);
            var warningSign = app.Find<PathIcon>("DialogWarningSign");
            Assert.True(warningSign.IsVisible);
            Assert.False(app.Find<PathIcon>("DialogErrorSign").IsVisible);
            Assert.Equal(Resource("WarningText"), ((ISolidColorBrush)warningSign.Foreground).Color);
            app.ViewModel.Dialog.Cancel();
            await delete;

            //an error message
            var message = app.ViewModel.ShowMessageAsync("Unable to save", "The disk is full.", true);
            app.Pump();
            Assert.True(app.ViewModel.Dialog.HasErrorSign);
            Assert.False(app.ViewModel.Dialog.HasWarningSign);
            var errorSign = app.Find<PathIcon>("DialogErrorSign");
            Assert.True(errorSign.IsVisible);
            Assert.False(app.Find<PathIcon>("DialogWarningSign").IsVisible);
            Assert.Equal(Resource("ErrorText"), ((ISolidColorBrush)errorSign.Foreground).Color);
            app.ViewModel.Dialog.Cancel();
            await message;

            //a plain message has no sign
            var plain = app.ViewModel.ShowMessageAsync("Update cancelled", "You need to select a model first.", false);
            app.Pump();
            Assert.False(app.Find<PathIcon>("DialogWarningSign").IsVisible);
            Assert.False(app.Find<PathIcon>("DialogErrorSign").IsVisible);
            app.ViewModel.Dialog.Cancel();
            await plain;
        }
    }

    [AvaloniaFact]
    public async Task TheStatusLineClearsWhenTheModelGetsUnsavedChanges()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            Assert.StartsWith("Opened", app.ViewModel.StatusText, StringComparison.Ordinal);

            //"Opened" is no longer the news once something changed
            app.ViewModel.SelectedModel.Layers.Entity.Namespace = "Renamed.Entities";
            Assert.True(app.ViewModel.Document.IsDirty);
            Assert.Null(app.ViewModel.StatusText);

            Assert.True(await app.ViewModel.SaveDocumentAsync());
            Assert.StartsWith("Saved", app.ViewModel.StatusText, StringComparison.Ordinal);
            app.Pump();
            Assert.False(app.Find<TextBlock>("StatusLine").Text == null);

            //"Saved" goes with the next change too
            app.ViewModel.SelectedModel.Layers.Entity.Namespace = "Renamed.Again";
            Assert.Null(app.ViewModel.StatusText);
            app.Pump();
            Assert.True(string.IsNullOrEmpty(app.Find<TextBlock>("StatusLine").Text));
        }
    }

    [AvaloniaFact]
    public async Task AMessageAboutTheChangeThatDirtiedTheModelStays()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            var name = app.ViewModel.SelectedModel.Name;

            //deleting dirties a saved model, and the message is set after it: it is shown
            var delete = app.ViewModel.DeleteSelectedModelAsync();
            app.Pump();
            app.ViewModel.Dialog.ChooseCommand.Execute(app.ViewModel.Dialog.Buttons.Find(b => b.Result == DialogResults.Yes));
            await delete;
            Assert.True(app.ViewModel.Document.IsDirty);
            Assert.Equal($"Deleted model {name}", app.ViewModel.StatusText);
        }
    }

    [AvaloniaFact]
    public void TheMergedStylesGiveTheSameColours()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());

            //Sync's status chips: the added colour now comes from the rule shared with the pills
            app.ViewModel.Navigate(app.ViewModel.Sync);
            var page = app.ViewModel.Sync;
            app.PumpUntil(() => !page.IsScanning);
            page.Report = PreviewWithOneAddedFile();
            page.ChangeFilters = ChangeFilterOption.For(page.Report);
            page.ChangeFilter = page.ChangeFilters[0];
            app.Pump();
            Border chip = null;
            foreach(var border in app.FindAll<Border>())
            {
                if(border.Classes.Contains("chip") && border.Classes.Contains("added"))
                    chip = border;
            }

            Assert.NotNull(chip);
            Assert.Equal(Resource("InsertedBackground"), BackgroundOf(chip));
            Assert.Equal(Resource("SuccessText"), ForegroundOf(chip));

            //Models: the kind label is a pill now, with the badge's look
            var models = app.ViewModel.Models;
            app.ViewModel.Navigate(models);
            models.SelectedNode = ParityTests.FindNode(models, "Warehouse");
            app.Pump();
            Border kind = null;
            foreach(var border in app.FindAll<Border>())
            {
                if(border.Classes.Contains("pill") && border.Child is TextBlock text && text.Text == models.Details.Kind)
                    kind = border;
            }

            Assert.NotNull(kind);
            Assert.Equal(Resource("BadgeBackground"), BackgroundOf(kind));
            Assert.Equal(new CornerRadius(10), kind.CornerRadius);
            Assert.Equal(new Thickness(8, 2), kind.Padding);
            Assert.Equal(12, ((TextBlock)kind.Child).FontSize);

            //Layers: one header per table, with its four column titles
            app.ViewModel.Navigate(app.ViewModel.Layers);
            app.Pump();
            var headers = app.FindAll<LayerTableHeader>();
            Assert.Equal(2, headers.Count);
            foreach(var header in headers)
            {
                var titles = new System.Collections.Generic.List<string>();
                foreach(var visual in header.GetVisualDescendants())
                {
                    if(visual is TextBlock title)
                        titles.Add(title.Text);
                }

                Assert.Equal(new[] { "LAYER", "PROJECT", "NAMESPACE", "RESULT" }, titles);
            }
        }
    }
}
