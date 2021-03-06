using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using TerrariaCraftingCalculator.Extensions;
using TerrariaCraftingCalculator.Models;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace TerrariaCraftingCalculator
{
    /// <summary>The application's main page.</summary>
    public sealed partial class MainPage : Page
    {
        // CONSTANTS
        /// <summary>The minimum quantity a user can define for a recipe, in the list of recipes to craft.</summary>
        private const int MinimumRecipeQuantity = 1;
        /// <summary>The amount of time to wait before sending an HTTP request to search for items via the Terraria Fandom API.</summary>
        private static readonly TimeSpan TimeToWaitBeforeHttpRequest = TimeSpan.FromMilliseconds(500);
        /// <summary>Solid color brush containing the user system's "foreground error color".</summary>
        private static readonly SolidColorBrush TextControlErrorForegroundColorBrush = new SolidColorBrush((Color)Application.Current.Resources["SystemErrorTextColor"]);





        // INSTANCE FIELDS
        /// <summary>A cancellation token source, used to postpone the HTTP request until the user stops typing an item's name to search.</summary>
        private CancellationTokenSource _cancellationTokenSource = null;
        /// <summary>The HTTP client used to comunicate with the back end server which contains crafting information about items.</summary>
        private static readonly HttpClient _httpClient = new HttpClient();





        // INSTANCE PROPERTIES
        /// <summary>List of suggestions to be displayed while the user is typing in the "search box" of available items.</summary>
        public ObservableCollection<string> SearchSuggestions { get; set; } = new ObservableCollection<string>();
        /// <summary>Holds the list of recipes added for crafting.</summary>
        public ObservableCollection<QuantifiedRecipeEntry> RecipesList { get; set; } = new ObservableCollection<QuantifiedRecipeEntry>();
        /// <summary>The calculated list of total ingredients that should be gathered for crafting.</summary>
        public ObservableCollection<QuantifiedItemEntry> TotalIngredientsList { get; set; } = new ObservableCollection<QuantifiedItemEntry>();





        // INSTANCE METHODS
        /// <summary>Constructor.</summary>
        public MainPage()
        {
            this.InitializeComponent();
            RecipesList.CollectionChanged += (sender, evtArgs) => RefreshTotalIngredientsList();
        }


        /// <summary>Called when the user types something in the search box.</summary>
        /// <param name="sender">Reference to the search box where the user has typed something.</param>
        /// <param name="args">Event information.</param>
        private async void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // Clear previous search suggestions, and cancel previous ongoing searches (if any)
            SearchSuggestions.Clear();

            if (_cancellationTokenSource != null)
                _cancellationTokenSource.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = _cancellationTokenSource.Token;
            try
            {
                // Wait for some time before actually performing the HTTP request to the server, to allow the user to finish typing.
                // If the user keeps typing during that time, previous executions of this method will get cancelled in favor of new ones.
                await Task.Delay(TimeToWaitBeforeHttpRequest, cancellationToken);

                // Perform an HTTP request to search for the item whose name the user has typed
                var queryStringParams = new Dictionary<string, string>()
                {
                    { "controller", "UnifiedSearchSuggestionsController" },
                    { "method", "getSuggestions" },
                    { "format", "json" },
                    { "query", HttpUtility.UrlEncode(sender.Text) },
                };
                var queryStringParamsKeyValue = queryStringParams.Select(keyVal => $"{keyVal.Key}={keyVal.Value}");
                var targetUri = $"https://terraria.fandom.com/wikia.php?{string.Join("&", queryStringParamsKeyValue)}";
                var response = await _httpClient.GetAsync(targetUri, cancellationToken);

                // Parse response looking for name items in the search result
                using (var responseStream = await response.Content.ReadAsStreamAsync())
                using (var jsonDoc = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken))
                {
                    IEnumerable<string> suggestionsArr;
                    try
                    {
                        suggestionsArr = jsonDoc?.RootElement
                            .GetProperty("suggestions")
                            .EnumerateArray()
                            .Select(element => element.GetString());
                        foreach (var suggestion in suggestionsArr)
                            SearchSuggestions.Add(suggestion);
                    }
                    catch (Exception ex) when (ex is KeyNotFoundException || ex is InvalidOperationException)
                    {
                        Debug.Fail("Failed to retrieve page suggestions form HTTP request.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }


        /// <summary>Called when the user picks one of the available options in the search box.</summary>
        /// <param name="sender">Reference to the search box where the user picked an entry.</param>
        /// <param name="args">Event information.</param>
        private async void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            try
            {
                // Disable control while processing
                sender.IsEnabled = false;

                // Generate a URL and call the API to retrieve crafting data
                string selectedItemStr = (string)args.SelectedItem,
                    wikiPageName = selectedItemStr.Replace(" ", "_"),
                    wikiPageUrl = $"https://terraria.fandom.com/wiki/{wikiPageName}";

                var response = await _httpClient.GetAsync(wikiPageUrl);

                // Parse HTTP response
                var quantityParser = new Regex(@"\((\d+)\)");
                using (var responseStream = await response.Content.ReadAsStreamAsync())
                {
                    var parser = new HtmlParser();
                    var parsedPage = await parser.ParseDocumentAsync(responseStream);
                    var recipeRows = parsedPage.QuerySelectorAll(".crafts tr[data-rowid]");
                    var foundRows = recipeRows.Select(r => r.OuterHtml).ToList();
                    IElement lastResultTd = null,
                        lastIngredientsTd = null,
                        lastCraftingStationTd = null;
                    var recipesResults = new List<RecipeEntry>();
                    foreach (var row in recipeRows)
                    {
                        // Find the "result , "ingredients" and "station" related to the current recipe
                        var rowResultTd = row.QuerySelector("td.result") ?? lastResultTd;
                        var rowIngredientsTd = row.QuerySelector("td.ingredients") ?? lastIngredientsTd;
                        var rowCraftingStationTd = row.QuerySelector("td.station") ?? lastCraftingStationTd;

                        lastResultTd = rowResultTd;
                        lastIngredientsTd = rowIngredientsTd;
                        lastCraftingStationTd = rowCraftingStationTd;


                        // Get information about the resulting item
                        var resultFirstImgElement = rowResultTd.QuerySelector<IHtmlImageElement>("img");
                        var resultQuantityStr = rowResultTd.QuerySelector(".note-text")?.TextContent ?? "(1)";
                        resultQuantityStr = quantityParser.Match(resultQuantityStr).Groups[1].Value;
                        int resultQuantity = int.Parse(resultQuantityStr);
                        var resultingItem = new QuantifiedItemEntry
                        {
                            Item = new ItemEntry
                            {
                                Name = resultFirstImgElement.AlternativeText,
                                ImageUrl = resultFirstImgElement.Dataset["src"],
                            },
                            Quantity = resultQuantity,
                        };


                        // Get information about the Crafting Station(s)
                        var craftingStationImgElements = rowCraftingStationTd.QuerySelectorAll<IHtmlImageElement>("img:not([title])");
                        var craftingStations = craftingStationImgElements.Select(img => new ItemEntry
                        {
                            Name = img.AlternativeText,
                            ImageUrl = img.Dataset["src"],
                        });
                        if (craftingStations.Any() == false)
                            craftingStations = new[]
                            {
                                new ItemEntry
                                {
                                    Name = "By Hand",
                                    ImageUrl = null,
                                }
                            };

                        // Get information about the Ingredient(s)
                        var recipeIngredients = new List<QuantifiedItemEntry>();
                        foreach (var ingredientLi in rowIngredientsTd.QuerySelectorAll<IHtmlListItemElement>("li"))
                        {
                            var ingredientImg = ingredientLi.QuerySelector<IHtmlImageElement>("img");
                            var ingredientEntry = new ItemEntry
                            {
                                Name = ingredientImg.AlternativeText,
                                ImageUrl = ingredientImg.Dataset["src"],
                            };

                            string ingredientQuantityStr = ingredientLi.QuerySelector(".note-text")?.TextContent ?? "(1)";
                            ingredientQuantityStr = quantityParser.Match(ingredientQuantityStr).Groups[1].Value;
                            int ingredientQuantity = int.Parse(ingredientQuantityStr);
                            recipeIngredients.Add(new QuantifiedItemEntry
                            {
                                Item = ingredientEntry,
                                Quantity = ingredientQuantity,
                            });
                        }


                        // Add the generated recipe to the results
                        recipesResults.Add(new RecipeEntry
                        {
                            ResultingItem = resultingItem,
                            CraftingStations = craftingStations.ToArray(),
                            Ingredients = recipeIngredients,
                        });
                    }

                    // Find results by matching typed search
                    var matchingRecipes = recipesResults
                        .Where(recipe => recipe.ResultingItem.Item.Name.Equals(selectedItemStr, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    int totalMatchingRecipes = matchingRecipes.Count;
                    QuantifiedRecipeEntry chosenRecipe = new QuantifiedRecipeEntry
                    {
                        Quantity = 1,
                        Recipe = null,
                    };
                    if (totalMatchingRecipes == 1)
                    {
                        // If there was a single match, add it to the recipes list right away
                        chosenRecipe.Recipe = matchingRecipes.First();
                    }
                    else if (totalMatchingRecipes > 1)
                    {
                        // For multiple matches (items with more than one recipe), display a "picker" dialog, so that the user
                        // can choose between one of the available recipes
                        var recipePickerDialog = new RecipePickerDialog(matchingRecipes);
                        var dialogResult = await recipePickerDialog.ShowAsync();
                        if (dialogResult == ContentDialogResult.Primary)
                            chosenRecipe.Recipe = recipePickerDialog.SelectedRecipe;
                    }
                    else
                    {
                        _searchBarFlyoutText.Text = $@"Could not find any crafting recipes for item ""{selectedItemStr}""!";
                        FlyoutBase.ShowAttachedFlyout(_searchBar);
                    }

                    // If a recipe has been picked, add it to our list of current recipes
                    if (chosenRecipe.Recipe != null)
                    {
                        RecipesList.Add(chosenRecipe);
                        sender.Text = string.Empty;
                    }
                }
            }
            finally
            {
                // Re-enable search box after conclusion of processing, and try to focus it
                sender.IsEnabled = true;
                sender.Focus(FocusState.Keyboard);
            }
        }


        /// <summary>Call this to recalculate and refresh the "total ingredients" list.</summary>
        /// <remarks>This method will be automatically called when the <see cref="RecipesList"/> observes a change to its contents.</remarks>
        private void RefreshTotalIngredientsList()
        {
            var ingredientTotals = RecipesList.SelectMany(recipe => recipe.Recipe.Ingredients.Select(ingredient => new QuantifiedItemEntry
                {
                    Item = ingredient.Item,
                    Quantity = ingredient.Quantity * recipe.Quantity,
                }))
                .GroupBy(ingredient => ingredient.Item.Name)
                .Select(ingredientGroup => new {
                    ingredientName = ingredientGroup.Key,
                    totalRequired = ingredientGroup.Sum(ingredient => ingredient.Quantity),
                    imageUrl = ingredientGroup.First().Item.ImageUrl,
                });
            TotalIngredientsList.Clear();
            foreach (var ingredientSummary in ingredientTotals)
                TotalIngredientsList.Add(new QuantifiedItemEntry
                {
                    Quantity = ingredientSummary.totalRequired,
                    Item = new ItemEntry
                    {
                        Name = ingredientSummary.ingredientName,
                        ImageUrl = ingredientSummary.imageUrl,
                    },
                });
        }


        /// <summary>Called when the quantity of a recipe changed.</summary>
        /// <param name="sender">Reference to the <see cref="TextBox"/> where the user picked an entry.</param>
        /// <param name="args">Event information.</param>
        private void RecipeQuantityChanged(object sender, TextChangedEventArgs args)
        {
            // Find the "decrease" button, and enable/disable it as necessary
            var textBox = (TextBox)sender;

            var closestPanel = VisualTreeHelperUtilities.FindClosestParentOfType<Panel>(textBox);
            var removeSymbol = VisualTreeHelperUtilities.GetAllChildren(closestPanel)
                .OfType<SymbolIcon>()
                .Where(symbolIcon => symbolIcon.Symbol == Symbol.Remove)
                .Single();
            var decreaseQuantityButton = VisualTreeHelperUtilities.FindClosestParentOfType<Button>(removeSymbol);

            // Try to parse the new value to see if it is a valid number
            bool canParseNumber = int.TryParse(textBox.Text, out int typedNumber);
            if (canParseNumber == false || typedNumber < MinimumRecipeQuantity)
            {
                decreaseQuantityButton.IsEnabled = false;

                // Do not accept anything but numbers
                string newText = Regex.Replace(textBox.Text, @"\D", "");
                if (string.IsNullOrWhiteSpace(newText))
                    newText = MinimumRecipeQuantity.ToString();
                else
                {
                    typedNumber = int.Parse(newText);
                    if (typedNumber < MinimumRecipeQuantity)
                        newText = MinimumRecipeQuantity.ToString();
                }
                textBox.Text = newText;
                return;
            }
            else
                decreaseQuantityButton.IsEnabled = (typedNumber > MinimumRecipeQuantity);
            RefreshTotalIngredientsList();
        }


        /// <summary>Called to update the quantity of a given recipe.</summary>
        /// <remarks>This method also enables/disables the button which decreases the quantity of items to craft, according to the new amount.</remarks>
        /// <param name="quantityTextBox">The <see cref="TextBox"/> component where the current quantity is located.</param>
        /// <param name="increaseBy">The amount to add/subtract from the current quantity.</param>
        private static void UpdateRecipeQuantity(TextBox quantityTextBox, int increaseBy)
        {
            // Increase the quantity
            if (int.TryParse(quantityTextBox.Text, out int currentQuantity) == false)
                return;
            quantityTextBox.Text = (currentQuantity + increaseBy).ToString();
        }


        /// <summary>Called when the "plus" button is clicked in the list of items to craft, in order to increase the crafting amount of an item.</summary>
        /// <param name="sender">Reference to the <see cref="Button"/> clicked by the user.</param>
        /// <param name="args">Event information.</param>
        private void ButtonIncreaseItemQuantityInCraftingList(object sender, RoutedEventArgs e)
        {
            // Use the visual tree to find the "quantity" text box related to the item in the list
            var buttonRef = (Button)sender;
            var closestPanel = VisualTreeHelperUtilities.FindClosestParentOfType<Panel>(buttonRef);
            var quantityTextBox = VisualTreeHelperUtilities.GetAllChildren(closestPanel)
                .OfType<TextBox>()
                .Single();
            UpdateRecipeQuantity(quantityTextBox, 1);
        }


        /// <summary>Called when the "minus" button is clicked in the list of items to craft, in order to decrease the crafting amount of an item.</summary>
        /// <param name="sender">Reference to the <see cref="Button"/> clicked by the user.</param>
        /// <param name="args">Event information.</param>
        private void ButtonDecreaseItemQuantityInCraftingList(object sender, RoutedEventArgs e)
        {
            // Use the visual tree to find the "quantity" text box related to the item in the list
            var buttonRef = (Button)sender;
            var closestPanel = VisualTreeHelperUtilities.FindClosestParentOfType<Panel>(buttonRef);
            var quantityTextBox = VisualTreeHelperUtilities.GetAllChildren(closestPanel)
                .OfType<TextBox>()
                .Single();

            UpdateRecipeQuantity(quantityTextBox, -1);
        }


        /// <summary>Called when the "trash" button is clicked in the list of items to craft, in order to remove an item from the crafting list.</summary>
        /// <param name="sender">Reference to the <see cref="Button"/> clicked by the user.</param>
        /// <param name="args">Event information.</param>
        private void ButtonRemoveItemFromCraftingList(object sender, RoutedEventArgs e)
        {
            var buttonRef = (Button)sender;
            var associatedQuantifiedRecipeEntry = (QuantifiedRecipeEntry)buttonRef.Tag;
            RecipesList.Remove(associatedQuantifiedRecipeEntry);
        }
    }
}
