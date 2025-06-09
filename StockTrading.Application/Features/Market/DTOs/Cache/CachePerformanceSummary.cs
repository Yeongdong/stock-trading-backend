namespace StockTrading.Application.Features.Market.DTOs.Cache;

public class CachePerformanceSummary
{
    public long TotalHits { get; init; }
    public long TotalMisses { get; init; }
    public double HitRatio { get; init; }
    public Dictionary<string, CacheKeyMetric> KeyMetrics { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}