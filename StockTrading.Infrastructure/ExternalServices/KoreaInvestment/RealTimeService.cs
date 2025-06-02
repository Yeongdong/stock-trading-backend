using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.Users;
using StockTrading.Application.Services;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Managers;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.State;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

/// <summary>
/// KIS 실시간 서비스 (리팩토링 버전)
/// </summary>
public class RealTimeService : IRealTimeService
{
    private readonly IWebSocketClient _webSocketClient;
    private readonly IRealTimeDataProcessor _dataProcessor;
    private readonly ISubscriptionManager _subscriptionManager;
    private readonly IRealTimeDataBroadcaster _broadcaster;
    private readonly ILogger<RealTimeService> _logger;

    private readonly ConnectionManager _connectionManager;
    private readonly RetryManager _retryManager;
    private readonly ServiceState _serviceState;

    public RealTimeService(IWebSocketClient webSocketClient, IRealTimeDataProcessor dataProcessor,
        ISubscriptionManager subscriptionManager, IRealTimeDataBroadcaster broadcaster,
        ILogger<RealTimeService> logger, ILoggerFactory loggerFactory)
    {
        _webSocketClient = webSocketClient;
        _dataProcessor = dataProcessor;
        _subscriptionManager = subscriptionManager;
        _broadcaster = broadcaster;
        _logger = logger;

        _connectionManager = new ConnectionManager(_webSocketClient, loggerFactory.CreateLogger<ConnectionManager>());
        _retryManager = new RetryManager(loggerFactory.CreateLogger<RetryManager>());
        _serviceState = new ServiceState();

        SetupEventHandlers();
    }

    public async Task StartAsync(UserInfo user)
    {
        if (_serviceState.IsStarted)
        {
            _logger.LogInformation("서비스가 이미 시작됨. 사용자: {UserId}", user.Id);
            return;
        }

        _logger.LogInformation("실시간 서비스 시작. 사용자: {UserId}, WebSocketToken: {HasToken}",
            user.Id, !string.IsNullOrEmpty(user.WebSocketToken));

        await _connectionManager.ConnectAsync();
        _subscriptionManager.SetWebSocketToken(user.WebSocketToken);
        _serviceState.Start(user);

        _logger.LogInformation("실시간 서비스 시작 완료. 사용자: {UserId}", user.Id);
    }

    public async Task SubscribeSymbolAsync(string symbol)
    {
        _serviceState.EnsureStarted();

        await _retryManager.ExecuteWithRetryAsync(
            operation: () => _subscriptionManager.SubscribeSymbolAsync(symbol),
            shouldRetry: RetryManager.IsConnectionException,
            onRetry: HandleConnectionLostAsync,
            operationName: $"종목 구독({symbol})");
    }

    public async Task UnsubscribeSymbolAsync(string symbol)
    {
        if (!_serviceState.IsStarted)
        {
            _logger.LogDebug("서비스가 중지된 상태에서 구독 해제 요청: {Symbol}", symbol);
            return;
        }

        try
        {
            await _subscriptionManager.UnsubscribeSymbolAsync(symbol);
        }
        catch (Exception ex) when (RetryManager.IsConnectionException(ex))
        {
            _logger.LogWarning("구독 해제 중 연결 끊어짐: {Symbol}", symbol);
        }
    }

    public IReadOnlyCollection<string> GetSubscribedSymbols() =>
        _subscriptionManager.GetSubscribedSymbols();

    public async Task StopAsync()
    {
        if (!_serviceState.IsStarted)
        {
            _logger.LogDebug("서비스가 이미 중지됨");
            return;
        }

        _logger.LogInformation("실시간 서비스 중지 시작");

        await _subscriptionManager.UnsubscribeAllAsync();
        await _connectionManager.DisconnectAsync();
        _serviceState.Stop();

        _logger.LogInformation("실시간 서비스 중지 완료");
    }

    private void SetupEventHandlers()
    {
        _webSocketClient.MessageReceived += (_, msg) => _dataProcessor.ProcessMessage(msg);
        _webSocketClient.ConnectionLost += OnConnectionLostEvent;
        // _dataProcessor.StockPriceReceived += (_, data) => _ = _broadcaster.BroadcastStockPriceAsync(data);
        // _dataProcessor.TradeExecutionReceived += (_, data) => _ = _broadcaster.BroadcastTradeExecutionAsync(data);
    }

    private async void OnConnectionLostEvent(object? sender, EventArgs e)
    {
        if (!_serviceState.IsStarted)
        {
            _logger.LogDebug("서비스 중지 상태에서 연결 끊어짐 이벤트 무시");
            return;
        }

        await HandleConnectionLostAsync();
    }

    private async Task HandleConnectionLostAsync()
    {
        _serviceState.EnsureUserExists();

        _logger.LogWarning("WebSocket 연결 끊어짐. 재연결 및 재구독 시작");

        await _connectionManager.ReconnectAsync();

        var subscribedSymbols = _subscriptionManager.GetSubscribedSymbols();
        await _connectionManager.ResubscribeAsync(
            symbols: subscribedSymbols,
            subscribeAction: _subscriptionManager.SubscribeSymbolAsync);

        _logger.LogInformation("재연결 및 재구독 완료");
    }
}