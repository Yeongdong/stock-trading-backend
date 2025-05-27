using StockTrading.Application.DTOs.External.KoreaInvestment;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

namespace StockTrading.Application.Services;

public interface IKisRealTimeDataProcessor
{
    event EventHandler<KisTransactionInfo> StockPriceReceived;
    event EventHandler<object> TradeExecutionReceived;
    void ProcessMessage(string messageJson);
}