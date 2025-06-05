using StockTrading.Application.DTOs.Stock;

namespace StockTrading.Application.DTOs.Cache;

public class CachedStockSearchResult
{
    public List<StockSearchResult> Stocks { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public string SearchQuery { get; init; } = string.Empty;
    public DateTime CachedAt { get; init; }
    public int HitCount { get; set; }

    public bool IsValid()
    {
        return Stocks.Count != 0 &&
               !string.IsNullOrWhiteSpace(SearchQuery) &&
               Page > 0 &&
               PageSize > 0;
    }
}