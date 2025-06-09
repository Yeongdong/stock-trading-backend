using StockTrading.Application.Features.Market.DTOs.Cache;
using StockTrading.Application.Features.Market.DTOs.Stock;

namespace StockTrading.Application.Features.Market.Services;

public interface IStockCacheService
{
    #region 검색 결과 캐시

    Task<CachedStockSearchResult?> GetSearchResultAsync(string searchTerm, int page, int pageSize);

    Task SetSearchResultAsync(string searchTerm, int page, int pageSize, List<StockSearchResult> stocks,
        int totalCount);

    #endregion

    #region 자동완성 캐시

    Task<CachedAutoCompleteResponse?> GetAutoCompleteAsync(string prefix, int maxResults = 10);
    Task SetAutoCompleteAsync(string prefix, List<CachedAutoCompleteItem> items, int maxResults = 10);

    #endregion

    #region 종목 상세 캐시

    Task<StockSearchResult?> GetStockByCodeAsync(string code);
    Task SetStockByCodeAsync(string code, StockSearchResult stock);

    #endregion

    #region 요약 정보 캐시

    Task<CachedStockSummary?> GetStockSummaryAsync();
    Task SetStockSummaryAsync(StockSearchSummary summary);

    #endregion

    #region 캐시 관리

    Task InvalidateSearchCacheAsync();
    Task InvalidateAllStockCacheAsync();
    Task InvalidateCacheByPatternAsync(string pattern);
    Task<CachePerformanceSummary> GetCacheMetricsAsync();

    #endregion

    #region 인기 검색어 관리

    Task IncrementSearchCountAsync(string searchTerm);
    Task<List<PopularSearchTerm>> GetPopularSearchTermsAsync(int count = 10);

    #endregion
}