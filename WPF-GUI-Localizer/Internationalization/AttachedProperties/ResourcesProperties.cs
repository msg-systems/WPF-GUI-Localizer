using System.Windows;

namespace Internationalization.AttachedProperties
{
    /// <summary>
    /// Workaround to both use Resources files and get access to the key, which is inaccessable if static bindings are used
    /// </summary>
    public class ResourcesProperties : DependencyObject
    {
        public static string GetResourceKey(DependencyObject d)
        {
            try
            {
                return (string)d.GetValue(ResourceKeyProperty);
            }
            catch
            {
                return null;
            }
        }

        public static void SetResourceKey(DependencyObject d, string value)
        {
            d.SetValue(ResourceKeyProperty, value);
        }

        public static readonly DependencyProperty ResourceKeyProperty = DependencyProperty.RegisterAttached(
            "ResourceKey", typeof(string), typeof(ResourcesProperties),
            new PropertyMetadata(default(string)));
    }
}
