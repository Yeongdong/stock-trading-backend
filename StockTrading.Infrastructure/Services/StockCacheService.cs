using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StockTrading.Application.DTOs.Cache;
using StockTrading.Application.DTOs.Stock;
using StockTrading.Application.Services;
using StockTrading.Domain.Settings;
using StockTrading.Infrastructure.Cache;

namespace StockTrading.Infrastructure.Services;

public class StockCacheService : IStockCacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IDatabase _redisDatabase;
    private readonly CacheTtl _cacheTtl;
    private readonly CacheMetrics _cacheMetrics;
    private readonly CacheSettings _cacheSettings;
    private readonly ILogger<StockCacheService> _logger;

    public StockCacheService(IDistributedCache distributedCache, IConnectionMultiplexer redis, CacheTtl cacheTtl,
        CacheMetrics cacheMetrics, IOptions<CacheSettings> cacheSettings, ILogger<StockCacheService> logger)
    {
        _distributedCache = distributedCache;
        _redisDatabase = redis.GetDatabase();
        _cacheTtl = cacheTtl;
        _cacheMetrics = cacheMetrics;
        _cacheSettings = cacheSettings.Value;
        _logger = logger;
    }

    #region 검색 결과 캐시

    public async Task<CachedStockSearchResult?> GetSearchResultAsync(string searchTerm, int page, int pageSize)
    {
        if (!_cacheSettings.Enabled) return null;

        var key = CacheKeys.SearchResult(searchTerm, page, pageSize);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var cachedData = await _distributedCache.GetStringAsync(key);
        stopwatch.Stop();

        if (cachedData == null)
        {
            _cacheMetrics.RecordMiss(key, stopwatch.Elapsed);
            _logger.LogDebug("캐시 미스: {Key}", key);
            return null;
        }

        var result = JsonSerializer.Deserialize<CachedStockSearchResult>(cachedData);
        _cacheMetrics.RecordHit(key, stopwatch.Elapsed);
        _logger.LogDebug("캐시 히트: {Key}, 결과 수: {Count}", key, result?.Stocks.Count ?? 0);

        if (result == null) return result;
        result.HitCount++;
        await UpdateHitCountAsync(key, result);

        return result;
    }

    #endregion

    #region 자동완성 캐시

    public async Task SetSearchResultAsync(string searchTerm, int page, int pageSize, List<StockSearchResult> stocks,
        int totalCount)
    {
        if (!_cacheSettings.Enabled) return;

        var key = CacheKeys.SearchResult(searchTerm, page, pageSize);

        var cachedResult = new CachedStockSearchResult
        {
            Stocks = stocks,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            SearchQuery = searchTerm,
            CachedAt = DateTime.UtcNow,
            HitCount = 0
        };

        var jsonData = JsonSerializer.Serialize(cachedResult);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheTtl.SearchResults
        };

        await _distributedCache.SetStringAsync(key, jsonData, options);

        await IncrementSearchCountAsync(searchTerm);
        _logger.LogDebug("검색 결과 캐시 저장: {Key}, 결과 수: {Count}", key, stocks.Count);
    }

    public async Task<CachedAutoCompleteResponse?> GetAutoCompleteAsync(string prefix, int maxResults = 10)
    {
        if (!_cacheSettings.Enabled || string.IsNullOrWhiteSpace(prefix)) return null;

        var key = CacheKeys.AutoComplete(prefix);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var cachedData = await _distributedCache.GetStringAsync(key);
        stopwatch.Stop();

        if (cachedData == null)
        {
            _cacheMetrics.RecordMiss(key, stopwatch.Elapsed);
            return null;
        }

        var result = JsonSerializer.Deserialize<CachedAutoCompleteResponse>(cachedData);
        _cacheMetrics.RecordHit(key, stopwatch.Elapsed);

        return result;
    }

    public async Task SetAutoCompleteAsync(string prefix, List<CachedAutoCompleteItem> items, int maxResults = 10)
    {
        if (!_cacheSettings.Enabled || string.IsNullOrWhiteSpace(prefix)) return;

        var key = CacheKeys.AutoComplete(prefix);

        var response = new CachedAutoCompleteResponse
        {
            Items = items.Take(maxResults).ToList(),
            Prefix = prefix,
            CachedAt = DateTime.UtcNow,
            MaxResults = maxResults
        };

        var jsonData = JsonSerializer.Serialize(response);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheTtl.AutoComplete
        };

        await _distributedCache.SetStringAsync(key, jsonData, options);
        _logger.LogDebug("자동완성 캐시 저장: {Key}, 결과 수: {Count}", key, items.Count);
    }

    #endregion

    #region 개별 종목 캐시

    public async Task<StockSearchResult?> GetStockByCodeAsync(string code)
    {
        if (!_cacheSettings.Enabled || string.IsNullOrWhiteSpace(code)) return null;

        var key = CacheKeys.StockByCode(code);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var cachedData = await _distributedCache.GetStringAsync(key);
        stopwatch.Stop();

        if (cachedData == null)
        {
            _cacheMetrics.RecordMiss(key, stopwatch.Elapsed);
            return null;
        }

        var result = JsonSerializer.Deserialize<StockSearchResult>(cachedData);
        _cacheMetrics.RecordHit(key, stopwatch.Elapsed);

        return result;
    }

    public async Task SetStockByCodeAsync(string code, StockSearchResult stock)
    {
        if (!_cacheSettings.Enabled || string.IsNullOrWhiteSpace(code)) return;

        var key = CacheKeys.StockByCode(code);

        var jsonData = JsonSerializer.Serialize(stock);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheTtl.StockDetail
        };

        await _distributedCache.SetStringAsync(key, jsonData, options);
        _logger.LogDebug("종목 상세 캐시 저장: {Code}", code);
    }

    #endregion

    #region 요약 정보 캐시

    public async Task<CachedStockSummary?> GetStockSummaryAsync()
    {
        if (!_cacheSettings.Enabled) return null;

        var key = CacheKeys.SearchSummary;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var cachedData = await _distributedCache.GetStringAsync(key);
        stopwatch.Stop();

        if (cachedData == null)
        {
            _cacheMetrics.RecordMiss(key, stopwatch.Elapsed);
            return null;
        }

        var result = JsonSerializer.Deserialize<CachedStockSummary>(cachedData);
        _cacheMetrics.RecordHit(key, stopwatch.Elapsed);

        return result;
    }

    public async Task SetStockSummaryAsync(StockSearchSummary summary)
    {
        if (!_cacheSettings.Enabled) return;

        var key = CacheKeys.SearchSummary;

        var cachedSummary = new CachedStockSummary
        {
            TotalCount = summary.TotalCount,
            LastUpdated = summary.LastUpdated,
            MarketCounts = summary.MarketCounts,
            CachedAt = DateTime.UtcNow,
            TotalSearchCount = 0, // 별도로 관리
            PopularSearchTerms = await GetPopularSearchTermsAsync()
        };

        var jsonData = JsonSerializer.Serialize(cachedSummary);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheTtl.Metadata
        };

        await _distributedCache.SetStringAsync(key, jsonData, options);
        _logger.LogDebug("종목 요약 정보 캐시 저장");
    }

    #endregion

    #region 캐시 관리

    public async Task InvalidateSearchCacheAsync()
    {
        _logger.LogInformation("검색 관련 캐시 무효화 시작");

        var patterns = new[]
        {
            CacheKeys.Patterns.AllSearchResults,
            CacheKeys.Patterns.AllAutoComplete
        };

        foreach (var pattern in patterns)
        {
            await InvalidateCacheByPatternAsync(pattern);
        }

        _logger.LogInformation("검색 관련 캐시 무효화 완료");
    }

    public async Task InvalidateAllStockCacheAsync()
    {
        _logger.LogInformation("모든 종목 캐시 무효화 시작");

        var patterns = new[]
        {
            CacheKeys.Patterns.AllSearchResults,
            CacheKeys.Patterns.AllAutoComplete,
            CacheKeys.Patterns.AllStocksByMarket,
            CacheKeys.Patterns.AllStockDetails,
            CacheKeys.Patterns.AllMetadata
        };

        foreach (var pattern in patterns)
        {
            await InvalidateCacheByPatternAsync(pattern);
        }

        await _distributedCache.RemoveAsync(CacheKeys.AllStocks);
        await _distributedCache.RemoveAsync(CacheKeys.SearchSummary);
        await _distributedCache.RemoveAsync(CacheKeys.LastUpdated);

        _logger.LogInformation("모든 종목 캐시 무효화 완료");
    }

    public async Task InvalidateCacheByPatternAsync(string pattern)
    {
        var server = _redisDatabase.Multiplexer.GetServer(
            _redisDatabase.Multiplexer.GetEndPoints().First());

        var keys = server.Keys(pattern: pattern);

        var keyArray = keys.ToArray();
        if (keyArray.Length > 0)
        {
            await _redisDatabase.KeyDeleteAsync(keyArray);
            _logger.LogDebug("패턴 캐시 삭제: {Pattern}, 삭제된 키 수: {Count}", pattern, keyArray.Length);
        }
    }

    public async Task<CachePerformanceSummary> GetCacheMetricsAsync()
    {
        var summary = _cacheMetrics.GetSummary();

        var key = CacheKeys.CacheMetrics;
        var jsonData = JsonSerializer.Serialize(summary);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        await _distributedCache.SetStringAsync(key, jsonData, options);

        return summary;
    }

    #endregion

    #region 인기 검색어 관리

    public async Task IncrementSearchCountAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm)) return;

        var sanitizedTerm = CacheKeys.SanitizeKey(searchTerm);
        var key = $"stocktrading:cache:search_count:{CacheKeys.SanitizeKey(searchTerm)}";

        await _redisDatabase.StringIncrementAsync(key);
        await _redisDatabase.KeyExpireAsync(key, TimeSpan.FromDays(30));

        var timestampKey = $"stocktrading:cache:search_time:{CacheKeys.SanitizeKey(sanitizedTerm)}";
        await _redisDatabase.StringSetAsync(timestampKey, DateTime.UtcNow.Ticks);
        await _redisDatabase.KeyExpireAsync(timestampKey, TimeSpan.FromDays(30));
    }

    public async Task<List<PopularSearchTerm>> GetPopularSearchTermsAsync(int count = 10)
    {
        var server = _redisDatabase.Multiplexer.GetServer(
            _redisDatabase.Multiplexer.GetEndPoints().First());

        var searchCountKeys = server.Keys(pattern: "stocktrading:cache:search_count:*");
        var popularTerms = new List<PopularSearchTerm>();

        foreach (var key in searchCountKeys)
        {
            var searchTerm = ExtractSearchTermFromKey(key);
            var searchCountValue = await _redisDatabase.StringGetAsync(key);

            if (!searchCountValue.HasValue || !searchCountValue.TryParse(out int searchCount) ||
                searchCount <= 0) continue;
            var timestampKey = $"stocktrading:cache:search_time:{CacheKeys.SanitizeKey(searchTerm)}";
            var timestampValue = await _redisDatabase.StringGetAsync(timestampKey);

            var lastSearched = DateTime.MinValue;
            if (timestampValue.HasValue && timestampValue.TryParse(out long ticks))
            {
                try
                {
                    lastSearched = new DateTime(ticks);
                }
                catch (ArgumentOutOfRangeException)
                {
                    lastSearched = DateTime.UtcNow;
                }
            }

            popularTerms.Add(new PopularSearchTerm
            {
                Term = searchTerm,
                SearchCount = searchCount,
                LastSearched = lastSearched
            });
        }

        return popularTerms
            .OrderByDescending(x => x.SearchCount)
            .ThenByDescending(x => x.LastSearched)
            .Take(count)
            .ToList();
    }

    #endregion

    #region Private Helper Methods

    private async Task UpdateHitCountAsync(string key, CachedStockSearchResult result)
    {
        var jsonData = JsonSerializer.Serialize(result);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheTtl.SearchResults
        };

        await _distributedCache.SetStringAsync(key, jsonData, options);
    }

    private static string ExtractSearchTermFromKey(string key)
    {
        const string prefix = "stocktrading:cache:search_count:";
        if (key.StartsWith(prefix))
            return key[prefix.Length..];

        var parts = key.Split(':');
        return parts.Length >= 4 ? parts[3] : string.Empty;
    }

    #endregion
}