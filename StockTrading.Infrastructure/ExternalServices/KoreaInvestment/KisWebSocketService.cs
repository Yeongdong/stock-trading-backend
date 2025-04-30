using System.Net.WebSockets;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.Services.Interfaces;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

public class KisWebSocketService: IWebSocketService
{
    
    private readonly ClientWebSocket _webSocket;
    private CancellationTokenSource _cancellationTokenSource;
    private readonly ILogger<KisWebSocketService> _logger;
    private readonly IHubContext<StockHub> _hubContext;
    private bool _isRunning;

    public KisWebSocketService(ILogger<KisWebSocketService> logger, IHubContext<StockHub> hubContext)
    {
        _webSocket = new ClientWebSocket();
        _logger = logger;
        _hubContext = hubContext;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public async Task StartAsync(UserDto user)
    {
        if (_isRunning) return;

        try
        {
            var uri = new Uri("ws://ops.koreainvestment.com:31000");
            await _webSocket.ConnectAsync(uri, _cancellationTokenSource.Token);
            _isRunning = true;

            await AuthenticateAsync(user);

            _ = ReceiveMessagesAsync();

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket connection failed");
            throw;
        }
    }

    private async Task AuthenticateAsync(UserDto user)
    {
        var authMessage = new
        {
            
        };
    }

    private Task ReceiveMessagesAsync()
    {
        throw new NotImplementedException();
    }

    public Task StopAsync()
    {
        throw new NotImplementedException();
    }

    public Task SubscribeSymbolAsync(string symbol)
    {
        throw new NotImplementedException();
    }

    public Task UnsubscribeSymbolAsync(string symbol)
    {
        throw new NotImplementedException();
    }
    
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public Task StartAsync()
    {
        throw new NotImplementedException();
    }
}