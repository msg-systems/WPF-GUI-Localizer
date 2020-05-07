using System;
using System.Globalization;
using System.Windows.Data;
using Internationalization.LiteralProvider.Abstract;
using Internationalization.LiteralProvider.Resource;

namespace Internationalization.Converter
{
    public class ResourcesTextConverter : IValueConverter
    {
        /// <summary>
        /// Converter between AttachedProperty ResourceKey and the corresponding Resources entry for the current culture;
        /// uses AbstractLiteralProvider.Instance to accesss the Resources files.
        /// Only works if AbstractLiteralProvider.Instance is of type ResourceLiteralProvider.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string resource)
            {
                if (AbstractLiteralProvider.Instance is ResourceLiteralProvider resourceLiteralProvider)
                {
                    var translation = resourceLiteralProvider.GetGuiTranslationOfCurrentCulture(resource);

                    if (!string.IsNullOrEmpty(translation))
                    {
                        return translation;
                    }
                }
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}