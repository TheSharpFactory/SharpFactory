#region Usings
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
#endregion

namespace SF.App.ViewModels;

/// <summary>
/// The step log of a long operation (building a model, Sync).
/// </summary>
internal sealed partial class ProgressViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _errorText;

    [ObservableProperty]
    private string _currentStep;

    [ObservableProperty]
    private bool _isRunning;

    /// <summary>
    /// The one-line outcome shown when the work is over ("Model AppDb created: 15 tables, ...").
    /// </summary>
    [ObservableProperty]
    private string _resultText;

    [ObservableProperty]
    private bool _isSucceeded;

    public ObservableCollection<LogEntryViewModel> Entries { get; } = new ObservableCollection<LogEntryViewModel>();

    /// <summary>
    /// The whole log as text, one "12:04:31  message" line per entry, for the Copy button.
    /// </summary>
    public string CopyText
    {
        get
        {
            var text = new StringBuilder();
            foreach(var entry in Entries)
                text.Append(entry.Time).Append("  ").Append(entry.IsError ? "ERROR " : "").AppendLine(entry.Text);

            return text.ToString();
        }
    }

    /// <summary>
    /// "48 lines" for the bar above the log.
    /// </summary>
    public string LineCountText
    {
        get
        {
            return Entries.Count == 1 ? "1 line" : $"{Entries.Count} lines";
        }
    }

    public void Clear()
    {
        Entries.Clear();
        OnPropertyChanged(nameof(LineCountText));
        ErrorText = null;
        CurrentStep = null;
        ResultText = null;
        IsSucceeded = false;
    }

    public void Succeed(string resultText)
    {
        ResultText = resultText;
        IsSucceeded = true;
    }

    public void Add(string message, string userMessage)
    {
        var isError = userMessage != null;
        Entries.Add(new LogEntryViewModel { Text = message, IsError = isError, Time = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) });
        CurrentStep = message;
        OnPropertyChanged(nameof(LineCountText));

        if(isError)
            ErrorText = userMessage;
    }
}

/// <summary>
/// One line of a progress log.
/// </summary>
internal sealed class LogEntryViewModel
{
    public string Text { get; set; }
    public bool IsError { get; set; }

    /// <summary>
    /// When the line was written: "12:04:31".
    /// </summary>
    public string Time { get; set; }
}
