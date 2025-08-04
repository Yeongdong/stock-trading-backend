using StockTrading.Application.Features.Market.DTOs.Stock;

namespace StockTrading.Application.Features.Market.Services;

public interface IStockCacheService
{
    #region 통합 검색

    Task<StockSearchResponse> SearchStocksAsync(string searchTerm, int page = 1, int pageSize = 20);
    Task LoadAllStocksAsync();
    Task RefreshCacheAsync();

    #endregion

    #region 종목 상세

    Task<StockSearchResult?> GetStockByCodeAsync(string code);

    #endregion

    #region 캐시 관리

    void InvalidateCache();
    CacheStats GetCacheStats();

    #endregion
}

/// <summary>
/// 캐시 통계
/// </summary>
public class CacheStats
{
    public int TotalStocks { get; set; }
    public DateTime LastLoadedAt { get; set; }
    public long SearchCount { get; set; }
    public double HitRatio { get; set; }
}