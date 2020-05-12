using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.Extensions.Logging;

namespace Internationalization.Utilities
{
    public static class VisualTreeUtils
    {
        private static readonly ILogger Logger;

        static VisualTreeUtils()
        {
            Logger = GlobalSettings.LibraryLoggerFactory.CreateLogger(typeof(VisualTreeUtils));
        }

        /// <summary>
        /// Collects all children of type T starting at <paramref name="parent"/>. Going through the VisualTree.
        /// </summary>
        /// <typeparam name="T">
        /// The type that all children need to satisfy in order to be included in the returned list.
        /// </typeparam>
        /// <param name="parent">Root element of the recursive search.</param>
        /// <returns>
        /// List of all child elements that were found.
        /// Will return an empty list, if the <paramref name="parent"/> object is not a DependencyObject
        /// or null.
        /// </returns>
        public static IList<T> GetVisualChildCollection<T>(object parent) where T : DependencyObject
        {
            var visualCollection = new List<T>();

            if (!(parent is DependencyObject depParent)) return visualCollection;

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(depParent); i++)
            {
                var child = VisualTreeHelper.GetChild(depParent, i);

                if (child is T variable)
                {
                    visualCollection.Add(variable);
                }

                //independent of child being T or not, if VisualTree can continue, call recursively.
                if (child is DependencyObject depChild)
                {
                    visualCollection = visualCollection.Concat(GetVisualChildCollection<T>(depChild)).ToList();
                }
            }

            return visualCollection;
        }

        /// <summary>
        /// Tries to find the closest parent of given parameter <paramref name="child"/>
        /// in the VisualTree that satisfies the typeparameter <paramref name="T"/>.
        /// Returns null if no fitting parent is found.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown, if <paramref name="child"/> is null.</exception>
        public static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            ExceptionLoggingUtils.ThrowIfNull(Logger, nameof(FindVisualParent), child,
                nameof(child), "Unable to find visual parent of null.");
            
            var parentObject = VisualTreeHelper.GetParent(child);

            //end of the tree, nothing found.
            if (parentObject == null) return null;

            if (parentObject is T parent)
            {
                return parent;
            }
            else
            {
                return FindVisualParent<T>(parentObject);
            }
        }
    }
}