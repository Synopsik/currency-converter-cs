// Steven Baar
// 12/18/26
// Currency Converter
// Service for interaction with the API.
// First, check our cache for locally saved data.
// If nothing is found, begin querying the provided date with retries if the query fails.

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

    public ExchangeRateService(IHttpClientFactory clientFactory, CacheService cacheService)
    {
        // Create our HTTP and Cache clients for queries
        _clientFactory = clientFactory;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Main method for querying rates.
    /// Paths to first check for rates in cache, then from API
    /// </summary>
    /// <param name="currencyCode">User input currency code, "usd" used as default</param>
    /// <param name="date">Date as a string in any format, "lastest" used as default</param>
    /// <param name="token">Aynch Cancellation Token if we need to cancel and start a new request</param>
    /// <returns>Potentially return our RatesResponse from the cache or API</returns>
    public async Task<RatesResponse?> GetRatesAsync(
        string currencyCode = "usd",
        string date = "latest",
        CancellationToken token = default)
    {
        // If the request is canceled, throw and exit this method
        token.ThrowIfCancellationRequested();

        var queryRate = new Rate
        {
            // Ensure the CurrencyCode parameter is lower case
            CurrencyCode = currencyCode.ToLowerInvariant(),
            // Normalize any Date given (ex. 1/1/25, 01-02-24, etc.) to yyyy.M.d
            Date = Formatting.NormalizeDate(date)
        };

        // First check for a cached rates response
        var cacheResponse = await _cacheService.RetrieveFromCache(queryRate);

        // If a cached response is found, return immediately
        if (cacheResponse != null) { return cacheResponse; }

        // If no cache file is found, query API for rates
        var queryResponse = await QueryApi(queryRate, token);

        // If a query response is found, return immediately
        if (queryResponse != null) { return queryResponse; }

        // If everything fails, return null
        return null;
    }

    /// <summary>
    /// Query API by festing for the given date.
    /// If nothing is found, begin searching forwards in time until valid data is found.
    /// If nothing is found, beging search backwards in time from the original date until valid data is found.
    /// </summary>
    /// <param name="rate">Specific rate currency and date to search for</param>
    /// <param name="token">Aynch Cancellation Token if we need to cancel and start a new request</param>
    /// <returns>Potentially return our RatesResponse from the API</returns>
    private async Task<RatesResponse?> QueryApi(Rate rate, CancellationToken token)
    {
        var client = _clientFactory.CreateClient("ExchangeApi");

        var startDate = Formatting.ToDateOnly(rate.Date); // Create DateOnly type from Date string
        var today = DateOnly.FromDateTime(DateTime.UtcNow); // Create DateOnly type for today

        if (startDate > today)
        {
            // Ensure that the provided Date is before today; we can't see future data that isn't there
            Console.WriteLine($"[WARN] Requested future Date {startDate}. Using today instead.");
            startDate = today; // Clamp highest startDate to today
        }

        // After comparing dates, we can begin attempting to fetch the data using our date

        async Task<RatesResponse?> Fetch(DateOnly d)
        {
            // If the request is canceled, throw and exit this method
            token.ThrowIfCancellationRequested();

            // Convert our date back into a string and use it to query our API
            var dateString = Formatting.ToApiDate(d);
            var path = $"currency-api@{dateString}/v1/currencies/{rate.CurrencyCode}.json";

            Console.WriteLine($"[API] GET {path}");
            using var response = await client.GetAsync(path);

            // Ensure that we received a success reply, otherwise return null
            if (response.StatusCode == HttpStatusCode.NotFound) return null;

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<RatesResponse?>();

            // After reading the JSON as a RatesResponse object, save the result to the Cache
            if (result != null) await _cacheService.WriteToCache(result);

            return result;
        }

        // Begin querying the API with the startDate
        var initial = await Fetch(startDate);
        if (initial != null) return initial;

        Console.WriteLine($"[WARN] No data for {rate.Date}, seraching nearby dates...");
        // If we didn't find anything, start searching days forward in time for any results
        if (startDate < today)
        {
            // Begin search dates forward
            for (var d = startDate.AddDays(1); d <= today; d = d.AddDays(1))
            {
                // If the request is canceled, throw and exit this method
                token.ThrowIfCancellationRequested();
                // Start fetching the next day and return if found
                var result = await Fetch(d);
                if (result != null)
                {
                    Console.WriteLine($"[INFO] Found data at {Formatting.ToApiDate(d)}");
                    return result;
                }
            }
        }

        // If nothing was found, begin searching dates backwards to a limit of 1 year
        var lowerBound = startDate.AddDays(-365);
        for (var d = startDate.AddDays(-1); d >= lowerBound; d = d.AddDays(-1))
        {
            // If the request is canceled, throw and exit this method
            token.ThrowIfCancellationRequested();
            // Start fetching the next day and return if found
            var result = await Fetch(d);
            if (result != null)
            {
                Console.WriteLine($"[INFO] Found earlier data at {Formatting.ToApiDate(d)}");
                return result;
            }
        }

        Console.WriteLine($"[WARN] No data found near {rate.Date}");
        return null;
    }

}