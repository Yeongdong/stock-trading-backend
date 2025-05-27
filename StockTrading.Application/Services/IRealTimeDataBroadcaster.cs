using StockTrading.Application.DTOs.External.KoreaInvestment;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

namespace StockTrading.Application.Services;

public interface IRealTimeDataBroadcaster
{
    Task BroadcastStockPriceAsync(KisTransactionInfo priceData);
    Task BroadcastTradeExecutionAsync(object executionData);
}