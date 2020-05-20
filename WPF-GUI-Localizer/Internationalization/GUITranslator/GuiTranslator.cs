using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using Internationalization.LiteralProvider.Abstract;
using Internationalization.Utilities;
using Microsoft.Extensions.Logging;

namespace Internationalization.GUITranslator
{
    public static class GuiTranslator
    {
        private static readonly ILogger Logger;

        static GuiTranslator()
        {
            Logger = GlobalSettings.LibraryLoggerFactory.CreateLogger(typeof(GuiTranslator));
        }

        /// <summary>
        /// Goes through all elements nested inside <paramref name="rootElement"/> and writes the current
        /// translation into each element.
        /// </summary>
        /// <param name="rootElement">
        /// The element, based on which all nested elements will recursively be translated.
        /// <paramref name="rootElement"/> itself will also be translated.
        /// </param>
        public static void TranslateGui(FrameworkElement rootElement)
        {
            //null check.
            ExceptionLoggingUtils.ThrowIfNull(Logger, rootElement, nameof(rootElement),
                "Unable to translate null UserControl / Window.",
                "TranslateGui received null as root element for translation.");

            //TranslateGuiElement will do nothing if visual is a non translatable like Grid.
            TranslateGuiElement(rootElement);

            foreach (var childVisual in LogicalTreeHelper.GetChildren(rootElement))
            {
                //if View or Window contain a TabControl - break foreach to prevent translating sub View.
                if (childVisual is TabControl)
                {
                    break;
                }

                if (childVisual is FrameworkElement element)
                {
                    //also iterate all childs of visual, if they are FrameworkElements.
                    //as DataGridColumns are not FrameworkElements, they require special treatment in TranslateGuiElement.
                    TranslateGui(element);
                }
            }
        }

        /// <summary>
        /// Translates only the given element.
        /// </summary>
        /// <param name="visual"></param>
        private static void TranslateGuiElement(FrameworkElement visual)
        {
            //special treatment.
            switch (visual)
            {
                case DataGrid grid:
                    HandleDataGrid(grid);
                    return;
                case Ribbon ribbon when ribbon.ApplicationMenu != null:
                    TranslateGui(ribbon.ApplicationMenu);
                    return;
                default:
                    //control flow can continue.
                    break;
            }

            //TODO vorher checken ob element übersetzt werden kann?
            var guiString = AbstractLiteralProvider.Instance.GetGuiTranslationOfCurrentCulture(visual);

            //visual is non translatable, does not have a Name or not supported type like Grid, Stackpanel ...
            if (guiString == null)
            {
                return;
            }

            //write to element specific Property.
            ControlElementInspector.WriteToControlElement(visual, guiString);
        }

        /// <summary>
        /// Since the recursive search in <see cref="TranslateGui"/> doesn't reach deeper than the DataGrid
        /// element itself, the Columns are iterated seperately here
        /// </summary>
        /// <param name="grid">The DataGrid that should be translated</param>
        private static void HandleDataGrid(DataGrid grid)
        {
            foreach (var column in grid.Columns)
            {
                if (column == null)
                {
                    return;
                }

                var guiString = AbstractLiteralProvider.Instance.GetGuiTranslationOfCurrentCulture(column);
                if (guiString != null)
                {
                    column.Header = guiString;
                }
            }
        }
    }
}