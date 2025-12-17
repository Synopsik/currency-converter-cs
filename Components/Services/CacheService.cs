// Steven Baar
// 12/18/26
// Currency Converter
// Service for interacting with our local cache file

using System.Text.Json;
using currency_converter_cs.Components.Models;
using currency_converter_cs.Components.Utils;

namespace currency_converter_cs.Components.Clients;

public class CacheService
{
    private readonly string _cacheFolder;

    public CacheService(IWebHostEnvironment env)
    {
        _cacheFolder = Path.Combine(env.WebRootPath, "cache");
        Directory.CreateDirectory(_cacheFolder);
    }


    /// <summary>
    /// Attempt to retrieve the list of rates from our local cache
    /// </summary>
    /// <param name="rate">Rate type</param>
    /// <returns>RatesResponse of conversion rates</returns>
    public async Task<RatesResponse?> RetrieveFromCache(Rate rate)
    {
        // Create the formatted file name and combine with our cacheFolder location name
        var normalizedDate = Formatting.NormalizeDate(rate.Date);
        var cacheFileName = $"{rate.CurrencyCode}-{normalizedDate}.json";
        var cachePath = Path.Combine(_cacheFolder, cacheFileName);
        // If our cache file is not found
        if (!File.Exists(cachePath))
        {
            return null;
        }
        try
        {
            Console.WriteLine($"[CACHE] Loading: {cacheFileName}");
            // Begin loading from JSON and return the result
            var json = await File.ReadAllTextAsync(cachePath);
            return JsonSerializer.Deserialize<RatesResponse?>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CACHE ERROR] {ex.Message}");
            return null;
        }
    }


    /// <summary>
    /// Try and write a RatesResponse to our local cache
    /// </summary>
    /// <param name="rates">RatesResponse from API</param>
    public async Task WriteToCache(RatesResponse rates)
    {
        // Get the currency name from the first key in our Rates field, then get the date string from the Date field
        var normalizedDate = Formatting.NormalizeDate(rates.Date);
        var cacheFileName = $"{rates.Rates.Keys.First()}-{normalizedDate}.json";
        // Combine with the folder path to create our custom currency and date path
        var cachePath = Path.Combine(_cacheFolder, cacheFileName);

        try
        {
            // Attempt to serialize and save our RatesResponse to file
            var json = JsonSerializer.Serialize(
                rates,
                new JsonSerializerOptions { WriteIndented = true }
            );
            await File.WriteAllTextAsync(cachePath, json);
            Console.WriteLine($"[CACHE] Saved: {cacheFileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CACHE WRITE ERROR] {ex.Message}");
        }
    }

}
