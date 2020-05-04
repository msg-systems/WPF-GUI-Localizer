using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using Internationalization.Enum;
using Internationalization.FileProvider.Interface;
using Internationalization.LiteralProvider.Interface;
using Internationalization.Model;
using Internationalization.Utilities;

namespace Internationalization.LiteralProvider.Abstract
{
    public abstract class AbstractLiteralProvider : ILiteralProvider
    {
        private static AbstractLiteralProvider _instance;
        protected abstract ProviderStatus Status { get; }


        /// <summary>
        /// The language originally used in the application, which is meant to be internationalized.
        /// </summary>
        public CultureInfo InputLanguage { get; protected set; }

        /// <summary>
        /// Used for example if InputLanguage is not english, to have recommendations be in english regardless.
        /// </summary>
        public CultureInfo PreferedLanguage { get; protected set; }

        /// <summary>
        /// Will return null, if singleton instance is null.
        /// Will manually continue pushing frames, if singleton instance is not null, but not initialized
        /// (endless loop possible).
        /// Will return the singleton instance, if fully initialized.
        /// </summary>
        public static ILiteralProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    return null;
                }

                //to avoid slowing down the UI.
                while (_instance.Status != ProviderStatus.Initialized)
                {
                    DoEventsDispatcher();
                }

                return _instance;
            }
            protected set => _instance = value as AbstractLiteralProvider;
        }

        /// <summary>
        /// The FileProvider used for saving literals, the exact usage varies depending ILiteralProvider implementation.
        /// </summary>
        protected IFileProvider FileProviderInstance { get; set; }

        /// <summary>
        /// Saves the current Literals using its FileProvider.
        /// </summary>
        public abstract void Save();

        /// <summary>
        /// Saves the current Literals using its FileProvider, if singleton instance is initialized
        /// and <paramref name="saveToFile"/> is true.
        /// Cancels initialization without saving otherwise.
        /// </summary>
        /// <param name="saveToFile">
        /// Determines if Literals get saved or not; Literals will not be saved, if instance is not
        /// initialized, independent of <paramref name="saveToFile"/> value.
        /// </param>
        public static void Exit(bool saveToFile)
        {
            if (_instance == null)
            {
                return;
            }

            switch (_instance.Status)
            {
                case ProviderStatus.InitializationInProgress:
                {
                    _instance.CancelInitialization();

                    while (_instance.Status == ProviderStatus.CancellationInProgress)
                    {
                        DoEventsDispatcher();
                    }

                    break;
                }
                case ProviderStatus.Initialized when saveToFile:
                    _instance.Save();
                    break;
                default:
                    //TODO revisit, after state was redone
                    break;
            }
        }

        public abstract void SetGuiTranslation(DependencyObject element, IEnumerable<TextLocalization> texts);

        /// <summary>
        /// This function returns an ObservableCollection object, as it is only used once by LocalizerEventHandler.
        /// </summary>
        public abstract ObservableCollection<TextLocalization> GetGuiTranslation(DependencyObject element);

        public abstract string GetGuiTranslationOfCurrentCulture(DependencyObject element);
        public abstract IEnumerable<CultureInfo> GetKnownLanguages();

        protected abstract void CancelInitialization();


        protected static void GetTranslationDummyText(ICollection<TextLocalization> localizedTexts,
            CultureInfo inputLanguage, CultureInfo preferedLanguage)
        {
            if (localizedTexts == null)
            {
                return;
            }

            foreach (var localization in localizedTexts)
            {
                if (localization != null && string.IsNullOrWhiteSpace(localization.Text))
                {
                    localization.Text = TextLocalizationsUtils.GetRecommendedText(localization.Language,
                        localizedTexts, true, inputLanguage, preferedLanguage);
                }
            }
        }

        private static void DoEventsDispatcher()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(
                    delegate(object f)
                    {
                        ((DispatcherFrame) f).Continue = false;
                        return null;
                    }), frame);
            Dispatcher.PushFrame(frame);
        }
    }
}