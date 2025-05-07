using System.Text.Json;
using Microsoft.Extensions.Logging;
using StockTrading.Infrastructure.ExternalServices.Interfaces;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

/*
 * 종목 구독 관리
 */
public class KisSubscriptionManager : IKisSubscriptionManager
{
    private readonly IKisWebSocketClient _webSocketClient;
    private readonly ILogger<KisSubscriptionManager> _logger;
    private readonly Dictionary<string, bool> _subscribedSymbols = new();

    public KisSubscriptionManager(IKisWebSocketClient webSocketClient, ILogger<KisSubscriptionManager> logger)
    {
        _webSocketClient = webSocketClient;
        _logger = logger;
    }

    public async Task SubscribeSymbolAsync(string symbol)
    {
        if (_subscribedSymbols.ContainsKey(symbol))
        {
            _logger.LogInformation($"이미 구독 중인 종목 {symbol}");
            return;
        }

        try
        {
            // 종목 구독 메시지
            var subscribeMessage = new
            {
                header = new
                {
                    tr_type = "1", // 1: 등록
                    tr_id = "H0STASP0", // 실시간 주식 호가
                    tr_key = symbol // 종목코드
                },
                body = new { }
            };

            await _webSocketClient.SendMessageAsync(JsonSerializer.Serialize(subscribeMessage));

            _subscribedSymbols[symbol] = true;
            _logger.LogInformation($"종목 구독 완료: {symbol}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"종목 구독 실패: {symbol}");
            throw;
        }
    }

    public async Task UnsubscribeSymbolAsync(string symbol)
    {
        if (!_subscribedSymbols.ContainsKey(symbol))
        {
            _logger.LogInformation($"구독 중이 아닌 종목: {symbol}");
            return;
        }

        try
        {
            // 종목 구독 해제 메시지
            var unsubscribeMessage = new
            {
                header = new
                {
                    tr_type = "2", // 2: 해제
                    tr_id = "H0STASP0", // 실시간 주식 호가
                    tr_key = symbol // 종목코드
                },
                body = new { }
            };

            await _webSocketClient.SendMessageAsync(JsonSerializer.Serialize(unsubscribeMessage));

            _subscribedSymbols.Remove(symbol);
            _logger.LogInformation($"종목 구독 해제 완료: {symbol}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"종목 구독 해제 실패: {symbol}");
            throw;
        }
    }

    public async Task UnsubscribeAllAsync()
    {
        foreach (var symbol in _subscribedSymbols.Keys.ToList())
        {
            await UnsubscribeSymbolAsync(symbol);
        }

        _logger.LogInformation("모든 종목 구독 해제 완료");
    }

    public IReadOnlyCollection<string> GetSubscribedSymbols()
    {
        return _subscribedSymbols.Keys.ToList().AsReadOnly();
    }
}