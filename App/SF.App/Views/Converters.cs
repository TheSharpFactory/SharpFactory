#region Usings
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;
#endregion

namespace SF.App.Views;

/// <summary>
/// Turns an icon key (IconHome, IconSync, ...) into the icon geometry from the app resources.
/// </summary>
internal sealed class IconConverter : IValueConverter
{
    public static IconConverter Instance { get; } = new IconConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if(value is string key && Application.Current != null && Application.Current.TryGetResource(key, null, out var resource) && resource is Geometry geometry)
            return geometry;

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// The icon of the Sync summary bar: an eye for a preview, a check for a Sync, a warning for a failure.
/// </summary>
internal sealed class SummaryIconConverter : IValueConverter
{
    public static SummaryIconConverter Instance { get; } = new SummaryIconConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var key = "IconEye";
        if(string.Equals(value as string, "success", StringComparison.Ordinal))
            key = "IconCheck";
        else if(string.Equals(value as string, "error", StringComparison.Ordinal))
            key = "IconWarning";

        return IconConverter.Instance.Convert(key, targetType, parameter, culture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Shows a text only when two strings are equal (diff line kinds, change kinds).
/// </summary>
internal sealed class EqualsConverter : IValueConverter
{
    public static EqualsConverter Instance { get; } = new EqualsConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return string.Equals(value as string, parameter as string, StringComparison.Ordinal);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
