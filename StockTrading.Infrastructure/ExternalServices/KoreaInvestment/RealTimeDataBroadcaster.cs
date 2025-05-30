using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.Services;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

/*
 * SignalR을 통한 클라이언트 메시지 브로드캐스팅
 */
public class RealTimeDataBroadcaster : IRealTimeDataBroadcaster
{
    private readonly IHubContext<StockHub> _hubContext;
    private readonly ILogger<RealTimeDataBroadcaster> _logger;

    public RealTimeDataBroadcaster(IHubContext<StockHub> hubContext, ILogger<RealTimeDataBroadcaster> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task BroadcastStockPriceAsync(KisTransactionInfo priceData)
    {
            _logger.LogInformation("SignalR 브로드캐스트 시작: {Symbol} - {Price}원", 
                priceData.Symbol, priceData.Price);

            await _hubContext.Clients.All.SendAsync("ReceiveStockPrice", priceData);
        
            _logger.LogInformation("SignalR 브로드캐스트 완료: {Symbol} - {Price}원, 연결된 클라이언트에게 전송됨", 
                priceData.Symbol, priceData.Price);
    }

    public async Task BroadcastTradeExecutionAsync(object executionData)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveTradeExecution", executionData);
    }
}