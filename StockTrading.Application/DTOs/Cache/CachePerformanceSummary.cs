namespace StockTrading.Application.DTOs.Cache;

public class CachePerformanceSummary
{
    public long TotalHits { get; set; }
    public long TotalMisses { get; set; }
    public double HitRatio { get; set; }
    public Dictionary<string, CacheKeyMetric> KeyMetrics { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}