using System;
using System.Globalization;
using System.Windows.Data;
using Internationalization.Utilities;

namespace Internationalization.Converter
{
    /// <summary>
    /// Converter between string and CultureInfo.
    /// Does not throw exceptions, because of how close to the GUI and end-user it operates.
    /// </summary>
    public class CultureInfoStringConverter : IValueConverter
    {
        /// <summary>
        /// Converts CultureInfo to string. String has format "{full name in OS language} ({language code})".
        /// Will always return at least an empty string.
        /// </summary>
        /// <param name="value">The <see cref="CultureInfo"/> object that should be converted.</param>
        /// <param name="targetType">
        /// The expected <paramref name="targetType"/> is <see cref="string"/>, however receiving a different
        /// type does not influence what is returned.
        /// </param>
        /// <param name="parameter">This converter does not use parameters.</param>
        /// <param name="culture">
        /// This value is ignored, as the value used by .NET for this parameter, does not align with
        /// <see cref="Thread.CurrentThread.CurrentUICulture"/>.
        /// </param>
        /// <returns>
        /// The corresponding string or string.Empty, if <paramref name="value"/> is not of type
        /// <see cref="CultureInfo"/> or null.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CultureInfo language)
            {
                return $"{language.DisplayName} ({language.Name})";
            }

            return string.Empty;
        }

        /// <summary>
        /// Converts string to CultureInfo.
        /// The String is expected to contain the correct language code in brackets at the end.
        /// Will always return at least a default value (null).
        /// </summary>
        /// <param name="value">The <see cref="string"/> object that should be converted.</param>
        /// <param name="targetType">
        /// The expected <paramref name="targetType"/> is CultureInfo, however receiving a different
        /// type does not influence what is returned.
        /// </param>
        /// <param name="parameter">This converter does not use parameters.</param>
        /// <param name="culture">
        /// This value is ignored, as the value used by .NET for this parameter, does not align with
        /// <see cref="Thread.CurrentThread.CurrentUICulture"/>.
        /// </param>
        /// <returns>
        /// The corresponding <see cref="CultureInfo"/> object or null, if <paramref name="value"/> is not of type
        /// <see cref="string"/> or an invalid language code.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string langString)
            {
                return CultureInfoUtil.GetCultureInfoOrDefault(langString, true);
            }

            return null;
        }
    }
}