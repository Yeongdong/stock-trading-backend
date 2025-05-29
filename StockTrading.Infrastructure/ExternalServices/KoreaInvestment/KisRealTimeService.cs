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

    private bool _isStarted;
    private UserInfo? _currentUser;

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
        _webSocketClient.MessageReceived += (_, msg) => _dataProcessor.ProcessMessage(msg);
        _webSocketClient.ConnectionLost += OnConnectionLost;
        _dataProcessor.StockPriceReceived += (_, data) => _ = _broadcaster.BroadcastStockPriceAsync(data);
        _dataProcessor.TradeExecutionReceived += (_, data) => _ = _broadcaster.BroadcastTradeExecutionAsync(data);
    }

    public async Task StartAsync(UserInfo user)
    {
        if (_isStarted) return;

        _currentUser = user;
        await _webSocketClient.ConnectAsync("ws://ops.koreainvestment.com:31000");
        await _webSocketClient.AuthenticateAsync(user.WebSocketToken!);
        _isStarted = true;
    }

    public async Task SubscribeSymbolAsync(string symbol)
    {
        if (!_isStarted) throw new InvalidOperationException("서비스를 먼저 시작하세요");
        await _subscriptionManager.SubscribeSymbolAsync(symbol);
    }

    public async Task UnsubscribeSymbolAsync(string symbol)
    {
        if (_isStarted) await _subscriptionManager.UnsubscribeSymbolAsync(symbol);
    }

    public IReadOnlyCollection<string> GetSubscribedSymbols() =>
        _subscriptionManager.GetSubscribedSymbols();

    public async Task StopAsync()
    {
        if (!_isStarted) return;

        _isStarted = false;
        await _subscriptionManager.UnsubscribeAllAsync();
        await _webSocketClient.DisconnectAsync();
    }

    private async void OnConnectionLost(object? sender, EventArgs e)
    {
        if (_currentUser == null) return;

        _logger.LogWarning("재연결 시도");
        _isStarted = false;

        try
        {
            await Task.Delay(3000);
            await StartAsync(_currentUser);

            foreach (var symbol in _subscriptionManager.GetSubscribedSymbols())
            {
                await _subscriptionManager.SubscribeSymbolAsync(symbol);
                await Task.Delay(500);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "재연결 실패");
        }
    }
}