using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Input;
using Internationalization.GUITranslator;
using Internationalization.LiteralProvider.Abstract;
using Internationalization.Model;
using Internationalization.View;
using Microsoft.Extensions.Logging;

namespace Internationalization.Utilities
{
    public static class LocalizationUtils
    {
        private static readonly ILogger Logger;

        static LocalizationUtils()
        {
            Logger = GlobalSettings.LibraryLoggerFactory.CreateLogger(typeof(LocalizationUtils));
        }
        /// <summary>
        /// Attach <see cref="OpenLocalizationDialog"/> to all supported GUI-elements
        /// </summary>
        /// <param name="parent">
        /// The parent-element, whose GUI-elements should have the translation action attached/dettached to/from them
        /// </param>
        /// <param name="isActive">if true handers will be attached, if not they will be removed</param>
        /// <param name="calledByLoaded">
        /// whether this function was called by a Loaded Event Handler or not, determines whether the Event Handler has
        /// to be removed from the event or not
        /// </param>
        private static void ManageLocalizationEvents(DependencyObject parent, bool isActive, bool calledByLoaded)
        {
            //make sure views don't get initialized multiple times
            if (calledByLoaded)
            {
                var parentFrameworkElement = (FrameworkElement)parent;
                parentFrameworkElement.Loaded -= ElementInitialized;
            }

            List<FrameworkElement> supportedElements = new List<FrameworkElement>();

            //read all supported child elements from parent
            supportedElements.AddRange(LogicalTreeUtils.GetLogicalChildCollection<RibbonTab>(parent));
            supportedElements.AddRange(LogicalTreeUtils.GetLogicalChildCollection<RibbonGroup>(parent));
            supportedElements.AddRange(LogicalTreeUtils.GetLogicalChildCollection<RibbonButton>(parent));
            supportedElements.AddRange(LogicalTreeUtils.GetLogicalChildCollection<Label>(parent));
            supportedElements.AddRange(LogicalTreeUtils.GetLogicalChildCollection<Button>(parent));
            supportedElements.AddRange(LogicalTreeUtils.GetLogicalChildCollection<TabItem>(parent));
            supportedElements.AddRange(LogicalTreeUtils.GetLogicalChildCollection<RadioButton>(parent));
            supportedElements.AddRange(LogicalTreeUtils.GetLogicalChildCollection<TextBlock>(parent));
            supportedElements.AddRange(LogicalTreeUtils.GetLogicalChildCollection<CheckBox>(parent));

            //Since the LogicalTree doesn't include the DataGrids Headers, this distinction will be made OpenLocalizationDialog
            supportedElements.AddRange(LogicalTreeUtils.GetLogicalChildCollection<DataGrid>(parent));

            if (isActive)
            {
                foreach (var element in supportedElements) element.MouseRightButtonUp += OpenLocalizationDialog;
            }
            else
            {
                foreach (var element in supportedElements) element.MouseRightButtonUp -= OpenLocalizationDialog;
            }
        }

        /// <summary>
        /// Initiates the Attachment of OpenLocalizationDialog to the Views GUI-elements
        /// </summary>
        /// <param name="sender">View, whose GUI-elements should have the translation action attached to them</param>
        /// <param name="e"></param>
        public static void ElementInitialized(object sender, EventArgs e)
        {
            ManageLocalizationEvents(sender as DependencyObject, true, true);
        }

        /// <summary>
        /// Attaches OpenLocalizationDialog to the Views GUI-elements immediately
        /// </summary>
        /// <param name="sender">View, whose GUI-elements should have the translation action removed from their MouseEvent</param>
        public static void AttachLocalizationHelper(object sender)
        {
            ManageLocalizationEvents(sender as DependencyObject, true, false);
        }

        /// <summary>
        /// Undoes the Attachment of OpenLocalizationDialog to the Views GUI-elements immediately
        /// </summary>
        /// <param name="sender">View, whose GUI-elements should have the translation action removed from their MouseEvent</param>
        public static void DettachLocalizationHelper(object sender)
        {
            ManageLocalizationEvents(sender as DependencyObject, false, false);
        }

        /// <summary>
        /// Manages communication for the Mouseclick Event:
        /// takes care of edge cases TabItem and DataGrid,
        /// gets current translations from LiteralProvider,
        /// opens LocalizationDialog with current translations,
        /// gives updated translations from LocalizationDialog back to the LiteralProvider,
        /// activates GUITranslator to translate the sender element
        /// </summary>
        /// <param name="sender">GUI-element, whose translations are ment to be updated</param>
        /// <param name="e">
        /// Event Parameter / Info (used for edge cases and to prevent this function from
        /// being called multiple times from nested GUI-Elements)
        /// </param>
        private static void OpenLocalizationDialog(object sender, MouseButtonEventArgs e)
        {
            switch (sender)
            {
                case TabItem tabItem:
                {
                    //a TabItem may contain nested Elements, make sure the tabItem itself was clicked
                    if ( !(e.Source is TabItem && tabItem.Header is string) ) { return; }

                    break;
                }
                case DataGrid _:
                {
                    //find the DataGridColumnHeader that was originally clicked
                    //use the VisualTree, as Headers aren't found in the LogicalTree
                    var columnHeader = VisualTreeUtils.FindVisualParent<DataGridColumnHeader>((DependencyObject)e.OriginalSource);

                    if (columnHeader == null)
                    {
                        //Header wasn't clicked
                        return;
                    }

                    sender = columnHeader;
                    
                    break;
                }
            }

            // After having verified that sender object given to this function is the one ment to be translated,
            // prevent the event from propagating further
            e.Handled = true;

            //get translations
            ObservableCollection<TextLocalization> localizedTexts = AbstractLiteralProvider.Instance.GetGuiTranslation((FrameworkElement) sender);
            if (localizedTexts == null)
            {
                return;
            }

            //extract InputLanguage
            var inputLanguageLocalization = localizedTexts.First(localization => Equals(localization.Language, AbstractLiteralProvider.Instance.InputLanguage));
            localizedTexts.Remove(inputLanguageLocalization);
            //show LocalizationDialog with InputLanguage seperated
            var localizationDialog = new LocalizationInputDialog
            {
                InputLocalization = inputLanguageLocalization,
                LocalizedTexts = localizedTexts
            };
            if (localizationDialog.ShowDialog() == false)
            {
                return;
            }

            //give updated texts back to LiteralProvider
            localizedTexts.Add(localizationDialog.InputLocalization); //localizedTexts (also inputLanguageLocalization) is already updated with out grabbing new value from dialog window
            AbstractLiteralProvider.Instance.SetGuiTranslation((FrameworkElement)sender, localizedTexts);

            //activate GuiTranslator, independent of how LiteralProvider operated, potentially unwanted?
            switch (sender)
            {
                case DataGridColumnHeader asColumnHeader:
                    try
                    {
                        GuiTranslator.TranslateGuiElement(LogicalTreeUtils.GetDataGridParent(asColumnHeader.Column));
                    }
                    catch
                    {
                        Logger.Log(LogLevel.Debug,
                            @"Unable to update new translation for DataGrid in GUI");
                    }

                    break;
                case FrameworkElement asFrameworkElement:
                    GuiTranslator.TranslateGuiElement(asFrameworkElement);
                    break;
            }
        }
    }
}
