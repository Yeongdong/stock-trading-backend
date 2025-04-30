using Microsoft.AspNetCore.SignalR;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

public class StockHub: Hub
{
    public async Task SendStockPrice(string symbol, decimal price)
    {
        await Clients.All.SendAsync("ReceiveStockPrice", symbol, price);
    }
}