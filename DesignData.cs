using System.Collections.Generic;
using TerrariaCraftingCalculator.Models;

namespace TerrariaCraftingCalculator
{
    /// <summary>Data to be used during design time in order to preview UI in Visual Studio.</summary>
    class DesignData
    {
        // CONSTANTS
        /// <summary>Backing field for the <see cref="ItemSuggestions"/> property.</summary>
        private static readonly IEnumerable<string> _itemSuggestions = new[] { "Ironskin Potion", "Lava Waders", "Megashark" };
        /// <summary>Backing field for the <see cref="ItemsToCraft"/> property.</summary>
        private static readonly IEnumerable<QuantifiedRecipeEntry> _itemsToCraft = new[]
        {
            new QuantifiedRecipeEntry
            {
                Quantity = 3,
                Recipe = new RecipeEntry
                {
                    ResultingItem = new QuantifiedItemEntry
                    {
                        Item = new ItemEntry
                        {
                            Name = "Ironskin Potion",
                        },
                        Quantity = 1,
                    },
                    Ingredients = new[]
                    {
                        new QuantifiedItemEntry
                        {
                            Quantity = 1,
                            Item = new ItemEntry
                            {
                                Name = "Bottled Water",
                            },
                        },
                        new QuantifiedItemEntry
                        {
                            Quantity = 1,
                            Item = new ItemEntry
                            {
                                Name = "Daybloom",
                            },
                        },
                        new QuantifiedItemEntry
                        {
                            Quantity = 1,
                            Item = new ItemEntry
                            {
                                Name = "Iron Ore",
                            },
                        },
                    },
                    CraftingStations = new[]
                    {
                        new ItemEntry
                        {
                            Name = "Placed Bottle",
                        },
                        new ItemEntry
                        {
                            Name = "Alchemy Table",
                        },
                    }
                },
            },
            new QuantifiedRecipeEntry
            {
                Quantity = 1,
                Recipe = new RecipeEntry
                {
                    ResultingItem = new QuantifiedItemEntry
                    {
                        Quantity = 1,
                        Item = new ItemEntry
                        {
                            Name = "Megashark",
                        },
                    },
                    Ingredients = new[]
                    {
                        new QuantifiedItemEntry
                        {
                            Quantity = 1,
                            Item = new ItemEntry
                            {
                                Name = "Minishark",
                            },
                        },
                        new QuantifiedItemEntry
                        {
                            Quantity = 1,
                            Item = new ItemEntry
                            {
                                Name = "Illegal Gun Parts",
                            },
                        },
                        new QuantifiedItemEntry
                        {
                            Quantity = 5,
                            Item = new ItemEntry
                            {
                                Name = "Shark Fin",
                            },
                        },
                        new QuantifiedItemEntry
                        {
                            Quantity = 20,
                            Item = new ItemEntry
                            {
                                Name = "Soul of Might",
                            },
                        },
                    },
                    CraftingStations = new[]
                    {
                        new ItemEntry
                        {
                            Name = "Mythril Anvil",
                        },
                        new ItemEntry
                        {
                            Name = "Orichalcum Anvil",
                        },
                    }
                },
            },
            new QuantifiedRecipeEntry
            {
                Quantity = 2,
                Recipe = new RecipeEntry
                {
                    ResultingItem = new QuantifiedItemEntry
                    {
                        Quantity = 1,
                        Item = new ItemEntry
                        {
                            Name = "Molten Charm",
                        },
                    },
                    Ingredients = new QuantifiedItemEntry[]
                    {
                        new QuantifiedItemEntry
                        {
                            Quantity = 1,
                            Item = new ItemEntry
                            {
                                Name = "Lava Charm",
                            },
                        },
                        new QuantifiedItemEntry
                        {
                            Quantity = 1,
                            Item = new ItemEntry
                            {
                                Name = "Obsidian Skull",
                            },
                        },
                    },
                    CraftingStations = new[]
                    {
                        new ItemEntry
                        {
                            Name = "Tinkerer's Workshop",
                        },
                    },
                },
            },
            new QuantifiedRecipeEntry
            {
                Quantity = 3,
                Recipe = new RecipeEntry
                {
                    ResultingItem = new QuantifiedItemEntry
                    {
                        Quantity = 30,
                        Item = new ItemEntry
                        {
                            Name = "Silk Rope",
                        },
                    },
                    Ingredients = new QuantifiedItemEntry[]
                    {
                        new QuantifiedItemEntry
                        {
                            Quantity = 1,
                            Item = new ItemEntry
                            {
                                Name = "Silk",
                            },
                        },
                    },
                    CraftingStations = new[]
                    {
                        new ItemEntry
                        {
                            Name = "Loom",
                        },
                    }
                },
            },
        };





        // STATIC PROPERTIES
        /// <summary>Item suggestions for the search bar.</summary>
        public static IEnumerable<string> ItemSuggestions => _itemSuggestions;
        /// <summary>A mock list of items to be rendered in the UI design preview.</summary>
        public static IEnumerable<QuantifiedRecipeEntry> ItemsToCraft => _itemsToCraft;
    }
}
