using StockTrading.Application.Features.Users.DTOs;

namespace StockTrading.Application.Features.Market.Services;

public interface IRealTimeService
{
    Task StartAsync(UserInfo user);
    Task StopAsync();
    Task SubscribeSymbolAsync(string symbol);
    Task UnsubscribeSymbolAsync(string symbol);
    IReadOnlyCollection<string> GetSubscribedSymbols();
}