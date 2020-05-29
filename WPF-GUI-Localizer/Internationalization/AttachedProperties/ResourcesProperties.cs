using System.Windows;

namespace Internationalization.AttachedProperties
{
    /// <summary>
    ///     A property to declare Resources keys as readable Property from UI-Elements (used for the Resources translator to
    ///     read corresponding Resources at runtime).
    /// </summary>
    public class ResourcesProperties : DependencyObject
    {
        public static readonly DependencyProperty ResourceKeyProperty = DependencyProperty.RegisterAttached(
            "ResourceKey", typeof(string), typeof(ResourcesProperties),
            new PropertyMetadata(default(string)));

        public static string GetResourceKey(DependencyObject d)
        {
            //only fails, if other property with same name is also attached.
            return (string) d.GetValue(ResourceKeyProperty);
        }

        public static void SetResourceKey(DependencyObject d, string value)
        {
            d.SetValue(ResourceKeyProperty, value);
        }
    }
}