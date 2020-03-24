using System.Windows;
using Internationalization.Utilities;

namespace Internationalization.AttachedProperties
{
    /// <summary>
    /// used to attach the LocalizationUtils to View or Window (currently can't be turned off during runtime)
    /// </summary>
    public class LocalizationProperties : DependencyObject
    {
        public static readonly DependencyProperty IsActiveProperty = DependencyProperty.RegisterAttached(
            "IsActive", typeof(string), typeof(LocalizationProperties),
            new PropertyMetadata(default(string), IsActiveChangedCallback));

        public static string GetIsActive(DependencyObject d)
        {
            return (string)d.GetValue(IsActiveProperty);
        }

        public static void SetIsActive(DependencyObject d, string value)
        {
            d.SetValue(IsActiveProperty, value);
        }

        /// <summary>
        /// Load the LocalizationUtils, when "isActive" Property is set in a View / Window
        /// </summary>
        /// <param name="d">The element (typically: View or Window), which the LocalizationUtils is attached to</param>
        /// <param name="e">Event Parameter / Info (used for access to new value)</param>
        private static void IsActiveChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var isActive = "True".Equals(e.NewValue.ToString());

            var parent = (FrameworkElement)d;


            if (isActive)
            {
                parent.Loaded += LocalizationUtils.ElementInitialized;
                LocalizationUtils.AttachLocalizationHelper(parent);
            }
            else
            {
                LocalizationUtils.DettachLocalizationHelper(parent);
            }
        }
    }
}
