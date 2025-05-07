using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Models;

namespace StockTrading.Infrastructure.ExternalServices.Interfaces;

public interface IRealTimeDataBroadcaster
{
    Task BroadcastStockPriceAsync(StockTransaction priceData);
    Task BroadcastTradeExecutionAsync(object executionData);
}