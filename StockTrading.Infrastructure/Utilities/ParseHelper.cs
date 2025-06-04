using System.Globalization;

namespace StockTrading.Infrastructure.Utilities;

public static class ParseHelper
{
    public static decimal ParseDecimalSafely(string? value, decimal defaultValue = 0m)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result)
            ? result
            : defaultValue;
    }

    public static int ParseIntSafely(string? value, int defaultValue = 0)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : defaultValue;
    }

    public static long ParseLongSafely(string? value, long defaultValue = 0L)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalResult))
            return (long)Math.Truncate(decimalResult);

        return defaultValue;
    }

    public static DateTime ParseDateTimeSafely(string? value, string format, DateTime defaultValue = default)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var result)
            ? result
            : defaultValue;
    }

    public static bool IsValidStockCode(string? stockCode, int expectedLength = 6)
    {
        return !string.IsNullOrWhiteSpace(stockCode) &&
               stockCode.Length == expectedLength &&
               stockCode.All(char.IsDigit);
    }
}