namespace StockTrading.Application.Services;

public interface IKisSubscriptionManager
{
    Task SubscribeSymbolAsync(string symbol);
    Task UnsubscribeSymbolAsync(string symbol);
    Task UnsubscribeAllAsync();
    IReadOnlyCollection<string> GetSubscribedSymbols();
    void SetWebSocketToken(string token);
}