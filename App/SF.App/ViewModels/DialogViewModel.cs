#region Usings
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Threading.Tasks;
#endregion

namespace SF.App.ViewModels;

/// <summary>
/// A message or question shown over the window. The task completes with the chosen button's result.
/// </summary>
internal sealed partial class DialogViewModel : ViewModelBase
{
    private readonly TaskCompletionSource<string> _result = new TaskCompletionSource<string>();

    public string Title { get; set; }
    public string Message { get; set; }
    public bool IsError { get; set; }

    /// <summary>
    /// Shows a warning sign next to the title (questions about destructive actions).
    /// </summary>
    public bool IsWarning { get; set; }

    /// <summary>
    /// An error shows a red sign.
    /// </summary>
    public bool HasErrorSign
    {
        get
        {
            return IsError;
        }
    }

    /// <summary>
    /// A warning (a question about a destructive action) shows an amber sign; an error that is also a warning shows the red one.
    /// </summary>
    public bool HasWarningSign
    {
        get
        {
            return IsWarning && !IsError;
        }
    }

    public List<DialogButton> Buttons { get; set; } = new List<DialogButton>();

    public Task<string> Result
    {
        get
        {
            return _result.Task;
        }
    }

    [RelayCommand]
    private void Choose(DialogButton button)
    {
        _result.TrySetResult(button?.Result);
    }

    public void Cancel()
    {
        _result.TrySetResult(null);
    }
}

/// <summary>
/// One button of a dialog.
/// </summary>
internal sealed class DialogButton
{
    public string Text { get; set; }
    public string Result { get; set; }
    public bool IsPrimary { get; set; }

    /// <summary>
    /// A destructive action: shown in red.
    /// </summary>
    public bool IsDanger { get; set; }
}

/// <summary>
/// The results dialogs return.
/// </summary>
internal static class DialogResults
{
    public const string Ok = "ok";
    public const string Yes = "yes";
    public const string No = "no";
    public const string Save = "save";
    public const string Discard = "discard";
    public const string Cancel = "cancel";
}
