using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using TerrariaCraftingCalculator.Models;
using Windows.UI.Xaml.Controls;


namespace TerrariaCraftingCalculator
{
    /// <summary>The application's main page.</summary>
    public sealed partial class MainPage : Page
    {
        // CONSTANTS
        /// <summary>The amount of time to wait before sending an HTTP request to search for items via the Terraria Fandom API.</summary>
        private readonly TimeSpan TimeToWaitBeforeHttpRequest = TimeSpan.FromMilliseconds(500);





        // INSTANCE FIELDS
        /// <summary>A cancellation token source, used to postpone the HTTP request until the user stops typing an item's name to search.</summary>
        private CancellationTokenSource _cancellationTokenSource = null;
        /// <summary>The HTTP client used to comunicate with the back end server which contains crafting information about items.</summary>
        private static readonly HttpClient _httpClient = new HttpClient();





        // INSTANCE METHODS
        /// <summary>Constructor.</summary>
        public MainPage()
        {
            this.InitializeComponent();
        }


        /// <summary>Called when the user types something in the search box.</summary>
        /// <param name="sender">Reference to the search box where the user has typed something.</param>
        /// <param name="args">Event information.</param>
        private async void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // Wait for some time before actually performing the HTTP request to the server, to allow the user to finish typing.
            // If the user keeps typing during that time, previous executions of this method will get cancelled in favor of new ones.
            if (_cancellationTokenSource != null)
                _cancellationTokenSource.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            var opGuid = Guid.NewGuid();
            try
            {
                await Task.Delay(TimeToWaitBeforeHttpRequest, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

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
            var response = await _httpClient.GetAsync(targetUri);

            // Parse response looking for name items in the search result
            using (var responseStream = await response.Content.ReadAsStreamAsync())
            using (var jsonDoc = await JsonDocument.ParseAsync(responseStream))
            {
                IEnumerable<string> suggestionsArr;
                try
                {
                    suggestionsArr = jsonDoc?.RootElement
                        .GetProperty("suggestions")
                        .EnumerateArray()
                        .Select(element => element.GetString());
                    sender.ItemsSource = suggestionsArr.ToArray();
                }
                catch (Exception ex) when (ex is KeyNotFoundException || ex is InvalidOperationException)
                {
                    Debug.Fail("Failed to retrieve page suggestions form HTTP request.");
                }
            }
        }


        /// <summary>Called when the user picks one of the available options in the search box.</summary>
        /// <param name="sender">Reference to the search box where the user picked an entry.</param>
        /// <param name="args">Event information.</param>
        private async void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
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
                var recipeRows = parsedPage.QuerySelectorAll("div.crafts tr[data-rowid]");
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
                            ImageUrl = resultFirstImgElement.Source,
                        },
                        Quantity = resultQuantity,
                    };


                    // Get information about the Crafting Station(s)
                    var craftingStationImgElements = rowCraftingStationTd.QuerySelectorAll<IHtmlImageElement>("img:not([title])");
                    var craftingStations = craftingStationImgElements.Select(img => new ItemEntry
                    {
                        Name = img.AlternativeText,
                        ImageUrl = img.Source,
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
                            ImageUrl = ingredientImg.Source,
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
            }
        }
    }
}
