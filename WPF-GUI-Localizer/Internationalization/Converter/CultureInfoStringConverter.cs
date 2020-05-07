using System;
using System.Globalization;
using System.Windows.Data;
using Internationalization.Utilities;

namespace Internationalization.Converter
{
    /// <summary>
    /// Converter between string and CultureInfo.
    /// </summary>
    public class CultureInfoStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CultureInfo language)
            {
                return $"{language.DisplayName} ({language.Name})";
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string langString)
            {
                return CultureInfoUtil.GetCultureInfo(langString, true);
            }

            return Binding.DoNothing;
        }
    }
}