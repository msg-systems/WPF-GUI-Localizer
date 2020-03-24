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
using Internationalization.FileProvider.Interface;
using Internationalization.LiteralProvider.Abstract;
using Internationalization.Model;
using Internationalization.Utilities;

namespace Internationalization.LiteralProvider.File
{
    public class FileLiteralProvider : AbstractLiteralProvider
    {

        private ProviderStatus _status;

        protected override ProviderStatus Status {
            get
            {
                // FileProvider is expected to have been initialized before using FileLiteralProvider,
                // because information, about what Languages are supported, comes from file
                if (_status == ProviderStatus.Initialized)
                {
                    return FileProviderInstance.Status;
                }
                //correct status when needed
                if (_status == ProviderStatus.CancellationInProgress && FileProviderInstance.Status == ProviderStatus.CancellationComplete)
                {
                    _status = ProviderStatus.CancellationComplete;
                }

                return _status;
            }
        }

        private FileLiteralProvider(IFileProvider fileProvider, CultureInfo inputLanguage, CultureInfo preferedLanguage)
        {
            FileProviderInstance = fileProvider;
            _status = ProviderStatus.Initialized;
            InputLanguage = inputLanguage;
            PreferedLanguage = preferedLanguage;
        }

        /// <summary>
        /// Initializes the singleton instance of AbstractLiteralProvider.
        /// Call this method before accessing the property <see cref="Instance"/>.
        /// </summary>
        /// <param name="fileProvider">has to be initialized before acessing <see cref="Instance"/></param>
        /// <param name="inputLanguage">The language originally used in the application, which is ment to be internationalized</param>
        public static void Initialize(IFileProvider fileProvider, CultureInfo inputLanguage)
        {
            Instance = new FileLiteralProvider(fileProvider, inputLanguage, new CultureInfo("en"));
        }

        /// <summary>
        /// Initializes the singleton instance of AbstractLiteralProvider.
        /// Call this method before accessing the property <see cref="Instance"/>.
        /// </summary>
        /// <param name="fileProvider">has to be initialized before acessing <see cref="Instance"/></param>
        /// <param name="inputLanguage">The language originally used in the application, which is ment to be internationalized</param>
        /// <param name="preferedLanguage">
        /// Used for example if InputLanguage is not english, to have recommendations be in english regardless.
        /// </param>
        public static void Initialize(IFileProvider fileProvider, CultureInfo inputLanguage, CultureInfo preferedLanguage)
        {
            Instance = new FileLiteralProvider(fileProvider, inputLanguage, preferedLanguage);
        }

        public override void Save()
        {
            FileProviderInstance.SaveDictionary();
        }

        public override ObservableCollection<TextLocalization> GetGuiTranslation(DependencyObject element)
        {
            GetControlProperties(element, out string controlId, out string currentText, out string controlType, out string parentDialogName);
            if (string.IsNullOrWhiteSpace(parentDialogName) || string.IsNullOrWhiteSpace(controlId) || string.IsNullOrWhiteSpace(controlType))
            {
                return null;
            }

            var dictOfDicts = FileProviderInstance.GetDictionary();
            ObservableCollection<TextLocalization> localizations = new ObservableCollection<TextLocalization>();

            foreach (CultureInfo language in dictOfDicts.Keys)
            {
                localizations.Add(GetLiteral(language, parentDialogName, controlType, controlId));
            }

            //if entry is new, use text from XAML
            if (localizations.All(localization => string.IsNullOrWhiteSpace(localization.Text)))
            {
                localizations.First(localization => Equals(localization.Language, InputLanguage)).Text = currentText;
            }
            GetTranslationDummyText(localizations, InputLanguage, PreferedLanguage);

            TextLocalization sourceLocalization = localizations.FirstOrDefault(loc =>
                Equals(loc.Language, InputLanguage));
            if (sourceLocalization != null)
            {
                foreach (TextLocalization localization in localizations)
                {
                    localization.KnownTranslations = TextLocalizationsUtils
                        .ExtractKnownTranslations(sourceLocalization.Text, localization.Language, dictOfDicts, InputLanguage);
                }
            }

            return localizations;
        }

        public override string GetGuiTranslationOfCurrentCulture(DependencyObject element)
        {
            GetControlProperties(element, out string controlId, out _, out string controlType, out string parentDialogName);
            if (string.IsNullOrWhiteSpace(parentDialogName) || string.IsNullOrWhiteSpace(controlId) || string.IsNullOrWhiteSpace(controlType))
            {
                return null;
            }

            return GetLiteral(Thread.CurrentThread.CurrentUICulture, parentDialogName, controlType, controlId).Text;
        }

        public override List<CultureInfo> GetKnownLanguages()
        {
            return FileProviderInstance.GetDictionary().Keys.ToList();
        }

        public override void SetGuiTranslation(DependencyObject element, ObservableCollection<TextLocalization> texts)
        {
            GetControlProperties(element, out string controlId, out _, out string controlType, out string parentDialogName);
            if (string.IsNullOrWhiteSpace(parentDialogName) || string.IsNullOrWhiteSpace(controlId) ||
                string.IsNullOrWhiteSpace(controlType) || texts == null)
            {
                /*should be logger*/
                Console.WriteLine(@"Failed to override translation for dialog '{0}', type '{1}' and name '{2}'.", parentDialogName, controlType, controlId);
                return;
            }

            SetLiteral(parentDialogName, controlType, controlId, texts);
        }

