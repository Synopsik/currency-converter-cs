using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using currency_converter_cs.Components.Models;
using Microsoft.AspNetCore.Hosting;

namespace currency_converter_cs.Components.Clients;

public class ExchangeRateService
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly string _cacheFolder;

    private static readonly Regex DateRegex = new Regex(@"^\d{4}\.\d{2}\.\d{2}$", RegexOptions.Compiled);

    public ExchangeRateService(IHttpClientFactory clientFactory, IWebHostEnvironment env)
    {
        _clientFactory = clientFactory;
        _cacheFolder = Path.Combine(env.WebRootPath, "cache");
        Directory.CreateDirectory(_cacheFolder);
    }

    public async Task<RatesResponse?> GetRatesAsync(
        string currencyCode = "usd",
        string date = "latest")
    {
        // Ensure all codes are lower case
        currencyCode = currencyCode.ToLowerInvariant();

        string normalizedDate = date;

        // If date != "latest", try to normalize the given date to the required form yyyy.MM.dd
        if (!string.Equals(date, "latest", StringComparison.OrdinalIgnoreCase))
        {
            if (!DateRegex.IsMatch(date))
            {
                Console.WriteLine($"[WARN] Normalizing date: {date}");
                if (DateTime.TryParse(date, out var parsed))
                {
                    if (parsed > DateTime.UtcNow)
                        Console.WriteLine("[WARN] Can't search dates in the future");

                    normalizedDate = parsed.ToString("yyyy.M.d");
                }
                else
                {
                    Console.WriteLine($"[ERROR] Could not parse '{date}', aborting fetch...");
                    return null;
                }
            }
        }
        else
        {
            normalizedDate = DateTime.UtcNow.ToString("yyyy.M.d");
        }

        string cacheFileName = $"{currencyCode}-{normalizedDate}.json";
        string cachePath = Path.Combine(_cacheFolder, cacheFileName);

        // If our cache file is found
        if (File.Exists(cachePath))
        {
            try
            {
                Console.WriteLine($"[CACHE] Loading: {cacheFileName}");
                // Begin loading from JSON and return the result
                string json = await File.ReadAllTextAsync(cachePath);
                return JsonSerializer.Deserialize<RatesResponse>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CACHE ERROR] {ex.Message}");
            }
        }

        // If no cache file is found, query API for conversions
        try
        {
            var client = _clientFactory.CreateClient("ExchangeApi");
            string apiPath = $"currency-api@{normalizedDate}/v1/currencies/{currencyCode}.json";

            Console.WriteLine($"[API] GET {apiPath}");
            var result = await client.GetFromJsonAsync<RatesResponse>(apiPath);

            if (result != null)
            {
                try
                {
                    string json = JsonSerializer.Serialize(
                        result,
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

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FETCH ERROR] {ex.Message}");
            Console.WriteLine($"[WARN] Use dates after 2024!");
            return null;
        }
    }
}