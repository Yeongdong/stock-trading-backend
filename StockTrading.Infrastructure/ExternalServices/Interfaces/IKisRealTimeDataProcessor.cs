using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Models;

namespace StockTrading.Infrastructure.ExternalServices.Interfaces;

public interface IKisRealTimeDataProcessor
{
    event EventHandler<StockTransaction> StockPriceReceived;
    event EventHandler<object> TradeExecutionReceived;
    void ProcessMessage(string messageJson);
}