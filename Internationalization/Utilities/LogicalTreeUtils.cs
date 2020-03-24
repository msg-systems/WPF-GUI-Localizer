using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Internationalization.Utilities {
    public static class LogicalTreeUtils {
     
        public static List<T> GetLogicalChildCollection<T>(object parent) where T : DependencyObject {
            var logicalCollection = new List<T>();

            //build list recursively
            GetLogicalChildCollection(parent as DependencyObject, logicalCollection);

            return logicalCollection;
        }

        private static void GetLogicalChildCollection<T>(DependencyObject parent, ICollection<T> logicalCollection)
            where T : DependencyObject
        {
            foreach (var child in LogicalTreeHelper.GetChildren(parent))
            {
                //if child is of type T, collect it
                if (child is T variable)
                {
                    logicalCollection.Add(variable);
                }

                //independent of child being T, if LogicalTree can continue, call recursively
                if (child is DependencyObject depChild)
                {
                    GetLogicalChildCollection(depChild, logicalCollection);
                }
            }
        }

        public static DataGrid GetDataGridParent(DataGridColumn column)
        {
            PropertyInfo propertyInfo = column.GetType().GetProperty("DataGridOwner", BindingFlags.Instance | BindingFlags.NonPublic);
            if (propertyInfo == null)
            {
                return null;
            }
            return propertyInfo.GetValue(column, null) as DataGrid;
        }
    }
}