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
    private readonly ClientWebSocket _webSocket;
    private CancellationTokenSource _cancellationTokenSource;
    private readonly ILogger<KisWebSocketClient> _logger;
    private bool _isConnected;
    private const int BUFFER_SIZE = 4096;

    // 이벤트를 통해 메시지 수신 알림
    public event EventHandler<string> MessageReceived;

    public KisWebSocketClient(ILogger<KisWebSocketClient> logger)
    {
        _webSocket = new ClientWebSocket();
        _logger = logger;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public async Task ConnectAsync(string url)
    {
        if (_isConnected) return;

        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            await _webSocket.ConnectAsync(new Uri(url), _cancellationTokenSource.Token);
            _isConnected = true;
            _logger.LogInformation("WebSocket 연결 성공");

            // 연결 후 메시지 수신 시작
            _ = ReceiveMessagesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket 연결 실패");
            _isConnected = false;
            throw;
        }
    }

    public async Task AuthenticateAsync(string token)
    {
        if (!_isConnected)
            throw new InvalidOperationException("WebSocket이 연결되어 있지 않음");

        if (string.IsNullOrEmpty(token))
            throw new ArgumentException("인증 토큰 필요");

        try
        {
            // 인증 메시지 구성 및 전송
            var authMessage = new
            {
                header = new
                {
                    approval_key = token,
                    custtype = "P",
                    tr_type = "1",
                    content_type = "utf-8"
                },
                body = new { }
            };

            await SendMessageAsync(JsonSerializer.Serialize(authMessage));
            _logger.LogInformation("인증 메시지 전송 완료");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "인증 실패");
            throw;
        }
    }

    public async Task SendMessageAsync(string message)
    {
        if (!_isConnected)
            throw new InvalidOperationException("WebSocket이 연결되어 있지 않음");

        try
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                _cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "메시지 전송 실패");
            throw;
        }
    }

    private async Task ReceiveMessagesAsync()
    {
        var buffer = new byte[BUFFER_SIZE];

        try
        {
            while (_webSocket.State == WebSocketState.Open && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                WebSocketReceiveResult result;
                using var ms = new MemoryStream();

                do
                {
                    result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Closing",
                            _cancellationTokenSource.Token);
                        _isConnected = false;
                        break;
                    }

                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Text && ms.Length > 0)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    using var reader = new StreamReader(ms, Encoding.UTF8);
                    var messageJson = await reader.ReadToEndAsync();

                    // 이벤트를 통해 메시지 전달
                    OnMessageReceived(messageJson);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket 수신 중 오류 발생");
            _isConnected = false;
        }
    }

    protected virtual void OnMessageReceived(string message)
    {
        MessageReceived?.Invoke(this, message);
    }

    public async Task DisconnectAsync()
    {
        if (!_isConnected) return;

        try
        {
            _cancellationTokenSource.Cancel();

            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Closing",
                    CancellationToken.None);
            }

            _isConnected = false;
            _logger.LogInformation("WebSocket 연결 종료");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket 종료 중 오류 발생");
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            DisconnectAsync().GetAwaiter().GetResult();
            _cancellationTokenSource?.Dispose();
            _webSocket.Dispose();

            _logger.LogInformation("WebSocket 리소스 해제");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket 리소스 해제 중 오류 발생");
        }
    }
}