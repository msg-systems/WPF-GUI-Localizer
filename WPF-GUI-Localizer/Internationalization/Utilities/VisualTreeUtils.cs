using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Internationalization.Utilities
{
    public static class VisualTreeUtils
    {
        public static List<T> GetVisualChildCollection<T>(object parent) where T : DependencyObject
        {
            var visualCollection = new List<T>();

            //build list recursively
            GetVisualChildCollection(parent as DependencyObject, visualCollection);

            return visualCollection;
        }

        private static void GetVisualChildCollection<T>(DependencyObject parent, List<T> visualCollection)
            where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T variable)
                {
                    visualCollection.Add(variable);
                }

                GetVisualChildCollection(child, visualCollection);
            }
        }

        public static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            //get parent item
            var parentObject = VisualTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
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