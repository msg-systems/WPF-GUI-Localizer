﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Media;
using Internationalization.AttachedProperties;
using Internationalization.Enum;
using Internationalization.FileProvider.Interface;
using Internationalization.LiteralProvider.Abstract;
using Internationalization.Model;
using Internationalization.Utilities;
using Microsoft.Extensions.Logging;

namespace Internationalization.LiteralProvider.File
{
    public class FileLiteralProvider : AbstractLiteralProvider
    {
        private static ILogger _logger;
        private ProviderStatus _status;

        protected override ProviderStatus Status
        {
            get
            {
                //FileProvider is expected to have been initialized before using FileLiteralProvider,
                //because information, about what languages are supported, comes from file.
                if (_status == ProviderStatus.Initialized)
                {
                    return FileProviderInstance.Status;
                }

                //correct status when needed.
                if (_status == ProviderStatus.CancellationInProgress &&
                    FileProviderInstance.Status == ProviderStatus.CancellationComplete)
                {
                    _status = ProviderStatus.CancellationComplete;
                }

                return _status;
            }
        }

        private FileLiteralProvider(IFileProvider fileProvider, CultureInfo inputLanguage, CultureInfo preferredLanguage)
        {
            FileProviderInstance = fileProvider;
            _status = ProviderStatus.Initialized;
            InputLanguage = inputLanguage;
            PreferredLanguage = preferredLanguage;
        }

        /// <summary>
        /// Initializes the singleton instance of AbstractLiteralProvider.
        /// Call this method before accessing the property Instance.
        /// </summary>
        /// <param name="fileProvider">Has to be initialized before acessing Instance.</param>
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
        /// <param name="fileProvider">Has to be initialized before acessing Instance.</param>
        /// <param name="inputLanguage">
        /// The language originally used in the application, which is ment to be internationalized.
        /// </param>
        /// <param name="preferredLanguage">
        /// Used for example if InputLanguage is not english, to have recommendations be in english regardless.
        /// </param>
        public static void Initialize(IFileProvider fileProvider, CultureInfo inputLanguage,
            CultureInfo preferredLanguage)
        {
            _logger = GlobalSettings.LibraryLoggerFactory.CreateLogger<FileLiteralProvider>();

            Instance = new FileLiteralProvider(fileProvider, inputLanguage, preferredLanguage);
        }

        public override void Save()
        {
            FileProviderInstance.SaveDictionary();
        }

        public override ObservableCollection<TextLocalization> GetGuiTranslation(DependencyObject element)
        {
            GetControlProperties(element, out var controlId, out var currentText, out var controlType,
                out var parentDialogName);
            if (string.IsNullOrWhiteSpace(parentDialogName) || string.IsNullOrWhiteSpace(controlId) ||
                string.IsNullOrWhiteSpace(controlType))
            {
                return null;
            }

            var dictOfDicts = GetDictionaryFromFileProvider();

            ICollection<TextLocalization> localizations = new Collection<TextLocalization>();

            foreach (var language in dictOfDicts.Keys)
            {
                localizations.Add(GetLiteral(language, parentDialogName, controlType,
                    controlId, true));
            }

            //if entry is new, use text from XAML.
            if (localizations.All(localization => string.IsNullOrWhiteSpace(localization.Text)))
            {
                localizations.First(localization => Equals(localization.Language, InputLanguage)).Text = currentText;
            }

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
                            InputLanguage, dictOfDicts);
                }

