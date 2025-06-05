using StockTrading.Application.DTOs.Stock;
using StockTrading.Domain.Entities;

namespace StockTrading.Application.Services;

public interface IStockService
{
    Task<List<StockSearchResult>> SearchStocksAsync(string searchTerm, int page = 1, int pageSize = 20);
    Task<StockSearchResult?> GetStockByCodeAsync(string code);
    Task UpdateStockDataFromKrxAsync();
    Task<StockSearchSummary> GetSearchSummaryAsync();
}