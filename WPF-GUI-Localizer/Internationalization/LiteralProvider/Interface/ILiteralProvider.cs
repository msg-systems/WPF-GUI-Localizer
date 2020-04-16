using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using Internationalization.Model;

namespace Internationalization.LiteralProvider.Interface
{
    public interface ILiteralProvider
    {
        /// <summary>
        /// The language originally used in the application, which is ment to be internatiolized
        /// </summary>
        CultureInfo InputLanguage { get; }

        /// <summary>
        /// Used if InputLanguage is not english, to have recommendations be in english regardless
        /// </summary>
        CultureInfo PreferedLanguage { get; }

        /// <summary>
        /// Saves the current Literals using its FileProvider
        /// </summary>
        void Save();

        void SetGuiTranslation(DependencyObject element, IEnumerable<TextLocalization> texts);

        /// <summary>
        /// This function returns an ObservableCollection object, as it is only used once by LocalizerEventHandler
        /// </summary>
        ObservableCollection<TextLocalization> GetGuiTranslation(DependencyObject element);

        string GetGuiTranslationOfCurrentCulture(DependencyObject element);
        IEnumerable<CultureInfo> GetKnownLanguages();
    }
}