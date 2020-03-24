using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using Internationalization.LiteralProvider.Abstract;

namespace Internationalization.GUITranslator {
    public static class GuiTranslator {

        /// <summary>
        /// Goes through all elements of this View and writes the current translation into each element
        /// </summary>
        /// <param name="userControl">the View that is ment to be searched for translatable elements</param>
        public static void TranslateDialog(UserControl userControl) {
            if (userControl != null) {
                TranslateGui(userControl);
            }
        }

        /// <summary>
        /// Goes through all elements of this Window and writes the current translation into each element
        /// </summary>
        /// <param name="mainWindow">the Window that is ment to be searched for translatable elements</param>
        public static void TranslateWindow(Window mainWindow) {
            if (mainWindow != null) {
                TranslateGui(mainWindow);
            }
        }

        private static void TranslateGui(FrameworkElement visual) {

            //TranslateGuiElement will do nothing if visual is a non translatable like Grid
            TranslateGuiElement(visual);

            foreach (var childVisual in LogicalTreeHelper.GetChildren(visual)) {
                // If View or Window contain a TabControl - break foreach to prevent translating sub View
                if (childVisual is TabControl) {
                    break;
                }

                if (childVisual is FrameworkElement element) {
                    //also iterate all childs of visual, if they are FrameworkElements
                    //as DataGridColumns are not FrameworkElements, they require special treatment in TranslateGuiElement
                    TranslateGui(element);
                }
            }
        }

        public static void TranslateGuiElement(FrameworkElement visual)
        {
            //special treatment
            switch (visual)
            {
                case DataGrid grid:
                    HandleDataGrid(grid);
                    return;
                case Ribbon ribbon when ribbon.ApplicationMenu != null:
                    TranslateGui(ribbon.ApplicationMenu);
                    return;
            }

            string guiString = AbstractLiteralProvider.Instance.GetGuiTranslationOfCurrentCulture(visual);

            //visual is non translatable, doeasn't have a Name or not supported type like Grid, Stackpanel ...
            if (guiString == null)
            {
                return;
            }

            //write to element specific Property
            switch (visual)
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
            }
        }

        /// <summary>
        /// Since the recursive search in <see cref="TranslateGui"/> doesn't reach deeper than the DataGrid
        /// element itself, the Columns are iterated seperately here
        /// </summary>
        /// <param name="grid"></param>
        private static void HandleDataGrid(DataGrid grid)
        {
            foreach (var column in grid.Columns)
            {
                if (column == null) { return; }

                string guiString = AbstractLiteralProvider.Instance.GetGuiTranslationOfCurrentCulture(column);
                if (guiString != null)
                {
                    column.Header = guiString;
                }
            }
        }
    }
}
