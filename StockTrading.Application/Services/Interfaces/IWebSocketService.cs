namespace StockTrading.DataAccess.Services.Interfaces;

public interface IWebSocketService: IDisposable
{
    Task StartAsync();
    Task StopAsync();
    Task SubscribeSymbolAsync(string symbol);
    Task UnsubscribeSymbolAsync(string symbol);
}