// Steven Baar
// 12/18/26
// Currency Converter
// This model is used to identify the conversions that must be saved

namespace currency_converter_cs.Components.Models;

public class FavoritePair
{
    public required string BaseCurrency { get; init; }
    public required string TargetCurrency { get; init; }
}