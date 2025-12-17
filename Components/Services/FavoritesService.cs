// Steven Baar
// 12/18/26
// Currency Converter
// Service for interacting with the public Favorites file

using System.Text.Json;
using currency_converter_cs.Components.Models;

namespace currency_converter_cs.Components.Clients;

public class FavoritesService
{
    private ExchangeRateService _exchangeRateService;
    private const string FavoritesPath = "wwwroot/favorites.json";

    public FavoritesService(ExchangeRateService exchangeRateService)
    {
        _exchangeRateService = exchangeRateService;
    }


    /// <summary>
    /// If our file exists, first ensure we aren't adding a duplicate.
    /// Then add the favorite pair to our favorites and save it back to disk.
    /// </summary>
    /// <param name="from">From Currency</param>
    /// <param name="to">To Currency</param>
    public async Task AddFavorite(string from, string to)
    {
        // Construct a path for our favorites.json file in the public web root
        var path = Path.Combine("wwwroot", "favorites.json");
        var favorites = new List<FavoritePair>();

        // If our file exists, read the JSON and deserialize it into a list of Favorite Pairs
        if (File.Exists(path))
        {
            var json = await File.ReadAllTextAsync(path);
            favorites = JsonSerializer.Deserialize<List<FavoritePair>>(json) ?? new();
        }

        // Ensure that the current favorite from & to doesn't match any existing entries
        if (!favorites.Any(f => f.BaseCurrency == from && f.TargetCurrency == to))
        {
            // Add our new currency pair to the entries
            favorites.Add(new FavoritePair { BaseCurrency = from, TargetCurrency = to });

            // Serialize our data back to JSON and write the file
            var newJson = JsonSerializer.Serialize(favorites, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, newJson);
        }
    }

    /// <summary>
    /// Regardless of ordinal case, search for our item to remove.
    /// Once found, remove the item, serialize the favorites back to JSON, and write the file to disk.
    /// </summary>
    /// <param name="baseCurrency">Base currency (USD)</param>
    /// <param name="targetCurrency">Target Currency (EUR)</param>
    public async Task RemoveFavorite(string baseCurrency, string targetCurrency)
    {
        var favorites = await ReadFavoritesFromFile();

        // Gather the first AddFavorite where BaseCurrency and TargetCurrency match the provided currencies.
        // OrdinalIgnoreCase ignores strings that mismatch cases, allowing upper and lower cases to be compared equally
        var itemToRemove = favorites.FirstOrDefault(f =>
            f.BaseCurrency.Equals(baseCurrency, StringComparison.OrdinalIgnoreCase) &&
            f.TargetCurrency.Equals(targetCurrency, StringComparison.OrdinalIgnoreCase));

        if (itemToRemove != null)
        {
            // Remove the specific pair
            favorites.Remove(itemToRemove);

            // Save the updated list back to disk
            var json = JsonSerializer.Serialize(favorites, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(FavoritesPath, json);

            // Reload the table to update changes
            await LoadFavoritesData();
        }
    }

    /// <summary>
    /// Gather all of our favorite pairs and group them to efficiently call our ExchangeRateServer for the favorite's data
    /// </summary>
    /// <returns>Live favorite conversion rates</returns>
    public async Task<List<ConversionRow>> LoadFavoritesData()
    {
        List<ConversionRow> favoriteRows = new();

        // Load the raw favorites list from JSON
        var favorites = await ReadFavoritesFromFile();

        // If no favorites are found, stop the loading process
        if (favorites.Count == 0)
        {
            return favoriteRows;
        }

        // Group by Base Currency to minimize API/Service calls
        // So, if we have USD->EUR and USD->GBP, we only fetch USD rates once.
        var groupedFavorites = favorites
            .GroupBy(f => f.BaseCurrency.ToLowerInvariant())
            .ToList();

        // Iterate through the unique base currencies and fetch current rates
        foreach (var group in groupedFavorites)
        {
            var baseCurrency = group.Key;

            // Call the service for either live or locally cached rates data (API updates data daily)
            var data = await _exchangeRateService.GetRatesAsync(baseCurrency);

            // Get our dictionary of names and rates using the baseCurrency for each group of favorites
            if (data != null && data.Rates.TryGetValue(baseCurrency, out var rateElement))
            {
                // Deserialize the inner dictionary of rates "usd": { "eur": 0.85, ... }
                var ratesDict = JsonSerializer.Deserialize<Dictionary<string, decimal>>(rateElement);

                // Ensure data was found
                if (ratesDict != null)
                {
                    // For every favorite in this group, find the specific target rate
                    foreach (var fav in group)
                    {
                        // Format currency string to lowercase
                        var target = fav.TargetCurrency.ToLowerInvariant();

                        // Ensure a decimal rate value was found
                        if (ratesDict.TryGetValue(target, out var rateValue))
                        {
                            // Create a new ConversionRow and Add it to our _favoriteRows
                            favoriteRows.Add(new ConversionRow
                            {
                                From = baseCurrency,
                                To = target,
                                Rate = rateValue,
                                Date = data.Date
                            });
                        }
                        else
                        {
                            // Just in case the API doesn't return the target currency
                            favoriteRows.Add(new ConversionRow
                            {
                                From = baseCurrency,
                                To = target,
                                Rate = 0,
                                Date = "N/A"
                            });
                        }
                    }
                }
            }
        }
        //  the flag to false so the page can switch from the default loading screen to displaying the results
        return favoriteRows;
    }


    /// <summary>
    /// Attempt to read Favorites from file, default to an empty Favorites list
    /// </summary>
    /// <returns>List of locally saved Favorites</returns>
    private async Task<List<FavoritePair>> ReadFavoritesFromFile()
    {
        // Returns a new empty list of favorite pairs if our favorites.json does NOT exist
        if (!File.Exists(FavoritesPath))
        {
            return new List<FavoritePair>();
        }

        // If our favorites.json does exist, return the deserialized list of favorite pairs
        try
        {
            var json = await File.ReadAllTextAsync(FavoritesPath);
            return JsonSerializer.Deserialize<List<FavoritePair>>(json) ?? new List<FavoritePair>();
        }
        // Worst case scenario, return a new empty list of favorite pairs
        catch
        {
            return new List<FavoritePair>();
        }
    }
}