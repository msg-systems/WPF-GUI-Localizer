using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
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
        private static ILogger _logger;

        private readonly Dictionary<CultureInfo, Dictionary<string, string>> _dictOfDicts =
            new Dictionary<CultureInfo, Dictionary<string, string>>();

        private bool _dictOfDictsIncludesChanges;
        private ProviderStatus _status;

        /// <summary>
        ///     Default Constructor needed for mocking.
        /// </summary>
        protected ResourceLiteralProvider()
        {
            _status = ProviderStatus.Empty;
        }

        /// <inheritdoc cref="ReadDicts" />
        private ResourceLiteralProvider(IFileProvider fileProvider, CultureInfo inputLanguage,
            CultureInfo preferredLanguage)
        {
            FileProviderInstance = fileProvider;
            InputLanguage = inputLanguage;
            PreferredLanguage = preferredLanguage;

            ReadDicts();
        }

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

        /// <summary>
        ///     Initializes the singleton instance of AbstractLiteralProvider.
        ///     Call this method before accessing the property Instance.
        /// </summary>
        /// <param name="fileProvider">Does not have to be initialized before acessing Instance.</param>
        /// <param name="inputLanguage">
        ///     The language originally used in the application, which is ment to be internationalized.
        /// </param>
        /// <inheritdoc cref="ReadDicts" />
        public static void Initialize(IFileProvider fileProvider, CultureInfo inputLanguage)
        {
            Initialize(fileProvider, inputLanguage, new CultureInfo("en"));
        }

        /// <summary>
        ///     Initializes the singleton instance of AbstractLiteralProvider.
        ///     Call this method before accessing the property Instance.
        /// </summary>
        /// <param name="fileProvider">Does not have to be initialized before acessing Instance.</param>
        /// <param name="inputLanguage">
        ///     The language originally used in the application, which is ment to be internationalized.
        /// </param>
        /// <param name="preferredLanguage">
        ///     Used for example if InputLanguage is not english, to have recommendations be in english regardless.
        /// </param>
        /// <inheritdoc cref="ReadDicts" />
        public static void Initialize(IFileProvider fileProvider, CultureInfo inputLanguage,
            CultureInfo preferredLanguage)
        {
            _logger = GlobalSettings.LibraryLoggerFactory.CreateLogger<ResourceLiteralProvider>();

            Instance = new ResourceLiteralProvider(fileProvider, inputLanguage, preferredLanguage);
        }

        public override ObservableCollection<TextLocalization> GetGuiTranslation(DependencyObject element)
        {
            //collect translation individually.
            ICollection<TextLocalization> localizations = new Collection<TextLocalization>();
            foreach (var lang in GetKnownLanguages())
            {
                var translation = GetTranslation(GetKeyFromUnkownElementType(element), lang, true);
                localizations.Add(new TextLocalization {Language = lang, Text = translation});
            }

            //fill translations without Text.
            GetTranslationDummyText(localizations, InputLanguage, PreferredLanguage);

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
                GetTranslation(GetKeyFromUnkownElementType(element), Thread.CurrentThread.CurrentUICulture, false);

            return translation;
        }

        /// <summary>
        ///     Needed for ResourcesTextConverter and only supported by ResourceLiteralProvider.
        ///     The ResourcesTextConverter can only access the resourceKey string, not the element
        ///     itself. It can therefore not use the GetGuiTranslationOfCurrentCulture(DependencyObject)
        ///     method provided by all ILiteralProviders.
        ///     Virtual Method to enable mocking.
        /// </summary>
        public virtual string GetGuiTranslationOfCurrentCulture(string resourceKey)
        {
            var translation = GetTranslation(resourceKey, Thread.CurrentThread.CurrentUICulture, false);

            return translation;
        }

        public override IEnumerable<CultureInfo> GetKnownLanguages()
        {
            IList<CultureInfo> langList = _dictOfDicts.Keys.ToList();
            langList.Remove(CultureInfo.InvariantCulture);
            return langList;
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

        protected override void CancelInitialization()
        {
            _status = ProviderStatus.CancellationInProgress;
            FileProviderInstance.CancelInitialization();
        }

        /// <exception cref="ResourcesNotFoundException">
        ///     Thrown, if both <see cref="GlobalSettings.ResourcesAssembly" /> is not set and the entry assembly
        ///     cannot be accesed.
        /// </exception>
        private void ReadDicts()
        {
            var rm = ResourcesManagerProvider.GetResourcesManager();

            Dictionary<string, string> invariantFallback = null;

            //collect all Resource entries.
            var langs = CultureInfo.GetCultures(CultureTypes.AllCultures);
            foreach (var lang in langs)
            {
                try
                {
                    //tryParents is false and will be handled in CultureInfoUtils insted, to avoid registering
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
                    //all non-existent languages will be ignored.
                }
            }

            TryAddChangesIntoDictOfDicts();

            if (_dictOfDicts.ContainsKey(InputLanguage))
            {
                _dictOfDicts.Add(CultureInfo.InvariantCulture, invariantFallback);
            }
            //if Inputlanguage is not present, use invariant as replacement instead, because
            //InputLanguage is expected to always exist.
            else
            {
                ExceptionLoggingUtils.ThrowIf<InputLanguageNotFoundException>(invariantFallback == null,
                    _logger, $"The given input language ({InputLanguage.EnglishName}) was not found in the " +
                             "Resources files.");

                _dictOfDicts.Add(InputLanguage, invariantFallback);
            }

            _status = ProviderStatus.Initialized;
        }

        private void TryAddChangesIntoDictOfDicts()
        {
            if (!_dictOfDictsIncludesChanges && FileProviderInstance.Status == ProviderStatus.Initialized ||
                FileProviderInstance.Status == ProviderStatus.Empty)
            {
                //call should be save, since State is Empty or Initialized.
                var changes = GetDictionaryFromFileProvider();

                //correct dict.
                foreach (var langDict in changes)
                {
                    //correct on language level.
                    if (!_dictOfDicts.ContainsKey(langDict.Key))
                    {
                        _dictOfDicts.Add(langDict.Key, new Dictionary<string, string>());
                    }

                    foreach (var changedLocalization in langDict.Value)
                    {
                        //correct on localization level.
                        if (!langDict.Value.ContainsKey(changedLocalization.Key))
                        {
                            _dictOfDicts[langDict.Key].Add(changedLocalization.Key, changedLocalization.Value);
                        }
                        else
                        {
                            _dictOfDicts[langDict.Key][changedLocalization.Key] = changedLocalization.Value;
                        }
                    }
                }

                _dictOfDictsIncludesChanges = true;
            }
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

        private string GetTranslation(string resourceKey, CultureInfo language, bool exactLanguage)
        {
            if (string.IsNullOrEmpty(resourceKey))
            {
                return null;
            }

            //check for changes everytime (changes-dict can update late, incase of ExcelFileProvider).
            TryAddChangesIntoDictOfDicts();

            return CultureInfoUtil.GetLanguageDictValueOrDefault(_dictOfDicts, language, resourceKey,
                InputLanguage, exactLanguage);
        }

        private Dictionary<CultureInfo, Dictionary<string, string>> GetDictionaryFromFileProvider()
        {
            var dict = FileProviderInstance.GetDictionary();

            if (dict == null || dict.Count == 0)
            {
                dict = new Dictionary<CultureInfo, Dictionary<string, string>>
                {
                    {Thread.CurrentThread.CurrentUICulture, new Dictionary<string, string>()}
                };
            }

            if (!dict.ContainsKey(InputLanguage))
            {
                dict.Add(InputLanguage, new Dictionary<string, string>());
            }

            return dict;
        }
    }
}