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
/// The icon of the Sync summary bar: the Sync arrows for files to write (and while checking), a check when the solution
/// is up to date, a warning when Sync could not or would not run.
/// </summary>
internal sealed class SummaryIconConverter : IValueConverter
{
    public static SummaryIconConverter Instance { get; } = new SummaryIconConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var key = "IconSync";
        if(string.Equals(value as string, "uptodate", StringComparison.Ordinal) || string.Equals(value as string, "success", StringComparison.Ordinal))
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

/// <summary>
/// The icon of the line about the last Sync: a check when it worked, a warning when it stopped.
/// </summary>
internal sealed class LastSyncIconConverter : IValueConverter
{
    public static LastSyncIconConverter Instance { get; } = new LastSyncIconConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var key = value is bool succeeded && succeeded ? "IconCheck" : "IconWarning";
        return IconConverter.Instance.Convert(key, targetType, parameter, culture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Stretch when true, Top when false: a card that fills the page only when it has something to fill it with.
/// </summary>
internal sealed class StretchWhenConverter : IValueConverter
{
    public static StretchWhenConverter Instance { get; } = new StretchWhenConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool fills && fills ? Avalonia.Layout.VerticalAlignment.Stretch : Avalonia.Layout.VerticalAlignment.Top;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// A two-tab control's selected index as a bool: true for the second tab.
/// </summary>
internal sealed class TabIndexConverter : IValueConverter
{
    public static TabIndexConverter Instance { get; } = new TabIndexConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool second && second ? 1 : 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is int index && index == 1;
    }
}
