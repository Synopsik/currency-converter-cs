// Steven Baar
// 12/18/26
// Currency Converter
// Formatting util for converting and working with dates

namespace currency_converter_cs.Components.Utils;
using System.Text.RegularExpressions;

public static class Formatting
{
    // Regex pattern to determine if the input date matches yyyy.M.d
    private static readonly Regex DateRegex = new Regex(@"^\d{4}\.\d{2}\.\d{2}$", RegexOptions.Compiled);

    /// <summary>
    /// Normalizes any given Date (ex. 1/1/25, 01-02-24, etc.) to the structure of yyyy.M.d
    /// </summary>
    /// <param name="date">Any given string in the format of a Date</param>
    /// <returns>A normalized string of the Date</returns>
    public static string NormalizeDate(string date)
    {
        // If the Date empty, or if lastest is passed, we return the current date (ToDateOnly will eventually parse this)
        if (string.Equals(date, "latest", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(date))
            return DateTime.UtcNow.ToString("yyyy.M.d");

        // If our Date already matches our Regex pattern, return the Date
        if (DateRegex.IsMatch(date))
            return date;

        Console.WriteLine($"[WARN] Normalizing Date: {date}");
        // Parse and format Date using the DateTime.TryParse method
        if (DateTime.TryParse(date, out var parsed))
            // Then return a string of that Date
            return parsed.ToString("yyyy.M.d");

        // If all else fails, return the current Date
        Console.WriteLine($"[ERROR] Could not parse '{date}', using latest instead.");
        return DateTime.UtcNow.ToString("yyyy.M.d");
    }

    /// <summary>
    /// Converts a normalized Date string into a DateOnly type
    /// </summary>
    /// <param name="dateString">String in the format yyyy.M.d</param>
    /// <returns>DateOnly representation of the string</returns>
    public static DateOnly ToDateOnly(string dateString)
    {
        // Split the input string on the separators
        var parts = dateString.Split('.');
        // Return the DateOnly construct
        return new DateOnly(
            int.Parse(parts[0]),
            int.Parse(parts[1]),
            int.Parse(parts[2])
        );
    }

    /// <summary>
    /// Converts a DateOnly type back into a normalized Date string
    /// </summary>
    /// <param name="d">DateOnly types</param>
    /// <returns>String in the format yyyy.M.d</returns>
    public static string ToApiDate(DateOnly d) => $"{d:yyyy.M.d}";
}