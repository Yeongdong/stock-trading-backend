using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;
using StockTrading.Application.Services;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

public class WebSocketClient : IWebSocketClient, IDisposable
{
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly ILogger<WebSocketClient> _logger;
    private bool _isConnected;
    private const int BUFFER_SIZE = 4096;

    public event EventHandler<string> MessageReceived = delegate { };
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

        _logger.LogInformation("WebSocket 연결 성공");
        _ = Task.Run(ReceiveMessagesAsync, _cancellationTokenSource.Token);
    }

    public async Task SendMessageAsync(string message)
    {
        if (!IsConnectionValid())
            throw new InvalidOperationException("WebSocket 연결이 끊어졌습니다. 재연결이 필요합니다.");

        var bytes = Encoding.UTF8.GetBytes(message);
        await _webSocket!.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            _cancellationTokenSource?.Token ?? CancellationToken.None);
    }

    public async Task DisconnectAsync() => await CleanupAsync();

    private bool IsConnectionValid()
    {
        return _webSocket?.State == WebSocketState.Open &&
               !(_cancellationTokenSource?.Token.IsCancellationRequested ?? true);
    }

    private async Task ReceiveMessagesAsync()
    {
        var buffer = new byte[BUFFER_SIZE];
        var messageBuilder = new StringBuilder();

        while (_webSocket?.State == WebSocketState.Open &&
               !(_cancellationTokenSource?.Token.IsCancellationRequested ?? true))
        {
            var result = await _webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                _cancellationTokenSource!.Token);

            if (result.MessageType == WebSocketMessageType.Close)
                break;

            if (result.MessageType != WebSocketMessageType.Text) continue;
            messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

            if (!result.EndOfMessage) continue;
            MessageReceived?.Invoke(this, messageBuilder.ToString());
            messageBuilder.Clear();
        }

        _isConnected = false;
        ConnectionLost?.Invoke(this, EventArgs.Empty);
    }

    private async Task CleanupAsync()
    {
        _isConnected = false;
        _cancellationTokenSource?.Cancel();

        if (_webSocket?.State == WebSocketState.Open)
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);

        _webSocket?.Dispose();
        _cancellationTokenSource?.Dispose();
        _webSocket = null;
        _cancellationTokenSource = null;
    }

    public void Dispose() => DisconnectAsync().GetAwaiter().GetResult();
}