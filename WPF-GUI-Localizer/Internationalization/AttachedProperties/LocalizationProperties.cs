using System.Windows;
using Internationalization.Localizer.LocalizerEventHandler;

namespace Internationalization.AttachedProperties
{
    /// <summary>
    /// Used to attach/detach the Localizer to View or Window.
    /// </summary>
    public class LocalizationProperties : DependencyObject
    {
        public static readonly DependencyProperty IsActiveProperty = DependencyProperty.RegisterAttached(
            "IsActive", typeof(string), typeof(LocalizationProperties),
            new PropertyMetadata(default(string), IsActiveChangedCallback));

        public static string GetIsActive(DependencyObject d)
        {
            return (string) d.GetValue(IsActiveProperty);
        }

        public static void SetIsActive(DependencyObject d, string value)
        {
            d.SetValue(IsActiveProperty, value);
        }

        /// <summary>
        /// Load the Localizer, when "isActive" Property is set in a View / Window.
        /// </summary>
        /// <param name="d">The element (typically: View or Window), which the Localizer is attached to.</param>
        /// <param name="e">Event Parameter / Info (used for access to new value).</param>
        private static void IsActiveChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var isActive = "True".Equals(e.NewValue.ToString());

            var parent = (FrameworkElement) d;


            if (isActive)
            {
                parent.Loaded += LocalizerEventHandler.ElementInitialized;
                LocalizerEventHandler.AttachLocalizationHelper(parent);
            }
            else
            {
                LocalizerEventHandler.DetachLocalizationHelper(parent);
            }
        }
    }
}