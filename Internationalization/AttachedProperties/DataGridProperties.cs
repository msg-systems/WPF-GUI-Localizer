using System.Windows;

namespace Internationalization.AttachedProperties
{
    /// <summary>
    /// This is a workaround to give DataGridColumns a Name Property
    /// </summary>
    public class DataGridProperties
    {
        public static string GetName(DependencyObject obj)
        {
            try
            {
                return (string)obj.GetValue(NameProperty);
            }
            catch
            {
                return null;
            }
        }

        public static void SetName(DependencyObject obj, string value)
        {
            obj.SetValue(NameProperty, value);
        }

        public static readonly DependencyProperty NameProperty =
            DependencyProperty.RegisterAttached("Name", typeof(string), typeof(DataGridProperties), new UIPropertyMetadata(""));
    }
}
