namespace StockTrading.Application.Features.Market.Services;

public interface ISubscriptionManager
{
    Task SubscribeSymbolAsync(string symbol);
    Task UnsubscribeSymbolAsync(string symbol);
    Task UnsubscribeAllAsync();
    IReadOnlyCollection<string> GetSubscribedSymbols();
    void SetWebSocketToken(string token);
}