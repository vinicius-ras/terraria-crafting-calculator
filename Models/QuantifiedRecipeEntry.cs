using System.Collections.Generic;

namespace TerrariaCraftingCalculator.Models
{
    /// <summary>A <see cref="RecipeEntry"/> and its associated quantity (for crafting).</summary>
    public class QuantifiedRecipeEntry
    {
        /// <summary>The recipe.</summary>
        public RecipeEntry Recipe { get; set; }
        /// <summary>The quantity associated with the recipe.</summary>
        public int Quantity { get; set; }
    }
}