                observableLocalizations.Add(localization);
            }

            return observableLocalizations;
        }

        public override string GetGuiTranslationOfCurrentCulture(DependencyObject element)
        {
            GetControlProperties(element, out var controlId, out _, out var controlType,
                out var parentDialogName);
            if (string.IsNullOrWhiteSpace(parentDialogName) || string.IsNullOrWhiteSpace(controlId) ||
                string.IsNullOrWhiteSpace(controlType))
            {
                return null;
            }

            return GetLiteral(Thread.CurrentThread.CurrentUICulture, parentDialogName, controlType,
                controlId, false).Text;
        }

        public override IEnumerable<CultureInfo> GetKnownLanguages()
        {
            IList<CultureInfo> langList = GetDictionaryFromFileProvider().Keys.ToList();
            langList.Remove(CultureInfo.InvariantCulture);
            return langList;
        }

        public override void SetGuiTranslation(DependencyObject element, IEnumerable<TextLocalization> texts)
        {
            GetControlProperties(element, out var controlId, out _, out var controlType,
                out var parentDialogName);
            if (string.IsNullOrWhiteSpace(parentDialogName) || string.IsNullOrWhiteSpace(controlId) ||
                string.IsNullOrWhiteSpace(controlType) || texts == null)
            {
                _logger.Log(LogLevel.Debug,
                    "Failed to override translation for dialog '{0}', type '{1}' and name '{2}'.",
                    parentDialogName, controlType, controlId);
                return;
            }

            SetLiteral(parentDialogName, controlType, controlId, texts.ToList());
        }

        protected override void CancelInitialization()
        {
            _status = ProviderStatus.CancellationInProgress;
            FileProviderInstance.CancelInitialization();
        }

        private void SetLiteral(string dialogName, string type, string elementName, IList<TextLocalization> texts)
        {
            foreach (var textLocalization in texts)
            {
                if (textLocalization?.Text != null)
                {
                    textLocalization.Text = EscapedStringConverter.ToEscapedString(textLocalization.Text);
                }
            }

            FileProviderInstance.Update(CreateGuiDictionaryKey(dialogName, type, elementName), texts);
        }

        private TextLocalization GetLiteral(CultureInfo language, string dialogName, string type, string elementName,
            bool exactLanguage)
        {
            var key = CreateGuiDictionaryKey(dialogName, type, elementName);

            var dictOfDicts = GetDictionaryFromFileProvider();

            string result = CultureInfoUtil.GetLanguageDictValueOrDefault(dictOfDicts, language, key,
                InputLanguage, exactLanguage);
            
            if (result == null && !exactLanguage)
            {
                _logger.Log(LogLevel.Debug,
                    "Found no translation for dialog '{0}', type '{1}', name '{2}' and language '{3}'.",
                    dialogName, type, elementName, language);
            }

            if(result != null)
            {
                result = EscapedStringConverter.ToNormalString(result);
            }

            return new TextLocalization {Language = language, Text = result};
        }

        private static string CreateGuiDictionaryKey(string dialogName, string type, string elementName = "")
        {
            var seperator = Properties.Settings.Default.Seperator_for_partial_Literalkeys;
            return dialogName + seperator + type + seperator + elementName;
        }

        private Dictionary<CultureInfo, Dictionary<string, string>> GetDictionaryFromFileProvider()
        {
            var dict = FileProviderInstance.GetDictionary();

            if (dict == null || dict.Count == 0)
            {
                dict = new Dictionary<CultureInfo, Dictionary<string, string>>()
                {
                    { Thread.CurrentThread.CurrentUICulture, new Dictionary<string, string>() }
                };
            }

            if (!dict.ContainsKey(InputLanguage))
            {
                dict.Add(InputLanguage, new Dictionary<string, string>());
            }

            return dict;
        }

        private static void GetControlProperties(DependencyObject element, out string controlId, out string currentText,
            out string controlType, out string parentDialogName)
        {
            parentDialogName = null;

            //determine Name, Type and current Text.
            switch (element)
            {
                case RibbonTab ribbonTab:
                {
                    controlId = ribbonTab.Name;
                    currentText = ribbonTab.Header as string;
                    controlType = typeof(RibbonTab).Name;
                    break;
                }
                case RibbonGroup ribbonGroup:
                {
                    controlId = ribbonGroup.Name;
                    currentText = ribbonGroup.Header as string;
                    controlType = typeof(RibbonGroup).Name;
                    break;
                }
                case TabItem tabItem:
                {
                    controlId = tabItem.Name;
                    currentText = tabItem.Header as string;
                    controlType = typeof(TabItem).Name;

                    break;
                }
                case RibbonButton ribbonButton:
                {
                    controlId = ribbonButton.Name;
                    currentText = ribbonButton.Content as string;
                    controlType = typeof(RibbonButton).Name;
                    break;
                }
                case Label label:
                {
                    controlId = label.Name;
                    currentText = label.Content as string;
                    controlType = typeof(Label).Name;
                    break;
                }
                case Button button:
                {
                    controlId = button.Name;
                    currentText = button.Content as string;
                    controlType = typeof(Button).Name;
                    break;
                }
                case RadioButton radioButton:
                {
                    controlId = radioButton.Name;
                    currentText = radioButton.Content as string;
                    controlType = typeof(RadioButton).Name;
                    break;
                }
                case CheckBox checkBox:
                {
                    controlId = checkBox.Name;
                    currentText = checkBox.Content as string;
                    controlType = typeof(CheckBox).Name;
                    break;
                }
                case TextBlock textBox:
                {
                    controlId = textBox.Name;
                    currentText = textBox.Text;
                    controlType = typeof(TextBlock).Name;
                    break;
                }
                case DataGridColumnHeader columnHeader:
                {
                    controlId = DataGridProperties.GetName(columnHeader.Column);
                    currentText = columnHeader.Content as string;
                    controlType = typeof(DataGridColumn).Name;
                    break;
                }
                case DataGridColumn column:
                {
                    controlId = DataGridProperties.GetName(column);
                    currentText = column.Header as string;
                    controlType = typeof(DataGridColumn).Name;
                    try
                    {
                        //column itself is not in the VisualTree, since it isn't a Visual.
                        parentDialogName = GetParentDialogName(LogicalTreeUtils.GetDataGridParent(column));
                    }
                    catch
                    {
                        _logger.Log(LogLevel.Debug, "Unable to find parent of DataGridColumn.");
                    }

                    break;
                }
                default:
                {
                    controlId = null;
                    currentText = null;
                    controlType = null;
                    _logger.Log(LogLevel.Debug, 
                        $"GetControlProperties was called for non translatable type ({element.GetType()})");

                    return;
                }
            }

            //determine Name of View or Window, if element isn't DataGridColumn.
            if (parentDialogName == null)
            {
                parentDialogName = GetParentDialogName(element);
            }

            //to avoid misalignment while using ExcelFileProvider.
            controlId = controlId?.Replace(Properties.Settings.Default.Seperator_for_partial_Literalkeys.ToString(),
                "");
            controlType = controlType?.Replace(Properties.Settings.Default.Seperator_for_partial_Literalkeys.ToString(),
                "");
            parentDialogName =
                parentDialogName?.Replace(Properties.Settings.Default.Seperator_for_partial_Literalkeys.ToString(), "");
        }

        private static string GetParentDialogName(object sender)
        {
            string parentDialogName = null;
            var currentObject = sender as DependencyObject;

            //move up the VisualTree, until UserControl or Window is found.
            //search limited by numbers of iterations to stop infinite loops.
            for (var i = 0; currentObject != null && parentDialogName == null; i++)
            {
                currentObject = VisualTreeHelper.GetParent(currentObject);

                switch (currentObject)
                {
                    case UserControl userControl:
                        parentDialogName = userControl.Name;
                        //by setting parentDialogName, the search is aborted.

                        break;
                    case Window window:
                        parentDialogName = window.Name;
                        //by setting parentDialogName, the search is aborted.

                        break;
                    default:
                        //continue searching.
                        break;
                }
            }

            return parentDialogName;
        }
    }
}