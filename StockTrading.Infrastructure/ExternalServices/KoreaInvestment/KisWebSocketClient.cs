using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StockTrading.Application.Services;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

/*
 * WebSocket 연결, 메시지 송수신, 기본 인증 등 저수준 통신
 */
public class KisWebSocketClient : IKisWebSocketClient, IDisposable
{
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly ILogger<KisWebSocketClient> _logger;
    private bool _isConnected;
    private const int BUFFER_SIZE = 4096;

    public event EventHandler<string> MessageReceived;
    public event EventHandler? ConnectionLost;

    public KisWebSocketClient(ILogger<KisWebSocketClient> logger)
    {
        _logger = logger;
    }

    public async Task ConnectAsync(string url)
    {
        if (_isConnected && _webSocket?.State == WebSocketState.Open) return;

        await CleanupAsync();
        _webSocket = new ClientWebSocket();
        _cancellationTokenSource = new CancellationTokenSource();
        await _webSocket.ConnectAsync(new Uri(url), _cancellationTokenSource.Token);
        _isConnected = true;

        _ = ReceiveMessagesAsync();
    }

    public async Task AuthenticateAsync(string token)
    {
        var message = new
        {
            header = new { approval_key = token, custtype = "P", tr_type = "1", content_type = "utf-8" },
            body = new { input = new { tr_id = "H0STCNT0", tr_key = "005930" } }
        };

        await SendMessageAsync(JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }

    public async Task SendMessageAsync(string message)
    {
        if (_webSocket?.State != WebSocketState.Open)
            throw new InvalidOperationException("WebSocket 연결되지 않음");

        var bytes = Encoding.UTF8.GetBytes(message);
        await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true,
            _cancellationTokenSource?.Token ?? CancellationToken.None);
    }


    private async Task ReceiveMessagesAsync()
    {
        var buffer = new byte[BUFFER_SIZE];

        try
        {
            while (_webSocket?.State == WebSocketState.Open &&
                   !(_cancellationTokenSource?.Token.IsCancellationRequested ?? true))
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer),
                    _cancellationTokenSource!.Token);

                if (result.MessageType == WebSocketMessageType.Close) break;
                if (result.MessageType != WebSocketMessageType.Text) continue;

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                MessageReceived?.Invoke(this, message);
            }
        }
        catch (OperationCanceledException)
        {
            /* 정상 종료 */
        }
        catch (WebSocketException)
        {
            _isConnected = false;
            ConnectionLost?.Invoke(this, EventArgs.Empty);
        }
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