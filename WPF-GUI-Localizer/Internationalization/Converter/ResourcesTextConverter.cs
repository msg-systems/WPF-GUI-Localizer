using System;
using System.Globalization;
using System.Windows.Data;
using Internationalization.LiteralProvider.Abstract;
using Internationalization.LiteralProvider.Resource;

namespace Internationalization.Converter
{
    /// <summary>
    /// Converter between <see cref="string"/> (from AttachedProperty ResourceKey) and the corresponding Resources
    /// entry for the current culture (also of type <see cref="string"/>).
    /// Does not throw exceptions, because of how close to the GUI and end-user it operates.
    /// Only <see cref="IValueConverter.Convert(object, Type, object, CultureInfo)"/> is implemented.
    /// </summary>
    public class ResourcesTextConverter : IValueConverter
    {
        /// <summary>
        /// Converts the ResourceKey to the corresponding Resources entry.
        /// Uses <see cref="AbstractLiteralProvider.Instance"/> to accesss the Resources files.
        /// Only works if <see cref="AbstractLiteralProvider.Instance"/> is of type
        /// <see cref="ResourceLiteralProvider"/>.
        /// </summary>
        /// <param name="value">The ResourceKey that should be converted.</param>
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
        /// The corresponding entry or string.Empty, if no entry was found for <paramref name="value"/>.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string translation = null;

            if (value is string resource)
            {
                if (AbstractLiteralProvider.Instance is ResourceLiteralProvider resourceLiteralProvider)
                {
                    translation = resourceLiteralProvider.GetGuiTranslationOfCurrentCulture(resource);
                }
            }

            return translation ?? string.Empty;
        }

        /// <summary>
        /// Not implemented, but does not throw Exception.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //this function is not needed and therefore not implemented, but cannot throw NotImplementedException,
            //because this function is called automatically by .NET.
            return Binding.DoNothing;
        }
    }
}