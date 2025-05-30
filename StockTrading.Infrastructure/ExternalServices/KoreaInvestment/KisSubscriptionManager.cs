using System.Text.Json;
using Microsoft.Extensions.Logging;
using StockTrading.Application.Services;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

/*
 * 종목 구독 관리
 */
public class KisSubscriptionManager : IKisSubscriptionManager
{
    private readonly IKisWebSocketClient _webSocketClient;
    private readonly ILogger<KisSubscriptionManager> _logger;
    private readonly Dictionary<string, bool> _subscribedSymbols = new();
    private string _webSocketToken;

    public KisSubscriptionManager(IKisWebSocketClient webSocketClient, ILogger<KisSubscriptionManager> logger)
    {
        _webSocketClient = webSocketClient;
        _logger = logger;
    }

    public void SetWebSocketToken(string token)
    {
        _webSocketToken = token;
    }

    public async Task SubscribeSymbolAsync(string symbol)
    {
        if (_subscribedSymbols.ContainsKey(symbol)) return;

        var message = new
        {
            header = new { approval_key = _webSocketToken, custtype = "P", tr_type = "1", content_type = "utf-8" },
            body = new { input = new { tr_id = "H0STASP0", tr_key = symbol } }
        };

        var jsonMessage = JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await _webSocketClient.SendMessageAsync(jsonMessage);

        _subscribedSymbols[symbol] = true;
    }

    public async Task UnsubscribeSymbolAsync(string symbol)
    {
        if (!_subscribedSymbols.ContainsKey(symbol)) return;

        var message = new
        {
            header = new { approval_key = _webSocketToken, custtype = "P", tr_type = "2", content_type = "utf-8" },
            body = new { input = new { tr_id = "H0STASP0", tr_key = symbol } }
        };

        await _webSocketClient.SendMessageAsync(JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));

        _subscribedSymbols.Remove(symbol);
    }

    public async Task UnsubscribeAllAsync()
    {
        foreach (var symbol in _subscribedSymbols.Keys.ToList())
        {
            await UnsubscribeSymbolAsync(symbol);
        }
    }

    public IReadOnlyCollection<string> GetSubscribedSymbols() =>
        _subscribedSymbols.Keys.ToList().AsReadOnly();
}