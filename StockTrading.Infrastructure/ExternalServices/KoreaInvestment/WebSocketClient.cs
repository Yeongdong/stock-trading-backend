using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;
using StockTrading.Application.Services;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

/*
 * WebSocket 연결, 메시지 송수신, 기본 인증 등 저수준 통신
 */
public class WebSocketClient : IWebSocketClient, IDisposable
{
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly ILogger<WebSocketClient> _logger;
    private bool _isConnected;
    private const int BUFFER_SIZE = 4096;

    public event EventHandler<string> MessageReceived;
    public event EventHandler? ConnectionLost;

    public WebSocketClient(ILogger<WebSocketClient> logger)
    {
        _logger = logger;
    }

    public async Task ConnectAsync(string url)
    {
        if (_isConnected && _webSocket?.State == WebSocketState.Open) return;

        await CleanupAsync();

        _webSocket = new ClientWebSocket();
        _cancellationTokenSource = new CancellationTokenSource();
        _webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
        await _webSocket.ConnectAsync(new Uri(url), _cancellationTokenSource.Token);
        _isConnected = true;

        _ = Task.Run(ReceiveMessagesAsync, _cancellationTokenSource.Token);
    }

    public async Task SendMessageAsync(string message)
    {
        // 연결 상태가 유효하지 않으면 재연결 시도
        if (!IsConnectionValid())
            throw new InvalidOperationException("WebSocket 연결이 끊어졌습니다. 재연결이 필요합니다.");

        var bytes = Encoding.UTF8.GetBytes(message);
        await _webSocket!.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            _cancellationTokenSource?.Token ?? CancellationToken.None);
    }

    private bool IsConnectionValid()
    {
        var isValid = _webSocket?.State == WebSocketState.Open;

        if (!isValid)
            _isConnected = false;

        return isValid;
    }

    private async Task ReceiveMessagesAsync()
    {
        var buffer = new byte[BUFFER_SIZE];

        while (_webSocket?.State == WebSocketState.Open &&
               !(_cancellationTokenSource?.Token.IsCancellationRequested ?? true))
        {
            var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer),
                _cancellationTokenSource!.Token);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                _logger.LogInformation("서버에서 연결 종료");
                break;
            }

            if (result.MessageType != WebSocketMessageType.Text) continue;

            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            MessageReceived?.Invoke(this, message);
        }

        _isConnected = false;
        _logger.LogWarning("WebSocket 수신 루프 종료. 연결 상태: {State}", _webSocket?.State);
        ConnectionLost?.Invoke(this, EventArgs.Empty);
    }

    public async Task DisconnectAsync() => await CleanupAsync();

    private async Task CleanupAsync()
    {
        _isConnected = false;
        if (_cancellationTokenSource is not null)
            await _cancellationTokenSource.CancelAsync();

        if (_webSocket?.State == WebSocketState.Open)
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);

        _webSocket?.Dispose();
        _cancellationTokenSource?.Dispose();
        _webSocket = null;
        _cancellationTokenSource = null;
    }

    public void Dispose() => DisconnectAsync().GetAwaiter().GetResult();
}