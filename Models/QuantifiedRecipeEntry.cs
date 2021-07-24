using System.Collections.Generic;
using Windows.UI.Xaml;

namespace TerrariaCraftingCalculator.Models
{
    /// <summary>A <see cref="RecipeEntry"/> and its associated quantity (for crafting).</summary>
    public class QuantifiedRecipeEntry : DependencyObject
    {
        // STATIC PROPERTIES
        /// <summary>Dependency property to back the <see cref="Quantity"/> property.</summary>
        private static readonly DependencyProperty QuantityProperty =
            DependencyProperty.Register(
                nameof(Quantity),
                typeof(int),
                typeof(QuantifiedRecipeEntry),
                new PropertyMetadata(1));





        // INSTANCE PROPERTIES
        /// <summary>The recipe.</summary>
        public RecipeEntry Recipe { get; set; }
        /// <summary>The quantity associated with the recipe.</summary>
        public int Quantity
        {
            get { return (int)GetValue(QuantityProperty); }
            set { SetValue(QuantityProperty, value); }
        }
    }
}
