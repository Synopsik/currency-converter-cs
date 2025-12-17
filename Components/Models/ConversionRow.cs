// Steven Baar
// 12/18/26
// Currency Converter
// This model is the template for the rows in our conversion table

namespace currency_converter_cs.Components.Models;

public class ConversionRow
{
    public required string From { get; init; }
    public required string To { get; init; }
    public required decimal Rate { get; init; }
    public required string Date { get; init; }
}