        protected override void CancelInitialization()
        {
            _status = ProviderStatus.CancellationInProgress;
            FileProviderInstance.CancelInitialization();
        }

        private void SetLiteral(string dialogName, string type, string elementName, ObservableCollection<TextLocalization> texts)
        {
            foreach (TextLocalization textLocalization in texts)
            {
                if (textLocalization?.Text != null)
                {
                    textLocalization.Text = ToEscapedString(textLocalization.Text);
                }
            }

            FileProviderInstance.Update(CreateGuiDictionaryKey(dialogName, type, elementName), texts);
        }

        private TextLocalization GetLiteral(CultureInfo language, string dialogName, string type, string elementName)
        {
            string result = null;
            string key = CreateGuiDictionaryKey(dialogName, type, elementName);

            var dictOfDicts = FileProviderInstance.GetDictionary();

            dictOfDicts.TryGetValue(language, out Dictionary<string, string> langDict);
            langDict?.TryGetValue(key, out result);

            if (result == null)
            {
                /*should be logger*/
                Console.WriteLine(@"Found no translation for dialog '{0}', type '{1}', name '{2}' and language '{3}'.",
                    dialogName, type, elementName, language);
            }
            else
            {
                result = EscapedStringToString(result);
            }

            return new TextLocalization { Language = language, Text = result };
        }

        private static string CreateGuiDictionaryKey(string dialogName, string type, string elementName = "")
        {
            char seperator = Properties.Settings.Default.Seperator_for_partial_Literalkeys;
            return dialogName + seperator + type + seperator + elementName;
        }

        private string EscapedStringToString(string s)
        {
            return s.Replace(@"\t", "\t").Replace(@"\r", "\r").Replace(@"\n", "\n").Replace(@"\slash", @"\");
        }

        private string ToEscapedString(string s)
        {
            StringBuilder builder = new StringBuilder();
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\t':
                        builder.Append(@"\t");
                        break;
                    case '\r':
                        builder.Append(@"\r");
                        break;
                    case '\n':
                        builder.Append(@"\n");
                        break;
                    case '\\':
                        //using @"\\" caused problems, when used with Replace(@"\\", @"\").Replace(@"\n", "\n")
                        //in situations like '\'+'n' -> "\\n" -> file -> "\\n" -> '\'+'n' -> '\n'
                        builder.Append(@"\slash");
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }

            return builder.ToString();
        }

        private static void GetControlProperties(DependencyObject element, out string controlId, out string currentText, out string controlType, out string parentDialogName)
        {
            parentDialogName = null;

            //determine Name, Type and current Text
            switch (element)
            {
                case RibbonTab ribbonTab:
                    {
                        controlId = ribbonTab.Name;
                        currentText = ribbonTab.Header as string;
                        //RibbonTabs count as "TabItem" for determining Literals
                        controlType = typeof(TabItem).Name;
                        break;
                    }
                case RibbonGroup ribbonGroup:
                    {
                        controlId = ribbonGroup.Name;
                        currentText = ribbonGroup.Header as string;
                        //RibbonGroups count as "TabItem" for determining Literals
                        controlType = typeof(TabItem).Name;
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
                        //RibbonButtons count as "Button" for determining Literals
                        controlType = typeof(Button).Name;
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
                            //column itself is not in the VisualTree, since it isn't a Visual
                            parentDialogName = GetParentDialogName(LogicalTreeUtils.GetDataGridParent(column));
                        }
                        catch
                        {
                            Console.WriteLine(@"Unable to find parent of DataGridColumn.");
                        }
                        break;
                    }
                default:
                    {
                        controlId = null;
                        currentText = null;
                        controlType = null;
                        break;
                    }
            }

            //determine Name of View or Window, if element isn't DataGridColumn
            if (parentDialogName == null)
            {
                parentDialogName = GetParentDialogName(element);
            }

            //to avoid misalignment while using ExcelFileProvider
            controlId = controlId?.Replace(Properties.Settings.Default.Seperator_for_partial_Literalkeys.ToString(), "");
            controlType = controlType?.Replace(Properties.Settings.Default.Seperator_for_partial_Literalkeys.ToString(), "");
            parentDialogName = parentDialogName?.Replace(Properties.Settings.Default.Seperator_for_partial_Literalkeys.ToString(), "");
        }

        private static string GetParentDialogName(object sender)
        {
            string parentDialogName = null;
            DependencyObject currentObject = (DependencyObject)sender;

            //move up the VisualTree, until UserControl or Window is found
            //search limeted to 40 iterations to stop infinite loops
            for (int i = 0; i < 40 && currentObject != null && parentDialogName == null; i++)
            {
                currentObject = VisualTreeHelper.GetParent(currentObject);

                switch (currentObject)
                {
                    case UserControl userControl:
                        parentDialogName = userControl.Name;

                        break;
                    case Window window:
                        parentDialogName = window.Name;

                        break;
                }
            }
            return parentDialogName;
        }
    }
}
