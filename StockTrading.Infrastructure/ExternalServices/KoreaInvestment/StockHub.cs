using Microsoft.AspNetCore.SignalR;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

public class StockHub : Hub
{
    // 실시간 주가 데이터 전송 메서드
    public async Task SendStockPrice(string symbol, decimal price)
    {
        await Clients.All.SendAsync("ReceiveStockPrice", symbol, price);
    }
    
    // 연결된 클라이언트에게 거래 체결 정보 전송
    public async Task SendTradeExecution(string orderId, string symbol, int quantity, decimal price)
    {
        await Clients.All.SendAsync("ReceiveTradeExecution", orderId, symbol, quantity, price);
    }
    
    // 클라이언트가 연결될 때 호출되는 메서드
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }
    
    // 클라이언트가 연결 해제될 때 호출되는 메서드
    public override async Task OnDisconnectedAsync(Exception exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}