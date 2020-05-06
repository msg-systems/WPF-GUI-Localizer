using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Internationalization.GUITranslator;
using Internationalization.LiteralProvider.Abstract;
using Internationalization.Model;
using Internationalization.Utilities;
using Internationalization.View;
using Microsoft.Extensions.Logging;

namespace Internationalization.Localizer.LocalizerDialogHandler
{
    public static class LocalizerDialogHandler
    {
        private static readonly ILogger Logger;

        static LocalizerDialogHandler()
        {
            Logger = GlobalSettings.LibraryLoggerFactory.CreateLogger(typeof(LocalizerDialogHandler));
        }

        /// <summary>
        /// Manages communication for the Mouseclick Event:
        /// takes care of edge cases TabItem and DataGrid,
        /// gets current translations from LiteralProvider,
        /// opens LocalizationDialog with current translations,
        /// gives updated translations from LocalizationDialog back to the LiteralProvider,
        /// activates GUITranslator to translate the sender element.
        /// </summary>
        /// <param name="sender">GUI-element, whose translations are ment to be updated.</param>
        /// <param name="e">
        /// Event Parameter / Info (used for edge cases and to prevent this function from
        /// being called multiple times from nested GUI-Elements).
        /// </param>
        public static void OpenLocalizationDialog(object sender, MouseButtonEventArgs e)
        {
            if (!CorrectElementWasClicked(ref sender, e))
            {
                Logger.Log(LogLevel.Trace, "Click event was ignored, because the part of the " +
                                           "element that was clicked is not translateble.");
                return;
            }

            //check for translations.
            var localizedTexts =
                AbstractLiteralProvider.Instance.GetGuiTranslation((FrameworkElement) sender);
            if (localizedTexts == null)
            {
                Logger.Log(LogLevel.Trace, "Click event was ignored, because no translations were found.");
                return;
            }

            //after having verified that the sender object given to this function is the
            //one meant to be translated and that a translation does exist, prevent the
            //event from being handled further (to other elements the sender element was
            //nested inside of).
            e.Handled = true;
            Logger.Log(LogLevel.Trace, "Click event activated. Will be handled.");

            if (!LetUserModifyLocalizations(ref localizedTexts))
            {
                Logger.Log(LogLevel.Trace, "Editing of localized Texts aborted by user.");
                return;
            }
            AbstractLiteralProvider.Instance.SetGuiTranslation((FrameworkElement) sender, localizedTexts);
            Logger.Log(LogLevel.Trace, "Edited localized texts saved in LiteralProvider.");

            //activate GuiTranslator, depending on GlobalSettings.
            RunTranslator(sender);

            Logger.Log(LogLevel.Trace, "Finished handling Click-Event.");
        }

        /// <summary>
        /// Checks if the edgecases TabItem and Datagrid acctually need to be handled.
        /// TabItems may need to be translated themselves or an element nested inside them
        /// (e.g. a Lable). Since DataGridHeaders were not individually handled in
        /// LocalizerEventHandler, they are handled here.
        /// </summary>
        /// <param name="senderObject">
        /// The object that triggered the MouseEvent.
        /// If it is a DataGrid it will be exchanged for the DataGridHeader that was clicked.
        /// </param>
        /// <param name="eventArgs">EventArgs received by event handler.</param>
        /// <returns>
        /// True if correct element was clicked. False, if this event is not meant to be handled.
        /// </returns>
        private static bool CorrectElementWasClicked(ref object senderObject, MouseButtonEventArgs eventArgs)
        {
            switch (senderObject)
            {
                case TabItem tabItem:
                {
                    //a TabItem may contain nested Elements, make sure the tabItem itself was clicked.
                    //this is part of the edge cases, because
                    return eventArgs.Source is TabItem && tabItem.Header is string;
                }
                case DataGrid _:
                {
                    //find the DataGridColumnHeader that was originally clicked
                    //use the VisualTree, as Headers aren't found in the LogicalTree.
                    //eventArgs.OriginalSource can be cast to DepencyObject, as
                    //FindVisualParent returns null in that case.
                    var columnHeader =
                        VisualTreeUtils.FindVisualParent<DataGridColumnHeader>(
                            eventArgs.OriginalSource as DependencyObject);

                    if (columnHeader == null)
                    {
                        //Header wasn't clicked.
                        return false;
                    }

                    senderObject = columnHeader;

                    return true;
                }
                default:
                    //if senderObject is not considered edgecase, it will be assumed to be correct.
                    return true;
            }
        }

        /// <summary>
        /// Opens a <see cref="LocalizationInputDialog"/> Window and writes changes in
        /// <paramref name="originalLocalizations"/>. Returns false, if editing was aborted by user.
        /// </summary>
        private static bool LetUserModifyLocalizations(
            ref ObservableCollection<TextLocalization> originalLocalizations)
        {
            //extract InputLanguage.
            var inputLanguageLocalization = originalLocalizations.First(localization =>
                Equals(localization.Language, AbstractLiteralProvider.Instance.InputLanguage));
            originalLocalizations.Remove(inputLanguageLocalization);

            //show LocalizationDialog with InputLanguage seperated.
            var localizationDialog = new LocalizationInputDialog
            {
                InputLocalization = inputLanguageLocalization,
                LocalizedTexts = originalLocalizations
            };
            if (localizationDialog.ShowDialog() == false)
            {
                return false;
            }

            //localizedTexts is already updated without
            //grabbing new value from dialog window explicitly.
            originalLocalizations.Add(localizationDialog.InputLocalization);

            return true;
        }

        /// <summary>
        /// Calls <see cref="GuiTranslator"/> for the given object, if this functionality is not
        /// turned off in <see cref="GlobalSettings"/> and the given object is of type
        /// FrameworkElement or DataGridColumnHeader.
        /// </summary>
        private static void RunTranslator(object objectToBeTranslated)
        {
            if (GlobalSettings.UseGuiTranslatorForLocalizationUtils)
            {
                switch (objectToBeTranslated)
                {
                    case DataGridColumnHeader asColumnHeader:
                        try
                        {
                            GuiTranslator.TranslateGui(
                                LogicalTreeUtils.GetDataGridParent(asColumnHeader.Column));
                            Logger.Log(LogLevel.Trace, "Translation of DataGridColumn successfully updated.");
                        }
                        catch
                        {
                            Logger.Log(LogLevel.Information,
                                "Unable to update new translation for DataGrid in GUI.");
                        }

                        break;
                    case FrameworkElement asFrameworkElement:
                        GuiTranslator.TranslateGui(asFrameworkElement);
                        Logger.Log(LogLevel.Trace, "Translation of element successfully updated.");
                        break;
                    default:
                        //no action, if GuiTranslator in unable to translate objectToBeTranslated.
                        Logger.Log(LogLevel.Information, "Translation of element was not successfully updated, " +
                                                   "because it is not a Framework element.");
                        break;
                }
            }
        }
    }
}
