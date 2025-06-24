using StockTrading.Application.Features.Market.DTOs.Stock;

namespace StockTrading.Application.Features.Market.Services;

public interface IStockService
{
    // 주식 검색 및 조회
    Task<StockSearchResponse> SearchStocksAsync(string searchTerm, int page = 1, int pageSize = 20);
    Task<StockSearchResult?> GetStockByCodeAsync(string code);
    Task<List<StockSearchResult>> GetStocksByMarketAsync(StockTrading.Domain.Enums.Market market);
    Task<StockSearchSummary> GetSearchSummaryAsync();

    // 데이터 동기화
    Task SyncDomesticStockDataAsync();
}