using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using Internationalization.FileProvider.Interface;
using Internationalization.Model;
using Internationalization.Utilities;

namespace Internationalization.LiteralProvider.Abstract
{
    public abstract class AbstractLiteralProvider
    {

        private static AbstractLiteralProvider _instance;
        protected abstract ProviderStatus Status { get; }

        /// <summary>
        /// The language originally used in the application, which is ment to be internatiolized
        /// </summary>
        public CultureInfo InputLanguage { get; protected set; }
        /// <summary>
        /// Used if InputLanguage is not english, to have recommendations be in english regardless
        /// </summary>
        public CultureInfo PreferedLanguage { get; protected set; }

        /// <summary>
        /// Will return null, if singleton instance is null;
        /// Will manually continue pushing frames, if singleton instance is not null, but not initialized
        /// (endless loop possible)
        /// Will return the singleton instance, if fully initialized
        /// </summary>
        public static AbstractLiteralProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    return null;
                }

                //to avoid slowing down the UI
                while (_instance.Status != ProviderStatus.Initialized)
                {
                    DoEventsDispatcher();
                }

                return _instance;
            }
            protected set => _instance = value;
        }

        /// <summary>
        /// The FileProvider used for saving literals, the exact usage varies with LiteralProvider Implementation
        /// </summary>
        protected IFileProvider FileProviderInstance { get; set; }

        /// <summary>
        /// Saves the current Literals using its FileProvider
        /// </summary>
        public abstract void Save();

        /// <summary>
        /// Saves the current Literals using its FileProvider, if singleton instance is initialized
        /// and <see cref="saveToFile"/> is true.
        /// Cancels initialization without saving if not initialized.
        /// </summary>
        /// <param name="saveToFile">
        /// Determines if Literals get saved or not; Literals will not be saved, if instance is not
        /// initialized, independent of <see cref="saveToFile"/> value.
        /// </param>
        public static void Exit(bool saveToFile)
        {
            if (_instance == null) { return; }

            if (_instance.Status == ProviderStatus.InitializationInProgress)
            {
                _instance.CancelInitialization();

                while (_instance.Status == ProviderStatus.CancellationInProgress)
                {
                    DoEventsDispatcher();
                }
            }
            else if (_instance.Status == ProviderStatus.Initialized && saveToFile)
            {
                _instance.Save();
            }
        }

        public abstract void SetGuiTranslation(DependencyObject element, IEnumerable<TextLocalization> texts);
        /// <summary>
        /// This function returns an ObservableCollection object, as it is only used once by LocalizationUtils
        /// </summary>
        public abstract ObservableCollection<TextLocalization> GetGuiTranslation(DependencyObject element);
        public abstract string GetGuiTranslationOfCurrentCulture(DependencyObject element);
        public abstract IEnumerable<CultureInfo> GetKnownLanguages();

        protected abstract void CancelInitialization();



        protected static void GetTranslationDummyText(ICollection<TextLocalization> localizedTexts, CultureInfo inputLanguage, CultureInfo preferedLanguage)
        {
            if (localizedTexts == null)
            {
                return;
            }

            foreach (TextLocalization localization in localizedTexts)
            {
                if (localization != null && string.IsNullOrWhiteSpace(localization.Text))
                {
                    localization.Text = TextLocalizationsUtils.GetRecommendedText(localization, localizedTexts,
                        true, inputLanguage, preferedLanguage);
                }
            }
        }

        private static void DoEventsDispatcher()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(
                    delegate (object f)
                    {
                        ((DispatcherFrame)f).Continue = false;
                        return null;
                    }), frame);
            Dispatcher.PushFrame(frame);
        }
    }
}
