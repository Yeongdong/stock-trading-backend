namespace StockTrading.Infrastructure.ExternalServices.Interfaces;

public interface IKisWebSocketClient
{
    event EventHandler<string> MessageReceived;
    Task ConnectAsync(string url);
    Task AuthenticateAsync(string token);
    Task SendMessageAsync(string message);
    Task DisconnectAsync();
    void Dispose();
}