using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using Internationalization.Utilities;
using Microsoft.Extensions.Logging;

namespace Internationalization.Localizer.LocalizerEventHandler
{
    public static class LocalizerEventHandler
    {
        private static readonly ILogger Logger;

        static LocalizerEventHandler()
        {
            Logger = GlobalSettings.LibraryLoggerFactory.CreateLogger(typeof(LocalizerEventHandler));
        }

        /// <summary>
        /// Attach Localizer to all supported GUI-elements
        /// </summary>
        /// <param name="parent">
        /// The parent-element, whose GUI-elements should have the translation action attached/detached to/from them.
        /// </param>
        /// <param name="isActive">If true handlers will be attached, if not they will be removed.</param>
        /// <param name="calledByLoaded">
        /// Whether this function was called by a "Loaded"-Event-Handler or not, determines whether the Event-Handler has
        /// to be removed from the event or not.
        /// </param>
        private static void ManageLocalizationEvents(DependencyObject parent, bool isActive, bool calledByLoaded)
        {
            Logger.Log(LogLevel.Debug, "Localizer will be {0} View (of type {1})",
                isActive ? "attached to" : "detached from", parent.DependencyObjectType.Name);

            //make sure views don't get initialized multiple times.
            if (calledByLoaded)
            {
                var parentFrameworkElement = (FrameworkElement) parent;
                parentFrameworkElement.Loaded -= ElementInitialized;
                Logger.Log(LogLevel.Debug, "Event handler was removed from Loaded-Event.");
            }
            else
            {
                Logger.Log(LogLevel.Debug, "Event handler was not removed from Loaded-Event.");
            }

            var supportedElements = new List<FrameworkElement>();

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

            //Since the LogicalTree doesn't include the DataGrids Headers,
            //this distinction will be made in OpenLocalizationDialog.
            supportedElements.AddRange(LogicalTreeUtils.GetLogicalChildCollection<DataGrid>(parent));

            if (isActive)
            {
                foreach (var element in supportedElements)
                    element.MouseRightButtonUp += LocalizerDialogHandler.LocalizerDialogHandler.OpenLocalizationDialog;
                Logger.Log(LogLevel.Debug, "Localizer was successfully attached to {0} elements.",
                    supportedElements.Count);
            }
            else
            {
                foreach (var element in supportedElements)
                    element.MouseRightButtonUp -= LocalizerDialogHandler.LocalizerDialogHandler.OpenLocalizationDialog;
                Logger.Log(LogLevel.Debug, "Localizer was successfully detached from {0} elements.",
                    supportedElements.Count);
            }
        }

        /// <summary>
        /// Initiates the Attachment of OpenLocalizationDialog to the Views GUI-elements.
        /// </summary>
        /// <param name="sender">View, whose GUI-elements should have the translation action attached to them.</param>
        /// <param name="e"></param>
        public static void ElementInitialized(object sender, EventArgs e)
        {
            ManageLocalizationEvents(sender as DependencyObject, true, true);
        }

        /// <summary>
        /// Attaches OpenLocalizationDialog to the Views GUI-elements immediately.
        /// </summary>
        /// <param name="sender">
        /// View, whose GUI-elements should have the translation action removed from their MouseEvent.
        /// It is assumed that the View is already loaded, if not attach <see cref="ElementInitialized"/>
        /// to the Loaded-Event of the View.
        /// </param>
        public static void AttachLocalizationHelper(object sender)
        {
            ManageLocalizationEvents(sender as DependencyObject, true, false);
        }

        /// <summary>
        /// Undoes the Attachment of OpenLocalizationDialog to the Views GUI-elements immediately.
        /// </summary>
        /// <param name="sender">View, whose GUI-elements should have the translation action removed from their MouseEvent.
        /// It is assumed that the View is already loaded, if not attach <see cref="ElementInitialized"/>
        /// to the Loaded-Event of the View.
        /// </param>
        public static void DetachLocalizationHelper(object sender)
        {
            ManageLocalizationEvents(sender as DependencyObject, false, false);
        }
    }
}