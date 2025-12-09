namespace currency_converter_cs.Components.Models;

public class ConversionRow
{
    public string From { get; set; }
    public string To { get; set; }
    public decimal Rate { get; set; }
    public string Date { get; set; }
}