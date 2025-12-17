// Steven Baar
// 12/18/26
// Currency Converter
// This model is used to query the Rate for the currency and specified Date

namespace currency_converter_cs.Components.Models;

public class Rate
{
    public required string CurrencyCode { get; init; }
    public required string Date { get; init; }
}