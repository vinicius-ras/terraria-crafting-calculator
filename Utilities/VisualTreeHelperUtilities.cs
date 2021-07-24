using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace TerrariaCraftingCalculator.Extensions
{
    /// <summary>Utility methods for the <see cref="VisualTreeHelper"/> class.</summary>
    static class VisualTreeHelperUtilities
    {
        /// <summary>Walks the visual tree starting from a node and searching on the parent nodes direction, until it reaches a parent node of the specified type.</summary>
        /// <typeparam name="T">The target type of parent node being searched for.</typeparam>
        /// <param name="startingObject">
        ///     A node to be the starting point of the search.
        ///     Notice that if that node is an instance of the target type <typeparamref name="T"/>, that node will be returned as the result of the search.
        /// </param>
        /// <returns>
        ///     Returns the first node of the given type <typeparamref name="T"/> that is found on the tree.
        ///     If there are no nodes in the tree that correspond to the expected type, returns <c>null</c>.
        ///     Also notice that if the <paramref name="startingObject"/> node is of type <typeparamref name="T"/>, it will be returned as the result of this method.
        /// </returns>
        public static T FindClosestParentOfType<T>(DependencyObject startingObject)
            where T : DependencyObject
        {
            while (startingObject != null && startingObject is T == false)
                startingObject = VisualTreeHelper.GetParent(startingObject);
            return (T) startingObject;
        }


        /// <summary>Retrieves an <see cref="IEnumerable{T}"/> containing all children from the given element in the visual tree.</summary>
        /// <remarks>LINQ can be used to further post-process the returned list, looking for specific children objects via filtering by property values, types, etc.</remarks>
        /// <param name="visualTreeHelper">Reference to the <see cref="VisualTreeHelper"/> object being used to perform the search.</param>
        /// <param name="parentNode">The parent node in the tree, whose children will be returned.</param>
        /// <returns>
        ///     Returns the first node of the given type <typeparamref name="T"/> that is found on the tree.
        ///     If there are no nodes in the tree that correspond to the expected type, returns <c>null</c>.
        ///     Also notice that if the <paramref name="parentNode"/> node is of type <typeparamref name="T"/>, it will be returned as the result of this method.
        /// </returns>
        public static IEnumerable<DependencyObject> GetAllChildren(DependencyObject parentNode)
        {
            var childrenStack = new Stack<DependencyObject>();
            childrenStack.Push(parentNode);
            while (childrenStack.Count > 0)
            {
                var currentNode = childrenStack.Pop();
                int totalChildren = VisualTreeHelper.GetChildrenCount(currentNode);
                for (int childIndex = 0; childIndex < totalChildren; childIndex++)
                {
                    var child = VisualTreeHelper.GetChild(currentNode, childIndex);
                    childrenStack.Push(child);
                    yield return child;
                }
            }
        }
    }
}
