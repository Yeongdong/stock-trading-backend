using System.Collections.Concurrent;
using StockTrading.Application.Features.Market.DTOs.Cache;

namespace StockTrading.Infrastructure.Cache;

public class CacheMetrics
{
    private readonly ConcurrentDictionary<string, CacheKeyMetric> _keyMetrics = new();
    private long _totalHits = 0;
    private long _totalMisses = 0;
    private readonly object _lockObject = new();

    public void RecordHit(string key, TimeSpan responseTime)
    {
        Interlocked.Increment(ref _totalHits);
        UpdateKeyMetric(key, true, responseTime);
    }

    public void RecordMiss(string key, TimeSpan responseTime)
    {
        Interlocked.Increment(ref _totalMisses);
        UpdateKeyMetric(key, false, responseTime);
    }

    public double GetHitRatio()
    {
        var total = _totalHits + _totalMisses;
        return total > 0 ? (double)_totalHits / total : 0;
    }

    public CachePerformanceSummary GetSummary()
    {
        lock (_lockObject)
        {
            return new CachePerformanceSummary
            {
                TotalHits = _totalHits,
                TotalMisses = _totalMisses,
                HitRatio = GetHitRatio(),
                KeyMetrics = _keyMetrics.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Clone()
                ),
                LastUpdated = DateTime.UtcNow
            };
        }
    }

    public void Reset()
    {
        lock (_lockObject)
        {
            _totalHits = 0;
            _totalMisses = 0;
            _keyMetrics.Clear();
        }
    }

    private void UpdateKeyMetric(string key, bool isHit, TimeSpan responseTime)
    {
        var keyPattern = ExtractKeyPattern(key);

        _keyMetrics.AddOrUpdate(keyPattern,
            new CacheKeyMetric
            {
                KeyPattern = keyPattern,
                HitCount = isHit ? 1 : 0,
                MissCount = isHit ? 0 : 1,
                TotalResponseTime = responseTime,
                LastAccessed = DateTime.UtcNow
            },
            (_, existing) =>
            {
                if (isHit) existing.HitCount++;
                else existing.MissCount++;

                existing.TotalResponseTime = existing.TotalResponseTime.Add(responseTime);
                existing.LastAccessed = DateTime.UtcNow;
                return existing;
            });
    }

    private static string ExtractKeyPattern(string key)
    {
        var parts = key.Split(':');
        return parts.Length >= 3 ? parts[2] : "unknown";
    }
}
