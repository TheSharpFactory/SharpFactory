#region Usings
using CommunityToolkit.Mvvm.ComponentModel;
#endregion

namespace SF.App.ViewModels;

/// <summary>
/// Base of every view model: property-change notifications through CommunityToolkit.Mvvm.
/// </summary>
internal abstract class ViewModelBase : ObservableObject
{
}
