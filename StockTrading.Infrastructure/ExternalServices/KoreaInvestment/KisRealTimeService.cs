using Microsoft.Extensions.Logging;
using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.Services.Interfaces;
using StockTrading.Infrastructure.ExternalServices.Interfaces;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Models;

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

    private async void OnStockPriceReceived(object sender, StockTransaction data)
    {
        await _broadcaster.BroadcastStockPriceAsync(data);
    }

    private async void OnTradeExecutionReceived(object sender, object data)
    {
        await _broadcaster.BroadcastTradeExecutionAsync(data);
    }

    public async Task StartAsync()
    {
        try
        {
            await _webSocketClient.ConnectAsync("ws://ops.koreainvestment.com:31000");
            _logger.LogInformation("실시간 서비스 시작");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "실시간 서비스 시작 실패");
            throw;
        }
    }

    public async Task StartAsync(UserDto user)
    {
        try
        {
            await StartAsync();
            await _webSocketClient.AuthenticateAsync(user.WebSocketToken);
            _logger.LogInformation($"사용자 {user.Id} 인증 완료");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"사용자 {user.Id} 인증 실패");
            await StopAsync();
            throw;
        }
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
        try
        {
            await _subscriptionManager.UnsubscribeAllAsync();
            await _webSocketClient.DisconnectAsync();
            _logger.LogInformation("실시간 서비스 종료");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "실시간 서비스 종료 실패");
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            // 이벤트 구독 해제
            _webSocketClient.MessageReceived -= OnWebSocketMessageReceived;
            _dataProcessor.StockPriceReceived -= OnStockPriceReceived;
            _dataProcessor.TradeExecutionReceived -= OnTradeExecutionReceived;

            // 리소스 해제
            _webSocketClient.Dispose();

            _logger.LogInformation("실시간 서비스 리소스 해제");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "실시간 서비스 리소스 해제 중 오류 발생");
        }
    }
}