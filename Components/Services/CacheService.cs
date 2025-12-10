using System.Text.Json;
using currency_converter_cs.Components.Models;

namespace currency_converter_cs.Components.Clients;

public class CacheService
{
    private readonly string _cacheFolder;

    public CacheService(IWebHostEnvironment env)
    {
        _cacheFolder = Path.Combine(env.WebRootPath, "cache");
        Directory.CreateDirectory(_cacheFolder);
    }


    public async Task<RatesResponse?> RetrieveFromCache(Rate rate)
    {
        // Create the formatted file name and combine with our cacheFolder location name
        var cacheFileName = $"{rate.currencyCode}-{rate.date}.json";
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


    public async Task WriteToCache(RatesResponse rates)
    {
        var cacheFileName = $"{rates.Rates.Keys.First()}-{rates.Date}.json";
        var cachePath = Path.Combine(_cacheFolder, cacheFileName);

        try
        {
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
