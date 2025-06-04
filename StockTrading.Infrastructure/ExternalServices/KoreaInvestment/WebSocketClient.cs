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
        if (_isConnected && _webSocket?.State == WebSocketState.Open) 
        {
            _logger.LogInformation("🔗 [WebSocket] 이미 연결된 상태입니다.");
            return;
        }

        await CleanupAsync();

        _logger.LogInformation("🔄 [WebSocket] 연결 시작: {Url}", url);
        
        _webSocket = new ClientWebSocket();
        _cancellationTokenSource = new CancellationTokenSource();
        _webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
        
        try
        {
            await _webSocket.ConnectAsync(new Uri(url), _cancellationTokenSource.Token);
            _isConnected = true;
            
            _logger.LogInformation("✅ [WebSocket] 연결 성공: {State}", _webSocket.State);

            // 메시지 수신 루프 시작
            _ = Task.Run(ReceiveMessagesAsync, _cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [WebSocket] 연결 실패: {Error}", ex.Message);
            throw;
        }
    }

    public async Task SendMessageAsync(string message)
    {
        if (!IsConnectionValid())
        {
            _logger.LogError("❌ [WebSocket] 연결이 유효하지 않음. 메시지 전송 실패");
            throw new InvalidOperationException("WebSocket 연결이 끊어졌습니다. 재연결이 필요합니다.");
        }

        _logger.LogDebug("📤 [WebSocket] 메시지 전송: {Message}", message);

        var bytes = Encoding.UTF8.GetBytes(message);
        await _webSocket!.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            _cancellationTokenSource?.Token ?? CancellationToken.None);
            
        _logger.LogDebug("✅ [WebSocket] 메시지 전송 완료");
    }

    private bool IsConnectionValid()
    {
        var isValid = _webSocket?.State == WebSocketState.Open && !(_cancellationTokenSource?.Token.IsCancellationRequested ?? true);

        if (!isValid)
        {
            _isConnected = false;
            _logger.LogWarning("⚠️ [WebSocket] 연결 상태 무효: State={State}, Cancelled={Cancelled}", 
                _webSocket?.State, _cancellationTokenSource?.Token.IsCancellationRequested);
        }

        return isValid;
    }

    private async Task ReceiveMessagesAsync()
    {
        var buffer = new byte[BUFFER_SIZE];
        var messageBuilder = new StringBuilder();

        _logger.LogInformation("🎧 [WebSocket] 메시지 수신 루프 시작");

        try
        {
            while (_webSocket?.State == WebSocketState.Open && 
                   !(_cancellationTokenSource?.Token.IsCancellationRequested ?? true))
            {
                try
                {
                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        _cancellationTokenSource!.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation("🚪 [WebSocket] 서버에서 연결 종료 요청");
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var messageChunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        messageBuilder.Append(messageChunk);

                        // 메시지가 완료되면 처리
                        if (result.EndOfMessage)
                        {
                            var completeMessage = messageBuilder.ToString();
                            messageBuilder.Clear();

                            _logger.LogDebug("📥 [WebSocket] 메시지 수신: {Message}", 
                                completeMessage.Length > 200 ? completeMessage.Substring(0, 200) + "..." : completeMessage);

                            // 이벤트 발생 - 이 부분이 핵심!
                            try
                            {
                                MessageReceived?.Invoke(this, completeMessage);
                                _logger.LogDebug("✅ [WebSocket] MessageReceived 이벤트 발생 완료");
                            }
                            catch (Exception eventEx)
                            {
                                _logger.LogError(eventEx, "❌ [WebSocket] MessageReceived 이벤트 처리 중 오류");
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("🔇 [WebSocket] 수신 작업 취소됨");
                    break;
                }
                catch (WebSocketException wsEx)
                {
                    _logger.LogError(wsEx, "❌ [WebSocket] WebSocket 오류: {Error}", wsEx.Message);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [WebSocket] 수신 루프에서 예외 발생");
        }
        finally
        {
            _isConnected = false;
            _logger.LogWarning("🔇 [WebSocket] 수신 루프 종료. 연결 상태: {State}", _webSocket?.State);
            
            // ConnectionLost 이벤트 발생
            try
            {
                ConnectionLost?.Invoke(this, EventArgs.Empty);
                _logger.LogDebug("✅ [WebSocket] ConnectionLost 이벤트 발생 완료");
            }
            catch (Exception eventEx)
            {
                _logger.LogError(eventEx, "❌ [WebSocket] ConnectionLost 이벤트 처리 중 오류");
            }
        }
    }

    public async Task DisconnectAsync() => await CleanupAsync();

    private async Task CleanupAsync()
    {
        _logger.LogDebug("🧹 [WebSocket] 정리 작업 시작");
        
        _isConnected = false;
        
        if (_cancellationTokenSource != null)
        {
            await _cancellationTokenSource.CancelAsync();
        }

        if (_webSocket?.State == WebSocketState.Open)
        {
            try
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ [WebSocket] 연결 종료 중 오류 (무시됨)");
            }
        }

        _webSocket?.Dispose();
        _cancellationTokenSource?.Dispose();
        _webSocket = null;
        _cancellationTokenSource = null;
        
        _logger.LogDebug("✅ [WebSocket] 정리 작업 완료");
    }

    public void Dispose() => DisconnectAsync().GetAwaiter().GetResult();
}