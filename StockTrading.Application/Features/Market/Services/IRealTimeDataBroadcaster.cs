using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

namespace StockTrading.Application.Features.Market.Services;

public interface IRealTimeDataBroadcaster
{
    Task BroadcastStockPriceAsync(KisTransactionInfo priceData);
    Task BroadcastTradeExecutionAsync(object executionData);
}