// Steven Baar
// 12/18/26
// Currency Converter
// This model is used to capture responses from the exchange API

using System.Text.Json;
using System.Text.Json.Serialization;

namespace currency_converter_cs.Components.Models;


public class RatesResponse
{
    public string Date { get; init; }
    [JsonExtensionData] // Treat the Value of our Dictionary as a generic JsonElement that we can access easily
    public Dictionary<string, JsonElement> Rates { get; init; }
}





