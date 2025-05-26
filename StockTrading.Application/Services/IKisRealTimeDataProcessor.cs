using StockTrading.Application.DTOs.External.KoreaInvestment;

namespace StockTrading.Application.Services;

public interface IKisRealTimeDataProcessor
{
    event EventHandler<StockTransaction> StockPriceReceived;
    event EventHandler<object> TradeExecutionReceived;
    void ProcessMessage(string messageJson);
}