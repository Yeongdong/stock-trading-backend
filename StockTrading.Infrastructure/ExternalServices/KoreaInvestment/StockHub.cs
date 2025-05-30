using Microsoft.AspNetCore.SignalR;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

public class StockHub : Hub
{
    public async Task SendStockPrice(string symbol, decimal price)
    {
        await Clients.All.SendAsync("ReceiveStockPrice", symbol, price);
    }

    public async Task SendTradeExecution(string orderId, string symbol, int quantity, decimal price)
    {
        await Clients.All.SendAsync("ReceiveTradeExecution", orderId, symbol, quantity, price);
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}