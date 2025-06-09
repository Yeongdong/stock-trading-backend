using StockTrading.Application.Features.Market.DTOs.Stock;

namespace StockTrading.Application.Features.Market.Services;

public interface IStockService
{
    Task<List<StockSearchResult>> SearchStocksAsync(string searchTerm, int page = 1, int pageSize = 20);
    Task<StockSearchResult?> GetStockByCodeAsync(string code);
    Task UpdateStockDataFromKrxAsync();
    Task<StockSearchSummary> GetSearchSummaryAsync();
}