#region Usings
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using SF.App.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
#endregion

namespace SF.App.Tests;

/// <summary>
/// Popups give long text room: the dialogs are wider, their message and details scroll with a visible bar, and the
/// title and buttons stay on screen however much text there is.
/// </summary>
public sealed class PopupLayoutTests
{
    [AvaloniaFact]
    public void LongReleaseNotesScrollInsideAWiderDialog()
    {
        using(var app = new AppHarness())
        {
            var dialog = NewDialog("An update is available", "Version 9.0.0.0 is ready (you have 1.2.0.3). Update now downloads it, replaces this copy and restarts the app.", 60);
            var pending = app.ViewModel.ShowDialogAsync(dialog);
            app.Screenshot("50-dialog-long-notes");

            var card = DialogCard(app);
            Assert.True(card.Bounds.Width > 700, "the dialog uses the wider size for long text: " + card.Bounds.Width);

            var details = app.Find<ScrollViewer>("DialogDetailsScroll");
            Assert.True(details.Extent.Height > details.Viewport.Height, "the release notes scroll");
            Assert.False(details.AllowAutoHide);
            AssertOnScreen(app, "Update now");

            dialog.ChooseCommand.Execute(dialog.Buttons[0]);
            Assert.True(pending.IsCompleted);
        }
    }

    [AvaloniaFact]
    public void ALongMessageScrollsAndKeepsTheButtonsOnScreenInASmallWindow()
    {
        using(var app = new AppHarness())
        {
            app.Window.Width = 1000;
            app.Window.Height = 640;
            var message = string.Join(" ", Enumerable.Repeat("This message is much longer than the window is tall, so it scrolls.", 60));
            var dialog = NewDialog("A long message", message, 30);
            var pending = app.ViewModel.ShowDialogAsync(dialog);
            app.Screenshot("51-dialog-small-window");

            var body = app.Find<ScrollViewer>("DialogBody");
            Assert.True(body.Extent.Height > body.Viewport.Height, "the message scrolls");
            Assert.False(body.AllowAutoHide);
            AssertOnScreen(app, "Update now");
            AssertOnScreen(app, "Later");

            dialog.ChooseCommand.Execute(dialog.Buttons[1]);
            Assert.True(pending.IsCompleted);
        }
    }

    [AvaloniaFact]
    public void ADialogWithDetailsOpensWideAndAShortQuestionStaysCompact()
    {
        using(var app = new AppHarness())
        {
            var withDetails = NewDialog("Sync into another solution?", "Sync it into Other.slnx instead?", 2);
            var pending = app.ViewModel.ShowDialogAsync(withDetails);
            app.Pump();
            Assert.True(DialogCard(app).Bounds.Width >= 720, "a details box gets the full width: " + DialogCard(app).Bounds.Width);
            withDetails.ChooseCommand.Execute(withDetails.Buttons[0]);
            Assert.True(pending.IsCompleted);

            var question = new DialogViewModel { Title = "Delete model", Message = "Delete model TestDb?" };
            question.Buttons.Add(new DialogButton { Text = "Cancel", Result = "cancel" });
            pending = app.ViewModel.ShowDialogAsync(question);
            app.Pump();
            var width = DialogCard(app).Bounds.Width;
            Assert.True(width >= 520 && width < 720, "a short question stays compact: " + width);
            question.ChooseCommand.Execute(question.Buttons[0]);
            Assert.True(pending.IsCompleted);
        }
    }

    [AvaloniaFact]
    public async Task TheWizardsAreWiderAndTheirStepScrolls()
    {
        using(var app = new AppHarness())
        {
            app.Window.Width = 1000;
            app.Window.Height = 640;
            await app.ViewModel.NewCommand.ExecuteAsync(null);
            app.Pump();
            Assert.IsType<AddModelWizardViewModel>(app.ViewModel.Overlay);

            var wizard = app.FindAll<Border>().First(b => b.Classes.Contains("wizard"));
            Assert.Equal(880, wizard.Bounds.Width);
            Assert.False(app.Find<ScrollViewer>("WizardBody").AllowAutoHide);
            AssertOnScreen(app, "Next");
            app.Screenshot("52-wizard-small-window");
        }
    }

    private static DialogViewModel NewDialog(string title, string message, int noteLines)
    {
        var notes = string.Join("\n", Enumerable.Range(1, noteLines).Select(i => "- Change " + i + ": a line of release notes that is long enough to wrap in a narrow dialog."));
        var dialog = new DialogViewModel
        {
            Title = title,
            Message = message,
            DetailsTitle = "What's new",
            Details = notes
        };
        dialog.Buttons.Add(new DialogButton { Text = "Later", Result = "later" });
        dialog.Buttons.Add(new DialogButton { Text = "Update now", Result = "update", IsPrimary = true });
        return dialog;
    }

    private static Border DialogCard(AppHarness app)
    {
        return app.Find<Border>("DialogCard");
    }

    private static void AssertOnScreen(AppHarness app, string buttonText)
    {
        var button = app.FindAll<Button>().First(b => b.IsEffectivelyVisible && string.Equals(b.Content as string, buttonText, System.StringComparison.Ordinal));
        var topLeft = button.TranslatePoint(new Point(0, 0), app.Window);
        Assert.True(topLeft.HasValue);
        Assert.True(topLeft.Value.Y >= 0 && topLeft.Value.Y + button.Bounds.Height <= app.Window.Bounds.Height, buttonText + " is inside the window: " + topLeft.Value.Y);
    }
}
