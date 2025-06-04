using System.Text.Json;
using Microsoft.Extensions.Logging;
using StockTrading.Application.Services;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

public class SubscriptionManager : ISubscriptionManager
{
    private readonly IWebSocketClient _webSocketClient;
    private readonly ILogger<SubscriptionManager> _logger;
    private readonly Dictionary<string, bool> _subscribedSymbols = new();
    private string _webSocketToken = string.Empty;

    public SubscriptionManager(IWebSocketClient webSocketClient, ILogger<SubscriptionManager> logger)
    {
        _webSocketClient = webSocketClient;
        _logger = logger;
    }

    public async Task SubscribeSymbolAsync(string symbol)
    {
        if (_subscribedSymbols.ContainsKey(symbol))
            return;

        if (string.IsNullOrEmpty(_webSocketToken))
            throw new InvalidOperationException("WebSocket 토큰이 설정되지 않았습니다.");

        _logger.LogInformation("종목 구독: {Symbol}", symbol);

        var subscriptionMessage = new
        {
            header = new
            {
                approval_key = _webSocketToken,
                custtype = "P",
                tr_type = "1",
                content_type = "utf-8"
            },
            body = new
            {
                input = new
                {
                    tr_id = "H0STCNT0",
                    tr_key = symbol
                }
            }
        };

        var jsonMessage = JsonSerializer.Serialize(subscriptionMessage, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        await _webSocketClient.SendMessageAsync(jsonMessage);
        await Task.Delay(1000);

        _subscribedSymbols[symbol] = true;
        await SubscribeAskBidAsync(symbol);
    }

    public async Task UnsubscribeSymbolAsync(string symbol)
    {
        if (!_subscribedSymbols.ContainsKey(symbol))
            return;

        _logger.LogInformation("종목 구독 해제: {Symbol}", symbol);

        var unsubscriptionMessage = new
        {
            header = new
            {
                approval_key = _webSocketToken,
                custtype = "P",
                tr_type = "2",
                content_type = "utf-8"
            },
            body = new
            {
                input = new
                {
                    tr_id = "H0STASP0",
                    tr_key = symbol
                }
            }
        };

        var jsonMessage = JsonSerializer.Serialize(unsubscriptionMessage, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        await _webSocketClient.SendMessageAsync(jsonMessage);
        _subscribedSymbols.Remove(symbol);
    }

    public async Task UnsubscribeAllAsync()
    {
        if (_subscribedSymbols.Count == 0)
            return;

        _logger.LogInformation("전체 구독 해제: {Count}개 종목", _subscribedSymbols.Count);

        var symbolsToUnsubscribe = _subscribedSymbols.Keys.ToList();

        foreach (var symbol in symbolsToUnsubscribe)
        {
            await UnsubscribeSymbolAsync(symbol);
            await Task.Delay(100);
        }
    }

    public IReadOnlyCollection<string> GetSubscribedSymbols()
    {
        return _subscribedSymbols.Keys.ToList().AsReadOnly();
    }

    public void SetWebSocketToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            throw new ArgumentException("WebSocket 토큰은 필수입니다.", nameof(token));

        _webSocketToken = token;
        _logger.LogInformation("WebSocket 토큰 설정 완료");
    }

    private async Task SubscribeAskBidAsync(string symbol)
    {
        var askBidMessage = new
        {
            header = new
            {
                approval_key = _webSocketToken,
                custtype = "P",
                tr_type = "1",
                content_type = "utf-8"
            },
            body = new
            {
                input = new
                {
                    tr_id = "H0STASP0",
                    tr_key = symbol
                }
            }
        };

        var jsonMessage = JsonSerializer.Serialize(askBidMessage, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        await _webSocketClient.SendMessageAsync(jsonMessage);
        await Task.Delay(500);
    }
}