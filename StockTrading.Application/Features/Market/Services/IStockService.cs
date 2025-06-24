using StockTrading.Application.Features.Market.DTOs.Stock;

namespace StockTrading.Application.Features.Market.Services;

public interface IStockService
{
    Task<StockSearchResponse> SearchStocksAsync(string searchTerm, int page = 1, int pageSize = 20);
    Task<StockSearchResult?> GetStockByCodeAsync(string code);
    Task SyncDomesticStockDataAsync();
    Task<StockSearchSummary> GetSearchSummaryAsync();
    Task<List<StockSearchResult>> GetStocksByMarketAsync(StockTrading.Domain.Enums.Market market);
}