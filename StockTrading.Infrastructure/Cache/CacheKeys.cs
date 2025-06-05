namespace StockTrading.Infrastructure.Cache;

public class CacheKeys
{
    private const string PREFIX = "stocktrading:cache:";
    public static string AllStocks => $"{PREFIX}stocks:all";

    public static string SearchResult(string query, int page, int pageSize)
        => $"{PREFIX}search:{SanitizeKey(query)}:p{page}:s{pageSize}";

    public static string StocksByMarket(string market)
        => $"{PREFIX}stocks:market:{SanitizeKey(market)}";

    public static string StockByCode(string code)
        => $"{PREFIX}stock:code:{code}";

    public static string AutoComplete(string prefix)
        => $"{PREFIX}autocomplete:{SanitizeKey(prefix)}";

    public static string SearchSummary => $"{PREFIX}metadata:summary";
    public static string LastUpdated => $"{PREFIX}metadata:last_updated";
    public static string CacheMetrics => $"{PREFIX}metrics:performance";

    public static string SanitizeKey(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "empty";

        return input
            .ToLowerInvariant()
            .Replace(" ", "_")
            .Replace(":", "_")
            .Replace("|", "_")
            .Replace("*", "_")
            .Replace("?", "_");
    }

    public static class Patterns
    {
        public static string AllSearchResults => $"{PREFIX}search:*";
        public static string AllAutoComplete => $"{PREFIX}autocomplete:*";
        public static string AllStocksByMarket => $"{PREFIX}stocks:market:*";
        public static string AllStockDetails => $"{PREFIX}stock:code:*";
        public static string AllMetadata => $"{PREFIX}metadata:*";
    }
}