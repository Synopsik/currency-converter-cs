namespace currency_converter_cs.Components.Utils;
using System.Text.RegularExpressions;

public static class Formatting
{
    private static readonly Regex DateRegex = new Regex(@"^\d{4}\.\d{2}\.\d{2}$", RegexOptions.Compiled);

    public static string NormalizeDate(string date)
    {
        var normalizedDate = String.Empty;
        // If date != "latest", try to normalize the given date to the required form yyyy.M.d
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
                    return normalizedDate;
                }
            }
        }
        else
        {
            normalizedDate = DateTime.UtcNow.ToString("yyyy.M.d");
        }

        return normalizedDate;
    }

    public static DateOnly ToDateOnly(string dateString)
    {
        var parts = dateString.Split('.');
        return new DateOnly(
            int.Parse(parts[0]),
            int.Parse(parts[1]),
            int.Parse(parts[2])
        );
    }

    public static string ToApiDate(DateOnly d) => $"{d:yyyy.M.d}";

}