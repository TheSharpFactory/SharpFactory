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

    /// <summary>
    /// The log shows only the problems: the lines about skipped objects, and errors.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShownEntries))]
    private bool _showProblemsOnly;

    public ObservableCollection<LogEntryViewModel> Entries { get; } = new ObservableCollection<LogEntryViewModel>();

    /// <summary>
    /// The lines about skipped objects, and errors.
    /// </summary>
    public ObservableCollection<LogEntryViewModel> Problems { get; } = new ObservableCollection<LogEntryViewModel>();

    public ObservableCollection<LogEntryViewModel> ShownEntries
    {
        get
        {
            return ShowProblemsOnly ? Problems : Entries;
        }
    }

    public bool HasProblems
    {
        get
        {
            return Problems.Count > 0;
        }
    }

    /// <summary>
    /// "1 problem", "3 problems", for the toggle that shows only them.
    /// </summary>
    public string ProblemCountText
    {
        get
        {
            return Problems.Count == 1 ? "1 problem" : $"{Problems.Count} problems";
        }
    }

    /// <summary>
    /// The whole log as text, one "12:04:31  message" line per entry, for the Copy button.
    /// </summary>
    public string CopyText
    {
        get
        {
            var text = new StringBuilder();
            foreach(var entry in Entries)
                text.Append(entry.Time).Append("  ").Append(entry.IsError ? "ERROR " : entry.IsWarning ? "WARNING " : "").AppendLine(entry.Text);

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

    /// <summary>
    /// Goes up with every Clear: a line sent by an earlier run, still on its way to the UI thread, is not added.
    /// </summary>
    public int Run { get; private set; }

    public void Clear()
    {
        Run++;
        Entries.Clear();
        Problems.Clear();
        ShowProblemsOnly = false;
        OnPropertyChanged(nameof(LineCountText));
        OnPropertyChanged(nameof(HasProblems));
        OnPropertyChanged(nameof(ProblemCountText));
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

    /// <summary>
    /// The engine's lines about what it left out: "Skipped ...", "Not supported ...", "Not a Sharp Factory file ...".
    /// </summary>
    private static bool IsWarningText(string message)
    {
        if(string.IsNullOrEmpty(message))
            return false;

        return message.StartsWith("Skipped", StringComparison.Ordinal) || message.StartsWith("Not supported", StringComparison.Ordinal)
            || message.StartsWith("Not a Sharp Factory file", StringComparison.Ordinal);
    }

    public void Add(string message, string userMessage)
    {
        var isError = userMessage != null;
        var entry = new LogEntryViewModel { Text = message, IsError = isError, IsWarning = !isError && IsWarningText(message), Time = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) };
        Entries.Add(entry);
        if(entry.IsError || entry.IsWarning)
        {
            Problems.Add(entry);
            OnPropertyChanged(nameof(HasProblems));
            OnPropertyChanged(nameof(ProblemCountText));
        }

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
    /// A line about something left out (a skipped object, a file Sync will not touch): shown in amber.
    /// </summary>
    public bool IsWarning { get; set; }

    /// <summary>
    /// When the line was written: "12:04:31".
    /// </summary>
    public string Time { get; set; }
}
