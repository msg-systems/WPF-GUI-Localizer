using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows.Data;
using Internationalization.Utilities;

namespace Internationalization.Converter
{
    /// <summary>
    /// Converter between IEnumerable of strings and IEnumerable of CultureInfo.
    /// Does not throw exceptions, because of how close to the GUI and end-user it operates.
    /// </summary>
    public class CultureInfoCollectionStringConverter : IValueConverter
    {
        /// <summary>
        /// Converts IEnumerable&lt;CultureInfo&gt; to ICollection&lt;string&gt;.
        /// Will always return at least an empty list.
        /// </summary>
        /// <param name="value">The IEnumerable&lt;CultureInfo&gt; object that should be converted.</param>
        /// <param name="targetType">
        /// The expected <paramref name="targetType"/> is IEnumerable&lt;string&gt;, however receiving a different
        /// type does not influence what is returned.
        /// </param>
        /// <param name="parameter">This converter does not use parameters.</param>
        /// <param name="culture">
        /// This value is ignored, as the value used by .NET for this parameter, does not align with
        /// <see cref="Thread.CurrentThread.CurrentUICulture"/>.
        /// </param>
        /// <returns>
        /// The converted sequence. All invalid elements out of <paramref name="value"/> will be excluded.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ICollection<string> languageStrings = new List<string>();
            if (value is IEnumerable<CultureInfo> languages)
            {
                foreach (var language in languages)
                {
                    if (language == null)
                    {
                        continue;
                    }

                    languageStrings.Add(language.DisplayName + " (" + language.Name + ")");
                }
            }

            return languageStrings;
        }

        /// <summary>
        /// Converts IEnumerable&lt;string&gt; to ICollection&lt;CultureInfo&gt;.
        /// Will always return at least an empty list.
        /// </summary>
        /// <param name="value">The IEnumerable&lt;string&gt; object that should be converted.</param>
        /// <param name="targetType">
        /// The expected <paramref name="targetType"/> is IEnumerable&lt;CultureInfo&gt;, however receiving a different
        /// type does not influence what is returned.
        /// </param>
        /// <param name="parameter">This converter does not use parameters.</param>
        /// <param name="culture">
        /// This value is ignored, as the value used by .NET for this parameter, does not align with
        /// <see cref="Thread.CurrentThread.CurrentUICulture"/>.
        /// </param>
        /// <returns>
        /// The converted sequence. All invalid elements out of <paramref name="value"/> will be excluded.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ICollection<CultureInfo> languages = new List<CultureInfo>();
            if (value is IEnumerable<string> languageStrings)
            {
                foreach (var language in languageStrings)
                {
                    var convertedLanguage = CultureInfoUtil.GetCultureInfoOrDefault(language, true);
                    if (convertedLanguage != null)
                    {
                        languages.Add(convertedLanguage);
                    }
                }
            }

            return languages;
        }
    }
}