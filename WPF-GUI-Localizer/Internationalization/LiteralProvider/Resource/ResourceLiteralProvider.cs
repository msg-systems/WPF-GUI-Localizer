﻿using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;
using Internationalization.AttachedProperties;
using Internationalization.Enum;
using Internationalization.Exception;
using Internationalization.FileProvider.Interface;
using Internationalization.LiteralProvider.Abstract;
using Internationalization.Model;
using Internationalization.Utilities;
using Microsoft.Extensions.Logging;

namespace Internationalization.LiteralProvider.Resource
{
    public class ResourceLiteralProvider : AbstractLiteralProvider
    {
        private readonly Dictionary<CultureInfo, Dictionary<string, string>> _dictOfDicts =
            new Dictionary<CultureInfo, Dictionary<string, string>>();

        private static ILogger _logger;
        private ProviderStatus _status;

        protected override ProviderStatus Status
        {
            //information about languages comes from Resource not the file,
            //therefore Status property of FileProviderInstance is ignored.
            get
            {
                //correct status when needed.
                if (_status == ProviderStatus.CancellationInProgress &&
                    FileProviderInstance.Status == ProviderStatus.CancellationComplete)
                {
                    _status = ProviderStatus.CancellationComplete;
                }

                return _status;
            }
        }

        private ResourceLiteralProvider(IFileProvider fileProvider, CultureInfo inputLanguage,
            CultureInfo preferedLanguage)
        {
            FileProviderInstance = fileProvider;
            InputLanguage = inputLanguage;
            PreferedLanguage = preferedLanguage;

            ReadDicts();
        }

        private void ReadDicts()
        {
            var rm = ResourcesUtils.GetResourcesManager();
            if (rm == null)
            {
                var nameOfAssembly = GlobalSettings.ResourcesAssembly == null
                    ? Assembly.GetEntryAssembly()?.FullName
                    : GlobalSettings.ResourcesAssembly.FullName;
                _logger.Log(LogLevel.Warning, $"Unable to read Resources files from assembly ({nameOfAssembly}).");
                return;
            }

            var invariantFallback = new Dictionary<string, string>();

            //collect all Resource entries.
            var langs = CultureInfo.GetCultures(CultureTypes.AllCultures);
            foreach (var lang in langs)
            {
                try
                {
                    //tryParents is false and will be handled in CultureInfoUtils insted to avoid registering
                    //same dict multiple times.
                    var resourceSet = rm.GetResourceSet(lang, true, false);
                    if (resourceSet == null) continue;

                    if (lang.Equals(CultureInfo.InvariantCulture))
                    {
                        invariantFallback = resourceSet.Cast<DictionaryEntry>().ToDictionary(
                            r => r.Key.ToString(), r => r.Value.ToString());
                    }
                    else
                    {
                        _dictOfDicts.Add(lang, resourceSet.Cast<DictionaryEntry>().ToDictionary(
                            r => r.Key.ToString(), r => r.Value.ToString()));
                    }
                }
                catch (CultureNotFoundException)
                {
                }
            }

            if (!_dictOfDicts.ContainsKey(InputLanguage))
            {
                _dictOfDicts.Add(InputLanguage, invariantFallback);
            }

            _status = ProviderStatus.Initialized;
        }

        /// <summary>
        /// Initializes the singleton instance of AbstractLiteralProvider.
        /// Call this method before accessing the property Instance.
        /// </summary>
        /// <param name="fileProvider">Does not have to be initialized before acessing Instance.</param>
        /// <param name="inputLanguage">
        /// The language originally used in the application, which is ment to be internationalized.
        /// </param>
        public static void Initialize(IFileProvider fileProvider, CultureInfo inputLanguage)
        {
            Initialize(fileProvider, inputLanguage, new CultureInfo("en"));
        }

        /// <summary>
        /// Initializes the singleton instance of AbstractLiteralProvider.
        /// Call this method before accessing the property Instance.
        /// </summary>
        /// <param name="fileProvider">Does not have to be initialized before acessing Instance.</param>
        /// <param name="inputLanguage">
        /// The language originally used in the application, which is ment to be internationalized.
        /// </param>
        /// <param name="preferedLanguage">
        /// Used for example if InputLanguage is not english, to have recommendations be in english regardless.
        /// </param>
        public static void Initialize(IFileProvider fileProvider, CultureInfo inputLanguage,
            CultureInfo preferedLanguage)
        {
            _logger = GlobalSettings.LibraryLoggerFactory.CreateLogger<ResourceLiteralProvider>();

            Instance = new ResourceLiteralProvider(fileProvider, inputLanguage, preferedLanguage);
        }

