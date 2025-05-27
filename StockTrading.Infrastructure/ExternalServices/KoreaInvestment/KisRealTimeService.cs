using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.External.KoreaInvestment;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.DTOs.Users;
using StockTrading.Application.Services;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

public class KisRealTimeService : IKisRealTimeService
{
    private readonly IKisWebSocketClient _webSocketClient;
    private readonly IKisRealTimeDataProcessor _dataProcessor;
    private readonly IKisSubscriptionManager _subscriptionManager;
    private readonly IRealTimeDataBroadcaster _broadcaster;
    private readonly ILogger<KisRealTimeService> _logger;

    public KisRealTimeService(
        IKisWebSocketClient webSocketClient,
        IKisRealTimeDataProcessor dataProcessor,
        IKisSubscriptionManager subscriptionManager,
        IRealTimeDataBroadcaster broadcaster,
        ILogger<KisRealTimeService> logger)
    {
        _webSocketClient = webSocketClient;
        _dataProcessor = dataProcessor;
        _subscriptionManager = subscriptionManager;
        _broadcaster = broadcaster;
        _logger = logger;

        // 이벤트 연결
        _webSocketClient.MessageReceived += OnWebSocketMessageReceived;
        _dataProcessor.StockPriceReceived += OnStockPriceReceived;
        _dataProcessor.TradeExecutionReceived += OnTradeExecutionReceived;
    }

    private void OnWebSocketMessageReceived(object sender, string message)
    {
        _dataProcessor.ProcessMessage(message);
    }

    private void OnStockPriceReceived(object sender, KisTransactionInfo data)
    {
        // fire-and-forget 처리
        _ = Task.Run(async () => { await _broadcaster.BroadcastStockPriceAsync(data); });
    }

    private async void OnTradeExecutionReceived(object sender, object data)
    {
        _ = Task.Run(async () => { await _broadcaster.BroadcastTradeExecutionAsync(data); });
    }

    public async Task StartAsync()
    {
        await _webSocketClient.ConnectAsync("ws://ops.koreainvestment.com:31000");
        _logger.LogInformation("실시간 서비스 시작");
    }

    public async Task StartAsync(UserInfo user)
    {
        await StartAsync();
        await _webSocketClient.AuthenticateAsync(user.WebSocketToken);
        _logger.LogInformation($"사용자 {user.Id} 인증 완료");
    }

    public async Task SubscribeSymbolAsync(string symbol)
    {
        await _subscriptionManager.SubscribeSymbolAsync(symbol);
    }

    public async Task UnsubscribeSymbolAsync(string symbol)
    {
        await _subscriptionManager.UnsubscribeSymbolAsync(symbol);
    }

    public IReadOnlyCollection<string> GetSubscribedSymbols()
    {
        return _subscriptionManager.GetSubscribedSymbols();
    }

    public async Task StopAsync()
    {
        await _subscriptionManager.UnsubscribeAllAsync();
        await _webSocketClient.DisconnectAsync();
        _logger.LogInformation("실시간 서비스 종료");
    }
}