using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Media;
using Internationalization.AttachedProperties;
using Microsoft.Extensions.Logging;

namespace Internationalization.Utilities
{
    public static class ControlElementInspector
    {
        private static readonly ILogger Logger;

        static ControlElementInspector()
        {
            Logger = GlobalSettings.LibraryLoggerFactory.CreateLogger(typeof(ControlElementInspector));
        }

        /// <summary>
        /// Reads the properties <paramref name="controlId"/>, <paramref name="currentText"/>,
        /// <paramref name="controlType"/> and <paramref name="parentDialogName"/> of the given
        /// <paramref name="element"/>.
        /// Returns true, if <paramref name="element"/> is eligible for translation.
        /// </summary>
        /// <param name="element">The element that should be inspected.</param>
        /// <param name="controlId">
        /// The Name Property of <paramref name="element"/>. This value is not null, if true is returned.
        /// </param>
        /// <param name="currentText">
        /// The Content / Text / Header Property of <paramref name="element"/> that is currently displayed in the
        /// GUI. This value can be null, if true is returned.
        /// </param>
        /// <param name="controlType">
        /// The Type of <paramref name="element"/>. This value is not null, if true is returned.
        /// </param>
        /// <param name="parentDialogName">
        /// The Name Property of the UserControl or Window, <paramref name="element"/> is contained inside.
        /// This value is not null, if true is returned.
        /// </param>
        /// <returns>True, if <paramref name="element"/> is eligible for translation.</returns>
        /// <exception cref="ArgumentNullException">Thrown, if <paramref name="element"/> is null.</exception>
        public static bool GetControlProperties(DependencyObject element, out string controlId, out string currentText,
            out string controlType, out string parentDialogName)
        {
            ExceptionLoggingUtils.ThrowIfNull(Logger, nameof(GetControlProperties), element,
                nameof(element), "Unable to get control properties of null element");

            //determine Name, Type and current Text.
            if (!GetElementSpecificControlProperties(element, out controlId, out currentText,
                out controlType, out parentDialogName))
            {
                return false;
            }

            //determine Name of View or Window, if element isn't DataGridColumn.
            if (parentDialogName == null)
            {
                parentDialogName = GetParentDialogName(element);
            }

            //determine again, if element can be translated.
            //if properties like Name are not set in XAML, they can still be string.Empty instead of null.
            if (string.IsNullOrEmpty(controlId) || string.IsNullOrEmpty(controlType) ||
                string.IsNullOrEmpty(parentDialogName))
            {
                return false;
            }

            //to avoid misalignment while using ExcelFileProvider.
            controlId = controlId.Replace(Properties.Settings.Default.Seperator_for_partial_Literalkeys.ToString(),
                "");
            controlType = controlType.Replace(Properties.Settings.Default.Seperator_for_partial_Literalkeys.ToString(),
                "");
            parentDialogName =
                parentDialogName.Replace(Properties.Settings.Default.Seperator_for_partial_Literalkeys.ToString(), "");

            return true;
        }

        private static bool GetElementSpecificControlProperties(DependencyObject element, out string controlId,
            out string currentText, out string controlType, out string parentDialogName)
        {
            parentDialogName = null;

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
                            Logger.Log(LogLevel.Debug, "Unable to find parent of DataGridColumn.");
                        }

                        break;
                    }
                default:
                    {
                        controlId = null;
                        currentText = null;
                        controlType = null;
                        Logger.Log(LogLevel.Trace,
                            $"GetControlProperties was called for non translatable type ({element.GetType()}).");

                        return false;
                    }
            }

            return true;
        }

        /// <summary>
        /// Sets the <paramref name="element"/> specific Property to <paramref name="guiString"/>.
        /// </summary>
        /// <param name="element">The element whose Property should be set.</param>
        /// <param name="guiString">The text that should be displayed in the GUI.</param>
        /// <exception cref="ArgumentNullException">Thrown, if <paramref name="element"/> is null.</exception>
        public static void WriteToControlElement(FrameworkElement element, string guiString)
        {
            ExceptionLoggingUtils.ThrowIfNull(Logger, nameof(WriteToControlElement), element,
                nameof(element), "Unable to write new gui text to null element.");

            switch (element)
            {
                case RibbonTab tab:
                    tab.Header = guiString;
                    break;
                case RibbonGroup ribbonGroup:
                    ribbonGroup.Header = guiString;
                    break;
                case RibbonButton button:
                    button.Label = guiString;
                    break;
                case RibbonRadioButton button:
                    button.Content = guiString;
                    break;
                case RibbonApplicationMenuItem menuItem:
                    menuItem.Header = guiString;
                    break;
                case Label label:
                    label.Content = guiString;
                    break;
                case Button button:
                    if (button.Content is string || button.Content == null)
                    {
                        button.Content = guiString;
                    }

                    break;
                case TabItem tabItem:
                    tabItem.Header = guiString;
                    break;
                case RadioButton radioButton:
                    radioButton.Content = guiString;
                    break;
                case TextBlock textBlock:
                    textBlock.Text = guiString;
                    break;
                case CheckBox checkBox:
                    checkBox.Content = guiString;
                    break;
                default:
                    Logger.Log(LogLevel.Debug, $"Unable to translate unkown type ({element.GetType()}) "
                                               + $"with not translation ({guiString}).");
                    break;
            }
        }

        private static string GetParentDialogName(object sender)
        {
            string parentDialogName = null;
            var currentObject = sender as DependencyObject;

            //move up the VisualTree, until UserControl or Window is found.
            while (currentObject != null && parentDialogName == null)
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
