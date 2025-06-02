using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

[Authorize]  // Hub 전체에 인증 요구
public class StockHub : Hub
{
    private readonly ILogger<StockHub> _logger;

    public StockHub(ILogger<StockHub> logger)
    {
        _logger = logger;
    }

    public async Task SendStockPrice(string symbol, decimal price)
    {
        _logger.LogInformation("📤 [StockHub] SendStockPrice 호출: {Symbol} - {Price}원", symbol, price);
        
        await Clients.All.SendAsync("ReceiveStockPrice", symbol, price);
        
        _logger.LogInformation("✅ [StockHub] SendStockPrice 전송 완료: {Symbol}", symbol);
    }

    public async Task SendTradeExecution(string orderId, string symbol, int quantity, decimal price)
    {
        _logger.LogInformation("📤 [StockHub] SendTradeExecution 호출: {Symbol}", symbol);
        
        await Clients.All.SendAsync("ReceiveTradeExecution", orderId, symbol, quantity, price);
        
        _logger.LogInformation("✅ [StockHub] SendTradeExecution 전송 완료: {Symbol}", symbol);
    }

    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        var userEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
        var userAgent = Context.GetHttpContext()?.Request.Headers["User-Agent"].ToString();
        
        _logger.LogInformation("🔗 [StockHub] 클라이언트 연결: {ConnectionId} | 사용자: {Email} ({Name}) | UserAgent: {UserAgent}", 
            connectionId, userEmail ?? "인증안됨", userName ?? "Unknown", userAgent ?? "Unknown");

        // 인증된 사용자인지 확인
        if (Context.User?.Identity?.IsAuthenticated == true)
        {
            _logger.LogInformation("✅ [StockHub] 인증된 사용자 연결: {Email}", userEmail);
            
            // 클라이언트에게 연결 완료 알림
            await Clients.Caller.SendAsync("Connected", new 
            { 
                connectionId = connectionId,
                user = new 
                {
                    email = userEmail,
                    name = userName,
                    isAuthenticated = true
                },
                timestamp = DateTime.UtcNow
            });
        }
        else
        {
            _logger.LogWarning("🚫 [StockHub] 인증되지 않은 사용자 연결 시도: {ConnectionId}", connectionId);
            
            // 인증되지 않은 경우 연결 종료
            await Clients.Caller.SendAsync("AuthenticationRequired", new 
            {
                message = "인증이 필요합니다.",
                timestamp = DateTime.UtcNow
            });
            
            Context.Abort();
            return;
        }
        
        await base.OnConnectedAsync();
        
        _logger.LogInformation("✅ [StockHub] 연결 완료: {ConnectionId} - {Email}", connectionId, userEmail);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        var userEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value;
        
        if (exception != null)
        {
            _logger.LogWarning("🚪 [StockHub] 클라이언트 연결 해제 (오류): {ConnectionId} - {Email} | 예외: {Exception}", 
                connectionId, userEmail ?? "Unknown", exception.Message);
        }
        else
        {
            _logger.LogInformation("🚪 [StockHub] 클라이언트 연결 해제 (정상): {ConnectionId} - {Email}", 
                connectionId, userEmail ?? "Unknown");
        }
            
        await base.OnDisconnectedAsync(exception);
    }

    // 클라이언트에서 호출할 수 있는 테스트 메서드
    public async Task SendTestData()
    {
        var connectionId = Context.ConnectionId;
        var userEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value;
        
        _logger.LogInformation("🧪 [StockHub] 테스트 데이터 요청: {ConnectionId} - {Email}", connectionId, userEmail);
        
        var testData = new
        {
            message = "테스트 데이터입니다",
            timestamp = DateTime.UtcNow,
            connectionId = connectionId,
            userEmail = userEmail
        };
        
        await Clients.Caller.SendAsync("ReceiveTestData", testData);
        
        _logger.LogInformation("✅ [StockHub] 테스트 데이터 전송 완료: {ConnectionId}", connectionId);
    }

    // 연결 상태 확인 메서드
    public async Task CheckConnection()
    {
        var connectionId = Context.ConnectionId;
        var userEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value;
        var isAuthenticated = Context.User?.Identity?.IsAuthenticated == true;
        
        _logger.LogInformation("🔍 [StockHub] 연결 상태 확인: {ConnectionId} - {Email} (인증: {IsAuthenticated})", 
            connectionId, userEmail ?? "Unknown", isAuthenticated);
        
        var connectionInfo = new
        {
            connectionId = connectionId,
            userEmail = userEmail,
            isAuthenticated = isAuthenticated,
            timestamp = DateTime.UtcNow,
            status = "connected"
        };
        
        await Clients.Caller.SendAsync("ConnectionStatus", connectionInfo);
    }

    // 그룹 참여 (종목별 그룹 관리용 - 추후 사용)
    public async Task JoinGroup(string groupName)
    {
        var connectionId = Context.ConnectionId;
        var userEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value;
        
        _logger.LogInformation("👥 [StockHub] 그룹 참여: {GroupName} - {ConnectionId} ({Email})", 
            groupName, connectionId, userEmail ?? "Unknown");
        
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
        var connectionId = Context.ConnectionId;
        var userEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value;
        
        _logger.LogInformation("🚪 [StockHub] 그룹 탈퇴: {GroupName} - {ConnectionId} ({Email})", 
            groupName, connectionId, userEmail ?? "Unknown");
        
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        
        await Clients.Caller.SendAsync("LeftGroup", new 
        { 
            groupName = groupName, 
            timestamp = DateTime.UtcNow 
        });
    }
}