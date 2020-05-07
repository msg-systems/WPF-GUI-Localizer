using System.Windows;

namespace Internationalization.AttachedProperties
{
    /// <summary>
    /// This property enables DataGridColumns to be given a Name Property.
    /// </summary>
    public class DataGridProperties
    {
        public static string GetName(DependencyObject obj)
        {
            //only fails, if other property with same name is also attached.
            return (string) obj.GetValue(NameProperty);
        }

        public static void SetName(DependencyObject obj, string value)
        {
            obj.SetValue(NameProperty, value);
        }

        public static readonly DependencyProperty NameProperty =
            DependencyProperty.RegisterAttached("Name", typeof(string), typeof(DataGridProperties),
                new UIPropertyMetadata(""));
    }
}