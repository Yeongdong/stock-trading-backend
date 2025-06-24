using StockTrading.Application.Features.Market.DTOs.Stock;

namespace StockTrading.Application.ExternalServices;

public interface IFinnhubApiClient
{
    Task<ForeignStockSearchResult> SearchSymbolsAsync(ForeignStockSearchRequest request);
}