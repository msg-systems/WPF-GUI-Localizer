using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using Internationalization.Utilities;

namespace Internationalization.Converter
{
    /// <summary>
    /// Converter between ICollection of strings and ICollection of CultureInfo.
    /// </summary>
    public class CultureInfoCollectionStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ICollection<string> languageStrings = new List<string>();
            if (value is IEnumerable<CultureInfo> languages)
            {
                foreach (var language in languages)
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
                foreach (var language in languageStrings)
                {
                    if (language == null) continue;

                    languages.Add(CultureInfoUtil.GetCultureInfo(language, true));
                }
            }

            return languages;
        }
    }
}