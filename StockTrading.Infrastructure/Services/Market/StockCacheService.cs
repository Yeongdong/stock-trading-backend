using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrading.Application.Features.Market.DTOs.Stock;
using StockTrading.Application.Features.Market.Repositories;
using StockTrading.Application.Features.Market.Services;
using StockTrading.Domain.Settings.Infrastructure;
using System.Collections.Concurrent;

namespace StockTrading.Infrastructure.Services.Market;

/// <summary>
/// 메모리 기반 주식 캐시 서비스
/// Redis 없이 ConcurrentDictionary 사용
/// </summary>
public class StockCacheService : IStockCacheService
{
    private readonly IStockRepository _stockRepository;
    private readonly CacheSettings _cacheSettings;
    private readonly ILogger<StockCacheService> _logger;

    // 메모리 캐시 저장소
    private static readonly ConcurrentDictionary<string, StockSearchResult> _stocksById = new();
    private static readonly List<StockSearchResult> _allStocks = [];
    private static readonly object _lockObject = new();
    
    // 캐시 메타데이터
    private static DateTime _lastLoadedAt = DateTime.MinValue;
    private static long _searchCount = 0;
    private static long _hitCount = 0;

    public StockCacheService(
        IStockRepository stockRepository,
        IOptions<CacheSettings> cacheSettings,
        ILogger<StockCacheService> logger)
    {
        _stockRepository = stockRepository;
        _cacheSettings = cacheSettings.Value;
        _logger = logger;
    }

    #region 통합 검색

    public async Task<StockSearchResponse> SearchStocksAsync(string searchTerm, int page = 1, int pageSize = 20)
    {
        Interlocked.Increment(ref _searchCount);

        if (!_cacheSettings.Enabled)
            return await SearchFromDatabaseAsync(searchTerm, page, pageSize);

        await EnsureCacheLoadedAsync();

        List<StockSearchResult> allStocks;
        lock (_lockObject)
        {
            allStocks = new List<StockSearchResult>(_allStocks);
        }

        if (allStocks.Count == 0)
            return await SearchFromDatabaseAsync(searchTerm, page, pageSize);

        var filteredStocks = FilterAndSortStocks(allStocks, searchTerm);
        var totalCount = filteredStocks.Count;

        var pagedResults = filteredStocks
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        Interlocked.Increment(ref _hitCount);

        return new StockSearchResponse
        {
            Results = pagedResults,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            HasMore = totalCount > page * pageSize
        };
    }

    public async Task LoadAllStocksAsync()
    {
        if (!_cacheSettings.Enabled) return;

        _logger.LogInformation("메모리에 모든 주식 데이터 로드 시작");

        var allStocks = await _stockRepository.GetAllAsync();
        var stockResults = allStocks.Select(MapToSearchResult).ToList();

        lock (_lockObject)
        {
            _allStocks.Clear();
            _allStocks.AddRange(stockResults);
            
            _stocksById.Clear();
            foreach (var stock in stockResults)
            {
                _stocksById[stock.Code] = stock;
            }
            
            _lastLoadedAt = DateTime.UtcNow;
        }

        _logger.LogInformation("메모리 로드 완료: {Count}개 종목", stockResults.Count);
    }

    public async Task RefreshCacheAsync()
    {
        await LoadAllStocksAsync();
    }

    #endregion

    #region 종목 상세

    public async Task<StockSearchResult?> GetStockByCodeAsync(string code)
    {
        if (!_cacheSettings.Enabled || string.IsNullOrWhiteSpace(code))
            return null;

        await EnsureCacheLoadedAsync();

        _stocksById.TryGetValue(code, out var stock);
        
        if (stock != null)
            Interlocked.Increment(ref _hitCount);
        
        Interlocked.Increment(ref _searchCount);

        return stock;
    }

    #endregion

    #region 캐시 관리

    public void InvalidateCache()
    {
        _logger.LogInformation("메모리 캐시 무효화");
        
        lock (_lockObject)
        {
            _allStocks.Clear();
            _stocksById.Clear();
            _lastLoadedAt = DateTime.MinValue;
        }
    }

    public CacheStats GetCacheStats()
    {
        return new CacheStats
        {
            TotalStocks = _allStocks.Count,
            LastLoadedAt = _lastLoadedAt,
            SearchCount = _searchCount,
            HitRatio = _searchCount > 0 ? (double)_hitCount / _searchCount * 100 : 0
        };
    }

    #endregion

    #region Private Methods

    private async Task EnsureCacheLoadedAsync()
    {
        if (_lastLoadedAt == DateTime.MinValue || 
            DateTime.UtcNow - _lastLoadedAt > TimeSpan.FromHours(24))
        {
            await LoadAllStocksAsync();
        }
    }

    private static List<StockSearchResult> FilterAndSortStocks(List<StockSearchResult> allStocks, string searchTerm)
    {
        var normalizedTerm = searchTerm.Trim().ToLower();

        return allStocks
            .Where(stock =>
                stock.Name.ToLower().Contains(normalizedTerm) ||
                stock.Code.Contains(searchTerm) ||
                (stock.Sector?.ToLower().Contains(normalizedTerm) ?? false))
            .OrderBy(stock =>
                // 우선순위 정렬
                stock.Name.ToLower() == normalizedTerm ? 0 :           // 완전일치
                stock.Code == searchTerm ? 1 :                         // 코드일치
                stock.Name.ToLower().StartsWith(normalizedTerm) ? 2 : 3) // 시작일치
            .ThenBy(stock => stock.Name.Length)                        // 짧은 이름 우선
            .ThenBy(stock => stock.Name)                               // 가나다순
            .ToList();
    }

    private async Task<StockSearchResponse> SearchFromDatabaseAsync(string searchTerm, int page, int pageSize)
    {
        _logger.LogInformation("DB 검색으로 대체: {SearchTerm}", searchTerm);

        var stocks = await _stockRepository.SearchByNameAsync(searchTerm, page, pageSize);
        var totalCount = await GetSearchTotalCountFromDbAsync(searchTerm);

        return new StockSearchResponse
        {
            Results = stocks.Select(MapToSearchResult).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            HasMore = totalCount > page * pageSize
        };
    }

    private async Task<int> GetSearchTotalCountFromDbAsync(string searchTerm)
    {
        var allResults = await _stockRepository.SearchByNameAsync(searchTerm, 1, int.MaxValue);
        return allResults.Count;
    }

    private static StockSearchResult MapToSearchResult(Domain.Entities.Stock stock)
    {
        return new StockSearchResult
        {
            Code = stock.Code,
            Name = stock.Name,
            Sector = stock.Sector,
            Market = stock.Market.ToString()
        };
    }

    #endregion
}