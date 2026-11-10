#region Usings
using System.ComponentModel;
#endregion

namespace SF.Models;

/// <summary>
///  Implementation of <see cref="INotifyPropertyChanged" /> to simplify models.
/// </summary>
public abstract class ModelBase : INotifyPropertyChanged
{
    /// <summary>
    ///     Multicast event for property change notifications.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    ///     Notifies listeners that a property value has changed.
    /// </summary>
    /// <param name="propertyName">
    ///     Name of the property used to notify listeners. 
    /// </param>
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
