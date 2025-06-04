using Microsoft.AspNetCore.SignalR;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.Services;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

/*
 * SignalR을 통한 클라이언트 메시지 브로드캐스팅
 */
public class RealTimeDataBroadcaster : IRealTimeDataBroadcaster
{
    private readonly IHubContext<StockHub> _hubContext;

    public RealTimeDataBroadcaster(IHubContext<StockHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task BroadcastStockPriceAsync(KisTransactionInfo priceData)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveStockPrice", priceData);
    }

    public async Task BroadcastTradeExecutionAsync(object executionData)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveTradeExecution", executionData);
    }
}