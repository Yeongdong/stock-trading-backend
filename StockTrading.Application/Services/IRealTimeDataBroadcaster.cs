using StockTrading.Application.DTOs.External.KoreaInvestment;

namespace StockTrading.Application.Services;

public interface IRealTimeDataBroadcaster
{
    Task BroadcastStockPriceAsync(StockTransaction priceData);
    Task BroadcastTradeExecutionAsync(object executionData);
}