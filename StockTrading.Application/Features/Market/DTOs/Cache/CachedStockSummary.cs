namespace StockTrading.Application.Features.Market.DTOs.Cache;

public class CachedStockSummary
{
    public int TotalCount { get; init; }
    public DateTime? LastUpdated { get; init; }
    public Dictionary<string, int> MarketCounts { get; init; } = new();
    public DateTime CachedAt { get; init; }
    public long TotalSearchCount { get; init; }
    public List<PopularSearchTerm> PopularSearchTerms { get; init; } = [];
}

public class PopularSearchTerm
{
    public string Term { get; init; } = string.Empty;
    public int SearchCount { get; init; }
    public DateTime LastSearched { get; init; }
}
