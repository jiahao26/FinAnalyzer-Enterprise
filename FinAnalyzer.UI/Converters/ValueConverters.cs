using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FinAnalyzer.UI.Converters;

/// <summary>
/// Converts a boolean value to Visibility.
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            bool invert = parameter?.ToString() == "Invert";
            return (boolValue != invert) ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a non-null/empty string to Visibility.
/// Default: Shows when string HAS value, hides when empty.
/// With Invert parameter: Shows when string is empty.
/// </summary>
public sealed class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool hasValue = !string.IsNullOrEmpty(value as string);
        bool invert = parameter?.ToString() == "Invert";
        if (invert)
            return hasValue ? Visibility.Collapsed : Visibility.Visible;
        return hasValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a collection to Visibility based on whether it has items.
/// </summary>
public sealed class CollectionToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is System.Collections.IEnumerable enumerable)
        {
            return enumerable.Cast<object>().Any() ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a percentage value to width.
/// </summary>
public sealed class PercentageToWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double percentage)
        {
            double maxWidth = 200;
            if (parameter is double max)
                maxWidth = max;
            return percentage / 100.0 * maxWidth;
        }
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts nav index to boolean for RadioButton IsChecked binding.
/// </summary>
public sealed class NavIndexConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int index && parameter is string paramStr && int.TryParse(paramStr, out int targetIndex))
        {
            return index == targetIndex;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts DocumentStatus.Processing to Visibility.
/// </summary>
public sealed class ProcessingStatusToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Models.DocumentStatus status)
        {
            return status == Models.DocumentStatus.Processing ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
