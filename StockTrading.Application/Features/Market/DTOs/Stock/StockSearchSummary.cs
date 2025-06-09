namespace StockTrading.Application.Features.Market.DTOs.Stock;

public class StockSearchSummary
{
    public int TotalCount { get; init; }
    public DateTime? LastUpdated { get; init; }
    public Dictionary<string, int> MarketCounts { get; init; } = new();
}