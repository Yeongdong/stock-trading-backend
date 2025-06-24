using StockTrading.Application.Features.Market.DTOs.Stock;

namespace StockTrading.Application.Features.Market.Services;

public interface IStockService
{
    // 국내 주식 검색 및 조회
    Task<StockSearchResponse> SearchStocksAsync(string searchTerm, int page = 1, int pageSize = 20);
    Task<StockSearchResult?> GetStockByCodeAsync(string code);
    Task<List<StockSearchResult>> GetStocksByMarketAsync(StockTrading.Domain.Enums.Market market);
    Task<StockSearchSummary> GetSearchSummaryAsync();
    Task SyncDomesticStockDataAsync();

    // 해외 주식
    Task<ForeignStockSearchResult> SearchForeignStocksAsync(ForeignStockSearchRequest request);
}