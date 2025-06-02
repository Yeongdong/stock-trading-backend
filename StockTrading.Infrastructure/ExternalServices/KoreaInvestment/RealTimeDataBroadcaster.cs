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
        try
        {
            _logger.LogInformation("📡 [Broadcaster] 주가 데이터 브로드캐스트 시작: {Symbol} - {Price}원", 
                priceData.Symbol, priceData.Price);

            // 연결된 클라이언트 수 확인 (가능한 경우)
            var connectionCount = "알 수 없음"; // SignalR에서 직접 가져올 수 있는 방법이 제한적
            
            _logger.LogInformation("📊 [Broadcaster] 브로드캐스트 데이터: Symbol={Symbol}, Price={Price}, Change={Change}, ChangeType={ChangeType}, Volume={Volume}", 
                priceData.Symbol, priceData.Price, priceData.PriceChange, priceData.ChangeType, priceData.Volume);

            // 실제 브로드캐스트 실행
            await _hubContext.Clients.All.SendAsync("ReceiveStockPrice", priceData);
            
            _logger.LogInformation("✅ [Broadcaster] 주가 데이터 브로드캐스트 완료: {Symbol} - {Price}원", 
                priceData.Symbol, priceData.Price);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Broadcaster] 주가 데이터 브로드캐스트 실패: {Symbol} - {Error}", 
                priceData.Symbol, ex.Message);
            throw;
        }
    }

    public async Task BroadcastTradeExecutionAsync(object executionData)
    {
        try
        {
            _logger.LogInformation("📡 [Broadcaster] 거래 체결 데이터 브로드캐스트 시작");
            
            await _hubContext.Clients.All.SendAsync("ReceiveTradeExecution", executionData);
            
            _logger.LogInformation("✅ [Broadcaster] 거래 체결 데이터 브로드캐스트 완료");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Broadcaster] 거래 체결 데이터 브로드캐스트 실패: {Error}", ex.Message);
            throw;
        }
    }
}