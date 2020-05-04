using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Internationalization.Utilities
{
    public static class LogicalTreeUtils
    {
        /// <summary>
        /// Collects all children of type T starting at <paramref name="parent"/>. Going through the LogicalTree.
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
        public static IList<T> GetLogicalChildCollection<T>(object parent) where T : DependencyObject
        {
            var logicalCollection = new List<T>();

            if (!(parent is DependencyObject depParent)) return logicalCollection;

            foreach (var child in LogicalTreeHelper.GetChildren(depParent))
            {
                //if child is of type T, collect it.
                if (child is T variable)
                {
                    logicalCollection.Add(variable);
                }

                //independent of child being T or not, if LogicalTree can continue, call recursively.
                if (child is DependencyObject depChild)
                {
                    logicalCollection = logicalCollection.Concat(GetLogicalChildCollection<T>(depChild)).ToList();
                }
            }

            return logicalCollection;
        }

        /// <summary>
        /// Returns the DataGrid of the given DataGridColumn.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown, if DataGrid of given DataGridColumn cannot be accessed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown, if given DataGridColumn is null.
        /// </exception>
        public static DataGrid GetDataGridParent(DataGridColumn column)
        {
            if (column == null)
            {
                throw new ArgumentNullException(nameof(column), "Unable find parent DataGrid of null.");
            }
            var propertyInfo = column.GetType()
                .GetProperty("DataGridOwner", BindingFlags.Instance | BindingFlags.NonPublic);
            if (propertyInfo == null)
            {
                throw new InvalidOperationException("Unable to access parent DataGrid.");
            }

            return propertyInfo.GetValue(column, null) as DataGrid;
        }
    }
}