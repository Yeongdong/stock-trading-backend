using System.Text.Json;
using Microsoft.Extensions.Logging;
using StockTrading.Application.Features.Market.Services;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.RealTime;

public class SubscriptionManager : ISubscriptionManager
{
    private readonly IWebSocketClient _webSocketClient;
    private readonly ILogger<SubscriptionManager> _logger;
    private readonly Dictionary<string, bool> _subscribedSymbols = new();
    private string _webSocketToken = string.Empty;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public SubscriptionManager(IWebSocketClient webSocketClient, ILogger<SubscriptionManager> logger)
    {
        _webSocketClient = webSocketClient;
        _logger = logger;
    }

    public async Task SubscribeSymbolAsync(string symbol)
    {
        if (_subscribedSymbols.ContainsKey(symbol))
            return;

        EnsureTokenSet();

        _logger.LogInformation("종목 구독: {Symbol}", symbol);

        await SendMessageWithDelayAsync(BuildMessage("1", "H0STCNT0", symbol));
        _subscribedSymbols[symbol] = true;

        await SendMessageWithDelayAsync(BuildMessage("1", "H0STASP0", symbol));
    }

    public async Task UnsubscribeSymbolAsync(string symbol)
    {
        if (!_subscribedSymbols.ContainsKey(symbol))
            return;

        _logger.LogInformation("종목 구독 해제: {Symbol}", symbol);

        await _webSocketClient.SendMessageAsync(BuildMessage("2", "H0STCNT0", symbol));
        await _webSocketClient.SendMessageAsync(BuildMessage("2", "H0STASP0", symbol));

        _subscribedSymbols.Remove(symbol);
        _logger.LogInformation("종목 구독 해제 완료: {Symbol}", symbol);
    }

    public async Task UnsubscribeAllAsync()
    {
        if (_subscribedSymbols.Count == 0)
            return;

        _logger.LogInformation("전체 구독 해제: {Count}개 종목", _subscribedSymbols.Count);

        foreach (var symbol in _subscribedSymbols.Keys.ToList())
        {
            await UnsubscribeSymbolAsync(symbol);
        }
    }

    public IReadOnlyCollection<string> GetSubscribedSymbols()
        => _subscribedSymbols.Keys.ToList().AsReadOnly();

    public void SetWebSocketToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            throw new ArgumentException("WebSocket 토큰은 필수입니다.", nameof(token));

        _webSocketToken = token;
        _logger.LogInformation("WebSocket 토큰 설정 완료");
    }

    private void EnsureTokenSet()
    {
        if (string.IsNullOrEmpty(_webSocketToken))
            throw new InvalidOperationException("WebSocket 토큰이 설정되지 않았습니다.");
    }

    private string BuildMessage(string trType, string trId, string symbol)
    {
        var message = new
        {
            header = new
            {
                approval_key = _webSocketToken,
                custtype = "P",
                tr_type = trType,
                content_type = "utf-8"
            },
            body = new
            {
                input = new
                {
                    tr_id = trId,
                    tr_key = symbol
                }
            }
        };

        return JsonSerializer.Serialize(message, JsonOptions);
    }

    private async Task SendMessageWithDelayAsync(string message)
    {
        await _webSocketClient.SendMessageAsync(message);
    }
}
