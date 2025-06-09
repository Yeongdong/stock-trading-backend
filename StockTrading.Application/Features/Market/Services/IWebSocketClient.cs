namespace StockTrading.Application.Features.Market.Services;

public interface IWebSocketClient
{
    event EventHandler<string> MessageReceived;
    event EventHandler? ConnectionLost;
    Task ConnectAsync(string url);
    Task SendMessageAsync(string message);
    Task DisconnectAsync();
}