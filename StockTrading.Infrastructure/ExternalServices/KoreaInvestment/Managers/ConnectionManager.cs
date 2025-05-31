using Microsoft.Extensions.Logging;
using StockTrading.Application.Services;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Managers;

/// <summary>
/// WebSocket 연결 관리자
/// </summary>
public class ConnectionManager
{
    private readonly IWebSocketClient _webSocketClient;
    private readonly ILogger<ConnectionManager> _logger;

    private const string WebSocketUrl = "ws://ops.koreainvestment.com:31000";
    private const int ReconnectDelayMs = 3000;
    private const int SubscriptionDelayMs = 500;

    public ConnectionManager(IWebSocketClient webSocketClient, ILogger<ConnectionManager> logger)
    {
        _webSocketClient = webSocketClient;
        _logger = logger;
    }

    /// <summary>
    /// WebSocket 연결
    /// </summary>
    public async Task ConnectAsync()
    {
        await _webSocketClient.ConnectAsync(WebSocketUrl);
        _logger.LogInformation("WebSocket 연결 성공: {Url}", WebSocketUrl);
    }

    /// <summary>
    /// WebSocket 연결 해제
    /// </summary>
    public async Task DisconnectAsync()
    {
        await _webSocketClient.DisconnectAsync();
        _logger.LogInformation("WebSocket 연결 해제 완료");
    }

    /// <summary>
    /// 재연결 (지연 시간 포함)
    /// </summary>
    public async Task ReconnectAsync()
    {
        _logger.LogInformation("WebSocket 재연결 시작");

        await Task.Delay(ReconnectDelayMs);
        await ConnectAsync();

        _logger.LogInformation("WebSocket 재연결 완료");
    }

    /// <summary>
    /// 구독 목록 재등록 (재연결 후)
    /// </summary>
    public async Task ResubscribeAsync(IEnumerable<string> symbols, Func<string, Task> subscribeAction)
    {
        _logger.LogInformation("기존 구독 종목 재등록 시작: {Count}개", symbols.Count());

        foreach (var symbol in symbols)
        {
            await subscribeAction(symbol);
            await Task.Delay(SubscriptionDelayMs);
            _logger.LogDebug("종목 재구독 완료: {Symbol}", symbol);
        }

        _logger.LogInformation("기존 구독 종목 재등록 완료");
    }
}