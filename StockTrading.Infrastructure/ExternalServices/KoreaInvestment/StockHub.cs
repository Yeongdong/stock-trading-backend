using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using StockTrading.Application.Services;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

[Authorize]
public class StockHub : Hub
{
    private readonly ILogger<StockHub> _logger;

    public StockHub(ILogger<StockHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value;

        _logger.LogInformation("SignalR 연결: {Email}", userEmail ?? "인증안됨");

        if (Context.User?.Identity?.IsAuthenticated == true)
        {
            await Clients.Caller.SendAsync("Connected", new
            {
                connectionId = Context.ConnectionId,
                user = new
                {
                    email = userEmail,
                    isAuthenticated = true
                },
                timestamp = DateTime.UtcNow
            });
        }
        else
        {
            _logger.LogWarning("SignalR 인증 실패: ");
            await Clients.Caller.SendAsync("AuthenticationRequired", new
            {
                message = "인증이 필요합니다.",
                timestamp = DateTime.UtcNow
            });
            Context.Abort();
            return;
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value;

        if (exception != null)
            _logger.LogWarning("SignalR 연결 해제 (오류): {Email} - {Error}", userEmail ?? "Unknown", exception.Message);
        else
            _logger.LogInformation("SignalR 연결 해제: {Email}", userEmail ?? "Unknown");

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendStockPrice(string symbol, decimal price)
    {
        await Clients.All.SendAsync("ReceiveStockPrice", symbol, price);
    }

    public async Task SendTradeExecution(string orderId, string symbol, int quantity, decimal price)
    {
        await Clients.All.SendAsync("ReceiveTradeExecution", orderId, symbol, quantity, price);
    }

    // 연결 상태 확인 메서드
    public async Task CheckConnection()
    {
        var connectionInfo = new
        {
            connectionId = Context.ConnectionId,
            userEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value,
            isAuthenticated = Context.User?.Identity?.IsAuthenticated == true,
            timestamp = DateTime.UtcNow,
            status = "connected"
        };

        await Clients.Caller.SendAsync("ConnectionStatus", connectionInfo);
    }

    // 그룹 참여 (종목별 그룹 관리용 - 추후 사용)
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("JoinedGroup", new
        {
            groupName = groupName,
            timestamp = DateTime.UtcNow
        });
    }

    // 그룹 탈퇴
    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("LeftGroup", new
        {
            groupName = groupName,
            timestamp = DateTime.UtcNow
        });
    }
}