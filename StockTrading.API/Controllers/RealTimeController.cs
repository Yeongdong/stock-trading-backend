using Microsoft.AspNetCore.Mvc;
using StockTrading.API.Services;
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

    /// <summary>
    /// 실시간 데이터 서비스 시작
    /// </summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartRealTimeService()
    {
        var user = await GetCurrentUserAsync();

        if (string.IsNullOrEmpty(user.WebSocketToken))
            return BadRequest("WebSocket 토큰이 필요합니다. KIS 토큰을 먼저 발급받아주세요.");

        await _realTimeService.StartAsync(user);
        return Ok(new { message = "실시간 데이터 서비스 시작" });
    }

    /// <summary>
    /// 실시간 데이터 서비스 중지
    /// </summary>
    [HttpPost("stop")]
    public async Task<IActionResult> StopRealTimeService()
    {
        await _realTimeService.StopAsync();

        return Ok(new { message = "실시간 데이터 서비스 중지" });
    }

    /// <summary>
    /// 종목 구독 추가
    /// </summary>
    [HttpPost("subscribe/{symbol}")]
    public async Task<IActionResult> SubscribeSymbol(string symbol)
    {
        if (string.IsNullOrEmpty(symbol) || symbol.Length != 6)
            return BadRequest("유효하지 않은 종목 코드. 6자리 코드 필요");

        try
        {
            await _realTimeService.SubscribeSymbolAsync(symbol);
            return Ok(new { message = $"종목 {symbol} 구독 완료" });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("실시간 서비스가 시작되지 않았습니다"))
        {
            return BadRequest("실시간 서비스를 먼저 시작해야 합니다. POST /api/realtime/start를 호출하세요.");
        }
    }

    /// <summary>
    /// 종목 구독 해제
    /// </summary>
    [HttpDelete("subscribe/{symbol}")]
    public async Task<IActionResult> UnsubscribeSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol) || symbol.Length != 6)
            throw new ArgumentException("유효하지 않은 종목 코드. 6자리 코드 필요");

        await _realTimeService.UnsubscribeSymbolAsync(symbol);

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