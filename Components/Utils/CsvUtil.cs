using System.Text;
using currency_converter_cs.Components.Models;


namespace currency_converter_cs.Components.Utils;

public static class CsvUtil
{
    public static async Task<string> FormatTableToCsv(List<ConversionRow> tableRows)
    {
        if (tableRows.Count == 0)
            return "";

        // Build the CSV String
        var sb = new StringBuilder();

        // Header Row
        sb.AppendLine("From Currency,To Currency,Exchange Rate,Date");

        // Data Rows
        foreach (var row in tableRows)
        {
            sb.AppendLine($"{row.From.ToUpper()},{row.To.ToUpper()},{row.Rate},{row.Date}");
        }

        // Return formatted table
        return sb.ToString();
    }
}