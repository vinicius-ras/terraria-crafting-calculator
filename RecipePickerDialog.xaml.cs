using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TerrariaCraftingCalculator.Models;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace TerrariaCraftingCalculator
{
    public sealed partial class RecipePickerDialog : ContentDialog
    {
        // INSTANCE PROPERTIES
        /// <summary>The list of recipes for the user to pick one.</summary>
        public ObservableCollection<RecipeEntry> RecipesToDisplay { get; set; } = new ObservableCollection<RecipeEntry>();
        /// <summary>The recipe the user has selected.</summary>
        public RecipeEntry SelectedRecipe { get; set; }




        // INSTANCE METHODS
        /// <summary>Constructor.</summary>
        /// <param name="recipesToDisplay">The list of recipes for the user to pick one.</param>
        public RecipePickerDialog(IEnumerable<RecipeEntry> recipesToDisplay)
        {
            this.InitializeComponent();
            this.Title = $"Pick a recipe for: {recipesToDisplay.First().ResultingItem.Item.Name}";
            foreach (var recipe in recipesToDisplay)
                RecipesToDisplay.Add(recipe);
        }


        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            SelectedRecipe = (RecipeEntry) _recipesCombo.SelectedValue;
        }


        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            SelectedRecipe = null;
        }
    }
}
