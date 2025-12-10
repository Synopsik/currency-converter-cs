using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using currency_converter_cs.Components.Models;
using currency_converter_cs.Components.Utils;
using Microsoft.AspNetCore.Hosting;

namespace currency_converter_cs.Components.Clients;

public class ExchangeRateService
{
    private CacheService _cacheService;
    private readonly IHttpClientFactory _clientFactory;

    private static readonly Regex DateRegex = new Regex(@"^\d{4}\.\d{2}\.\d{2}$", RegexOptions.Compiled);

    public ExchangeRateService(IHttpClientFactory clientFactory, CacheService cacheService)
    {
        _clientFactory = clientFactory;
        _cacheService = cacheService;
    }

    public async Task<RatesResponse?> GetRatesAsync(
        string currencyCode = "usd",
        string date = "latest")
    {
        var queryRate = new Rate
        {
            // Ensure the currencyCode parameter is lower case
            currencyCode = currencyCode.ToLowerInvariant(),
            // Normalize any date given (ex. 1/1/25, 01-02-24, etc.) to yyyy.M.d
            date = Formatting.NormalizeDate(date)
        };

        // First check for a cached rates response
        var cacheResponse = await _cacheService.RetrieveFromCache(queryRate);

        // If a cached response is found, return immediately
        if (cacheResponse != null) { return cacheResponse; }

        // If no cache file is found, query API for rates
        var queryResponse = await QueryApi(queryRate);

        // If a query response is found, return immediately
        if (queryResponse != null) { return queryResponse; }

        // If everything fails, return null
        return null;
    }

    private async Task<RatesResponse?> QueryApi(Rate rate)
    {
        var client = _clientFactory.CreateClient("ExchangeApi");

        var startDate = Formatting.ToDateOnly(rate.date);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        async Task<RatesResponse?> Fetch(DateOnly d)
        {
            var dateString = Formatting.ToApiDate(d);
            var path = $"currency-api@{dateString}/v1/currencies/{rate.currencyCode}.json";

            Console.WriteLine($"[API] GET {path}");
            using var response = await client.GetAsync(path);

            if (response.StatusCode == HttpStatusCode.NotFound) return null;

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<RatesResponse?>();

            if (result != null) await _cacheService.WriteToCache(result);

            return result;
        }

        // Try the requested date
        var initial = await Fetch(startDate);
        if (initial != null) return initial;

        Console.WriteLine($"[WARN] No data for {rate.date}, seraching nearby dates...");

        // Begin search dates forward
        for (var d = startDate.AddDays(1); d <= today; d = d.AddDays(1))
        {
            var result = await Fetch(d);
            if (result != null)
            {
                Console.WriteLine($"[INFO] Found data at {Formatting.ToApiDate(d)}");
                return result;
            }
        }

        // If nothing was found, begin searching dates backwards to a limit of 1 year
        var lowerBound = startDate.AddDays(-365);
        for (var d = startDate.AddDays(-1); d >= lowerBound; d = d.AddDays(-1))
        {
            var result = await Fetch(d);
            if (result != null)
            {
                Console.WriteLine($"[INFO] Found earlier data at {Formatting.ToApiDate(d)}");
                return result;
            }
        }

        Console.WriteLine($"[WARN] No data found near {rate.date}");
        return null;
    }

}