        public override ObservableCollection<TextLocalization> GetGuiTranslation(DependencyObject element)
        {
            //collect translation individually.
            ICollection<TextLocalization> localizations = new Collection<TextLocalization>();
            foreach (var lang in GetKnownLanguages())
            {
                var translation = GetTranslation(GetKeyFromUnkownElementType(element), lang);
                localizations.Add(new TextLocalization {Language = lang, Text = translation});
            }

            //fill translations without Text.
            GetTranslationDummyText(localizations, InputLanguage, PreferedLanguage);

            //fill known translations and convert to ObservableCollection.
            var sourceLocalization = localizations.FirstOrDefault(loc =>
                Equals(loc.Language, InputLanguage));
            var observableLocalizations =
                new ObservableCollection<TextLocalization>();
            foreach (var localization in localizations)
            {
                if (sourceLocalization != null)
                {
                    localization.KnownTranslations = TextLocalizationsUtils
                        .ExtractKnownTranslations(sourceLocalization.Text, localization.Language,
                            InputLanguage, _dictOfDicts);
                }

                observableLocalizations.Add(localization);
            }

            return observableLocalizations;
        }

        public override string GetGuiTranslationOfCurrentCulture(DependencyObject element)
        {
            var translation =
                GetTranslation(GetKeyFromUnkownElementType(element), Thread.CurrentThread.CurrentUICulture);

            return string.IsNullOrEmpty(translation) ? "<<empty>>" : translation;
        }

        /// <summary>
        /// Workaround for ResourcesTextConverter, only supported by ResourceLiteralProvider.
        /// The ResourcesTextConverter can only access the resourceKey string, not the element
        /// itself. It can therefore not use the GetGuiTranslationOfCurrentCulture(DependencyObject)
        /// method provided by all ILiteralProviders.
        /// </summary>
        public string GetGuiTranslationOfCurrentCulture(string resourceKey)
        {
            var translation = GetTranslation(resourceKey, Thread.CurrentThread.CurrentUICulture);

            return string.IsNullOrEmpty(translation) ? "<<empty>>" : translation;
        }

        protected override void CancelInitialization()
        {
            _status = ProviderStatus.CancellationInProgress;
            FileProviderInstance.CancelInitialization();
        }

        private static string GetKeyFromUnkownElementType(DependencyObject element)
        {
            //ResourceKeyProperty in only attached to DataGridColumn.
            if (element is DataGridColumnHeader asColumnHeader)
            {
                return ResourcesProperties.GetResourceKey(asColumnHeader.Column);
            }

            return ResourcesProperties.GetResourceKey(element);
        }

        private string GetTranslation(string resourceKey, CultureInfo language)
        {
            if (string.IsNullOrEmpty(resourceKey))
            {
                return null;
            }

            //check for changes everytime (changes-dict can change due to late loading).
            Dictionary<CultureInfo, Dictionary<string, string>> changes = null;
            try
            {
                changes = FileProviderInstance.GetDictionary();
            }
            catch (FileProviderNotInitializedException)
            {
                //logged in Debug, as this behaviour is intended, if the file does not exists initially.
                _logger.Log(LogLevel.Debug, "Unable to read changes from FileProvider.");
            }

            string translation = null;

            CultureInfoUtil.TryGetLanguageDict(changes, language)?.TryGetValue(resourceKey, out translation);

            if (translation != null) return translation;

            //if needed use translations from Resources.
            var langDict = CultureInfoUtil.TryGetLanguageDict(_dictOfDicts, language);
            langDict.TryGetValue(resourceKey, out translation);

            return translation ?? string.Empty;
        }

        public override IEnumerable<CultureInfo> GetKnownLanguages()
        {
            return _dictOfDicts.Keys.ToList();
        }

        public override void SetGuiTranslation(DependencyObject element, IEnumerable<TextLocalization> texts)
        {
            IList<TextLocalization> textsEnumerated = texts.ToList();

            var key = GetKeyFromUnkownElementType(element);

            foreach (var textLocalization in textsEnumerated)
            {
                _dictOfDicts.TryGetValue(textLocalization.Language, out var langDict);
                if (langDict == null)
                {
                    continue;
                }

                if (langDict.ContainsKey(key))
                {
                    langDict.Remove(key);
                }

                langDict.Add(key, textLocalization.Text);
            }

            FileProviderInstance.Update(key, textsEnumerated);
        }

        public override void Save()
        {
            FileProviderInstance.SaveDictionary();
        }
    }
}