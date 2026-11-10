using System.Globalization;
using Avalonia.Data.Converters;

namespace XwaInstallerLibCore;

internal class CheckedVisibilityConverter : IMultiValueConverter
{
    public static readonly CheckedVisibilityConverter Default = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2)
        {
            return true;
        }

        if (values.Take(2).Any(t => t is Avalonia.Data.BindingNotification or null))
        {
            return true;
        }

        bool visible = values[0] is true;
        bool isChecked = values[1] is true;
        return visible && isChecked;
    }
}
