#region Usings
using CommunityToolkit.Mvvm.ComponentModel;
#endregion

namespace SF.App.ViewModels;

/// <summary>
/// One page of the main window.
/// </summary>
internal abstract class PageViewModel : ViewModelBase
{
    protected PageViewModel(MainWindowViewModel shell)
    {
        Shell = shell;
    }

    public MainWindowViewModel Shell { get; }

    public abstract string Title { get; }

    /// <summary>
    /// Called when the page is shown, so it can refresh what depends on the open model.
    /// </summary>
    public virtual void OnShown()
    {
    }

    /// <summary>
    /// Called when a model file is opened, created or closed.
    /// </summary>
    public virtual void OnDocumentChanged()
    {
    }

    /// <summary>
    /// Called when another model of the same file is picked. By default the page refreshes as for a new document.
    /// </summary>
    public virtual void OnSelectedModelChanged()
    {
        OnDocumentChanged();
    }
}

/// <summary>
/// One entry of the navigation rail.
/// </summary>
internal sealed partial class NavItemViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isEnabled = true;

    /// <summary>
    /// A short count or mark next to the title (the files Sync would write); null for none.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasBadge))]
    private string _badge;

    [ObservableProperty]
    private string _badgeTip;

    public bool HasBadge
    {
        get
        {
            return !string.IsNullOrEmpty(Badge);
        }
    }

    /// <summary>
    /// Why the entry is off: its tooltip while there is no model file.
    /// </summary>
    public string Tip
    {
        get
        {
            return IsEnabled ? BadgeTip : "Open or create a model file first.";
        }
    }

    partial void OnIsEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(Tip));
    }

    partial void OnBadgeTipChanged(string value)
    {
        OnPropertyChanged(nameof(Tip));
    }

    public string Title { get; set; }

    /// <summary>
    /// The key of the icon geometry in the app resources.
    /// </summary>
    public string IconKey { get; set; }

    public PageViewModel Page { get; set; }

    /// <summary>
    /// True for pages that need an open model file.
    /// </summary>
    public bool RequiresDocument { get; set; }
}
