namespace StockTrading.Application.Features.Market.DTOs.Stock;

public class StockSearchResponse
{
    public List<StockSearchResult> Results { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public bool HasMore { get; init; }
    public int HitCount { get; set; } = 0;
    public DateTime CachedAt { get; init; } = DateTime.UtcNow;
}