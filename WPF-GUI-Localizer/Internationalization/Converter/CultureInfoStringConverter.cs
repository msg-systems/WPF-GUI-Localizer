using System;
using System.Globalization;
using System.Windows.Data;
using Internationalization.Utilities;

namespace Internationalization.Converter
{
    /// <summary>
    /// Converter for string &lt;-&gt; CultureInfo;
    /// main direction is string to CultureInfo
    /// </summary>
    public class CultureInfoStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CultureInfo language)
            { 
                return language.DisplayName + " (" + language.Name + ")";
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string langString)
            {
                return CultureInfoUtil.GetCultureInfo(langString, true);
            }

            return null;
        }
    }
}
