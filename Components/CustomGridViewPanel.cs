using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace TerrariaCraftingCalculator.Components
{
    /// <summary>A custom panel whose measurements are obtained from the largest child of a given parent UI control.</summary>
    public class CustomGridViewPanel : Panel
    {
        // INSTANCE METHODS
        /// <summary>Retrieves the maximum dimentions of the given collection of UI elements.</summary>
        /// <remarks>The maximum dimensions are calculated independently for the X and Y axes.</remarks>
        /// <param name="uiElements">The collection of elements for which the maximum dimensions will be calculated.</param>
        /// <returns>Returns the calculated maximum dimensions of the given elements.</returns>
        private Size GetMaximumSize(UIElementCollection uiElements) =>
            uiElements.Count == 0
                ? new Size(0, 0)
                : new Size(
                    uiElements.Max(element => element.DesiredSize.Width),
                    uiElements.Max(element => element.DesiredSize.Height)
                );





        // OVERRIDES: Panel (FrameworkElement)
        protected override Size MeasureOverride(Size availableSize)
        {
            // Call the measure method for all children, so that they will update their DesiredSize properties.
            // Then, find the largest of the children.
            foreach (var child in Children)
                child.Measure(availableSize);
            var maxChildSize = GetMaximumSize(Children);

            // Calculate the total required size for the panel to contain all children
            var totalSize = new Size(availableSize.Width, maxChildSize.Height);
            int currentRow = 0;
            double currentRowWidth = 0;
            foreach (var child in Children)
            {
                if (currentRowWidth + maxChildSize.Width > availableSize.Width)
                {
                    currentRow++;
                    currentRowWidth = maxChildSize.Width;
                    totalSize.Height += maxChildSize.Height;
                }
                else
                    currentRowWidth += maxChildSize.Width;
            }
            return totalSize;
        }


        protected override Size ArrangeOverride(Size finalSize)
        {
            var maxChildSize = GetMaximumSize(Children);
            int currentRow = 0, currentCol = 0;
            double currentRowWidth = 0;
            foreach (var child in Children)
            {
                if (currentRowWidth + maxChildSize.Width > finalSize.Width)
                {
                    currentRow++;
                    currentRowWidth = maxChildSize.Width;
                    currentCol = 0;
                }
                else
                    currentRowWidth += maxChildSize.Width;

                child.Arrange(new Rect(
                    currentCol * maxChildSize.Width,
                    currentRow * maxChildSize.Height,
                    maxChildSize.Width,
                    maxChildSize.Height));
                currentCol++;
            }
            return finalSize;
        }
    }
}
