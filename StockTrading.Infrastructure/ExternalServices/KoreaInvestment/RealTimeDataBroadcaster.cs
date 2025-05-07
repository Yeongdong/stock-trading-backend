using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Models;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

/*
 * SignalR을 통한 클라이언트 메시지 브로드캐스팅
 */
public class RealTimeDataBroadcaster
{
    private readonly IHubContext<StockHub> _hubContext;
    private readonly ILogger<RealTimeDataBroadcaster> _logger;

    public RealTimeDataBroadcaster(IHubContext<StockHub> hubContext, ILogger<RealTimeDataBroadcaster> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task BroadcastStockPriceAsync(StockTransaction priceData)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("ReceiveStockPrice", priceData);
            _logger.LogDebug($"실시간 시세 전송: {priceData.Symbol}, 가격: {priceData.Price}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "실시간 시세 브로드캐스팅 오류");
        }
    }

    public async Task BroadcastTradeExecutionAsync(object executionData)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("ReceiveTradeExecution", executionData);
            _logger.LogDebug("체결 정보 전송 완료");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "체결 정보 브로드캐스팅 오류");
        }
    }
}