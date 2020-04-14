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
        /// Converter for AttachedProperty ResourceKey -&gt; Resources entry for current Culture;
        /// uses AbstractLiteralProvider.Instance for accesss to Resources files.
        /// only works if AbstractLiteralProvider.Instance is of type ResourceLiteralProvider.
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