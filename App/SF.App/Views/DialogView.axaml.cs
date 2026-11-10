#region Usings
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
#endregion

namespace SF.App.Views;

internal sealed partial class DialogView : UserControl
{
    public DialogView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttached;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnAttached(object sender, VisualTreeAttachmentEventArgs e)
    {
        Dispatcher.UIThread.Post(FocusDefaultButton, DispatcherPriority.Loaded);
    }

    private void OnDataContextChanged(object sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(FocusDefaultButton, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// The primary button takes the keyboard (Enter chooses it); otherwise the first button.
    /// </summary>
    private void FocusDefaultButton()
    {
        var first = (Button)null;
        foreach(var visual in this.GetVisualDescendants())
        {
            if(visual is not Button button)
                continue;

            if(button.Classes.Contains("accent"))
            {
                button.Focus();
                return;
            }

            first ??= button;
        }

        first?.Focus();
    }
}
