using System.Collections.Generic;

namespace TerrariaCraftingCalculator.Models
{
    /// <summary>Represents a recipe to make an item (ingredients, resulting items, and crafting station).</summary>
    class RecipeEntry
    {
        /// <summary>A description of the item(s) that will be the result of the recipe.</summary>
        public QuantifiedItemEntry ResultingItem { get; set; }
        /// <summary>The ingredients that compose the recipe.</summary>
        public IEnumerable<QuantifiedItemEntry> Ingredients { get; set; }
        /// <summary>The crafting station where the recipe is to be executed.</summary>
        public IEnumerable<ItemEntry> CraftingStations { get; set; }
    }
}
