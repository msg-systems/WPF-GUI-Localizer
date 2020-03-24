using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using Internationalization.Utilities;

namespace Internationalization.Converter
{
    /// <summary>
    /// Converter for ICollection&lt;string&gt; &lt;-&gt; ICollection&lt;CultureInfo&gt;;
    /// main direction is string to CultureInfo
    /// </summary>
    public class CultureInfoCollectionStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ICollection<string> languageStrings = new List<string>();
            if (value is IEnumerable<CultureInfo> languages)
            {
                foreach (CultureInfo language in languages)
                {
                    languageStrings.Add(language.DisplayName + " (" + language.Name + ")");
                }
            }

            return languageStrings;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ICollection<CultureInfo> languages = new List<CultureInfo>();
            if (value is IEnumerable<string> languageStrings)
            {
                foreach (string language in languageStrings)
                {
                    languages.Add(CultureInfoUtil.GetCultureInfo(language, true));
                }
            }

            return languages;
        }
    }
}
