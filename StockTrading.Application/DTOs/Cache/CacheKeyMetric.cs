namespace StockTrading.Application.DTOs.Cache;

public class CacheKeyMetric
{
    public string KeyPattern { get; init; } = string.Empty;
    public long HitCount { get; set; }
    public long MissCount { get; set; }
    public TimeSpan TotalResponseTime { get; set; }
    public DateTime LastAccessed { get; set; }

    public double HitRatio => HitCount + MissCount > 0
        ? (double)HitCount / (HitCount + MissCount)
        : 0;

    public TimeSpan AverageResponseTime => (HitCount + MissCount) > 0
        ? TimeSpan.FromTicks(TotalResponseTime.Ticks / (HitCount + MissCount))
        : TimeSpan.Zero;

    public CacheKeyMetric Clone() => new()
    {
        KeyPattern = KeyPattern,
        HitCount = HitCount,
        MissCount = MissCount,
        TotalResponseTime = TotalResponseTime,
        LastAccessed = LastAccessed
    };
}