using Microsoft.AspNetCore.Mvc;
using StockTrading.API.Services;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.Services;

namespace StockTrading.API.Controllers;

[Route("api/[controller]")]
public class RealTimeController : BaseController
{
    private readonly IRealTimeService _realTimeService;
    private readonly ILogger<RealTimeController> _logger;

    public RealTimeController(IRealTimeService realTimeService, IUserContextService userContextService,
        ILogger<RealTimeController> logger) : base(userContextService)
    {
        _realTimeService = realTimeService;
        _logger = logger;
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartRealTimeService()
    {
        var user = await GetCurrentUserAsync();

        if (string.IsNullOrEmpty(user.WebSocketToken))
            return BadRequest("WebSocket 토큰이 필요합니다. KIS 토큰을 먼저 발급받아주세요.");

        await _realTimeService.StartAsync(user);
        return Ok(new { message = "실시간 데이터 서비스 시작" });
    }

    [HttpPost("stop")]
    public async Task<IActionResult> StopRealTimeService()
    {
        await _realTimeService.StopAsync();
        return Ok(new { message = "실시간 데이터 서비스 중지" });
    }

    [HttpPost("subscribe/{symbol}")]
    public async Task<IActionResult> SubscribeSymbol(string symbol)
    {
        if (string.IsNullOrEmpty(symbol) || symbol.Length != 6)
            return BadRequest("유효하지 않은 종목 코드. 6자리 코드 필요");

        await _realTimeService.SubscribeSymbolAsync(symbol);
        return Ok(new { message = $"종목 {symbol} 구독 완료" });
    }

    [HttpDelete("subscribe/{symbol}")]
    public async Task<IActionResult> UnsubscribeSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol) || symbol.Length != 6)
            throw new ArgumentException("유효하지 않은 종목 코드. 6자리 코드 필요");

        await _realTimeService.UnsubscribeSymbolAsync(symbol);
        return Ok(new { message = $"종목 {symbol} 구독 해제" });
    }

    [HttpGet("subscriptions")]
    public IActionResult GetSubscriptions()
    {
        var subscriptions = _realTimeService.GetSubscribedSymbols();
        return Ok(new { symbols = subscriptions });
    }

    [HttpGet("status")]
    public IActionResult GetRealTimeStatus()
    {
        var subscribedSymbols = _realTimeService.GetSubscribedSymbols();

        return Ok(new
        {
            subscribedSymbols = subscribedSymbols,
            subscribedCount = subscribedSymbols.Count,
            serviceStatus = "running",
            timestamp = DateTime.UtcNow
        });
    }
}