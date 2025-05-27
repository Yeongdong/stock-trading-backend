using Microsoft.AspNetCore.Mvc;
using StockTrading.API.Services;
using StockTrading.Application.Services;

namespace StockTrading.API.Controllers;

[Route("api/[controller]")]
public class RealTimeController : BaseController
{
    private readonly IKisRealTimeService _realTimeService;
    private readonly ILogger<RealTimeController> _logger;

    public RealTimeController(IKisRealTimeService realTimeService, IUserContextService userContextService,
        ILogger<RealTimeController> logger) : base(userContextService)
    {
        _realTimeService = realTimeService;
        _logger = logger;
    }

    /// <summary>
    /// 실시간 데이터 서비스 시작
    /// </summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartRealTimeService()
    {
        var user = await GetCurrentUserAsync();

        if (string.IsNullOrEmpty(user.WebSocketToken))
        {
            throw new ArgumentException("WebSocket 토큰 없음. 먼저 KIS 토큰 발급 필요");
        }

        await _realTimeService.StartAsync(user);

        _logger.LogInformation("사용자 {Email}의 실시간 데이터 서비스 시작", user.Email);
        return Ok(new { message = "실시간 데이터 서비스 시작" });
    }

    /// <summary>
    /// 실시간 데이터 서비스 중지
    /// </summary>
    [HttpPost("stop")]
    public async Task<IActionResult> StopRealTimeService()
    {
        var user = await GetCurrentUserAsync();

        await _realTimeService.StopAsync();

        _logger.LogInformation("사용자 {Email}의 실시간 데이터 서비스 중지", user.Email);
        return Ok(new { message = "실시간 데이터 서비스 중지" });
    }

    /// <summary>
    /// 종목 구독 추가
    /// </summary>
    [HttpPost("subscribe/{symbol}")]
    public async Task<IActionResult> SubscribeSymbol(string symbol)
    {
        if (string.IsNullOrEmpty(symbol) || symbol.Length != 6)
        {
            throw new ArgumentException("유효하지 않은 종목 코드. 6자리 코드 필요");
        }

        var user = await GetCurrentUserAsync();

        await _realTimeService.SubscribeSymbolAsync(symbol);

        _logger.LogInformation("사용자 {Email}가 종목 {Symbol} 구독", user.Email, symbol);
        return Ok(new { message = $"종목 {symbol}이 구독 완료" });
    }

    /// <summary>
    /// 종목 구독 해제
    /// </summary>
    [HttpDelete("subscribe/{symbol}")]
    public async Task<IActionResult> UnsubscribeSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol) || symbol.Length != 6)
        {
            throw new ArgumentException("유효하지 않은 종목 코드. 6자리 코드 필요");
        }

        var user = await GetCurrentUserAsync();

        await _realTimeService.UnsubscribeSymbolAsync(symbol);

        _logger.LogInformation("사용자 {Email}가 종목 {Symbol} 구독 해제", user.Email, symbol);
        return Ok(new { message = $"종목 {symbol} 구독 해제" });
    }


    /// <summary>
    /// 구독 중인 종목 목록 조회
    /// </summary>
    [HttpGet("subscriptions")]
    public IActionResult GetSubscriptions()
    {
        var subscriptions = _realTimeService.GetSubscribedSymbols();
        return Ok(new { symbols = subscriptions });
    }
}