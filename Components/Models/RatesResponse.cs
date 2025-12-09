using System.Text.Json;
using System.Text.Json.Serialization;

namespace currency_converter_cs.Components.Models;


public class RatesResponse
{
    public string Date { get; set; }
    [JsonExtensionData]
    public Dictionary<string, JsonElement> Rates { get; set; }
}