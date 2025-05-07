using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.Services.Interfaces;

namespace stock_trading_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RealTimeController : ControllerBase
{
    private readonly IKisRealTimeService _realTimeService;
    private readonly IUserService _userService;
    private readonly ILogger<RealTimeController> _logger;

    public RealTimeController(IKisRealTimeService realTimeService, IUserService userService,
        ILogger<RealTimeController> logger)
    {
        _realTimeService = realTimeService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// 실시간 데이터 서비스 시작
    /// </summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartRealTimeService()
    {
        try
        {
            var user = await GetUser();

            if (string.IsNullOrEmpty(user.WebSocketToken))
                return BadRequest("WebSocket 토큰 없음. 먼저 KIS 토큰 발급 필요");

            await _realTimeService.StartAsync(user);

            _logger.LogInformation($"사용자 {user.Email}의 실시간 데이터 서비스 시작");
            return Ok(new { message = "실시간 데이터 서비스 시작" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "실시간 데이터 서비스 시작 중 오류 발생");
            return StatusCode(500, new { error = "실시간 데이터 서비스 시작 중 오류 발생" });
        }
    }

    /// <summary>
    /// 실시간 데이터 서비스 중지
    /// </summary>
    [HttpPost("stop")]
    public async Task<IActionResult> StopRealTimeService()
    {
        try
        {
            await _realTimeService.StopAsync();

            var user = await GetUser();
            _logger.LogInformation($"사용자 {user.Email}의 실시간 데이터 서비스 중지");

            return Ok(new { message = "실시간 데이터 서비스 중지" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "실시간 데이터 서비스 중지 중 오류 발생");
            return StatusCode(500, new { error = "실시간 데이터 서비스 중지 중 오류 발생" });
        }
    }

    /// <summary>
    /// 종목 구독 추가
    /// </summary>
    [HttpPost("subscribe/{symbol}")]
    public async Task<IActionResult> SubscribeSymbol(string symbol)
    {
        try
        {
            if ((string.IsNullOrEmpty(symbol)) || symbol.Length != 6)
                return BadRequest(new { error = "유효하지 않은 종목 코드. 6자리 코드 필요" });

            await _realTimeService.SubscribeSymbolAsync(symbol);

            var user = await GetUser();
            _logger.LogInformation($"사용자 {user.Email}가 종목 {symbol} 구독");

            return Ok(new { message = $"종목 {symbol}이 구독 완료" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "종목 구독 중 오류 발생");
            return StatusCode(500, new { error = "종목 구독 중 오류 발생" });
        }
    }

    /// <summary>
    /// 종목 구독 해제
    /// </summary>
    [HttpDelete("subscribe/{symbol}")]
    public async Task<IActionResult> UnsubscribeSymbol(string symbol)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(symbol) || symbol.Length != 6)
            {
                return BadRequest(new { error = "유효하지 않은 종목 코드. 6자리 코드 필요" });
            }

            await _realTimeService.UnsubscribeSymbolAsync(symbol);

            var user = await GetUser();
            _logger.LogInformation($"사용자 {user.Email}가 종목 {symbol} 구독 해제");

            return Ok(new { message = $"종목 {symbol} 구독 해제" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, $"종목 구독 해제 실패: {ex.Message}");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "종목 구독 해제 중 오류 발생");
            return StatusCode(500, new { error = "종목 구독 해제 중 오류 발생" });
        }
    }

    /// <summary>
    /// 구독 중인 종목 목록 조회
    /// </summary>
    [HttpGet("subscriptions")]
    public IActionResult GetSubscriptions()
    {
        try
        {
            var subscriptions = _realTimeService.GetSubscribedSymbols();

            return Ok(new { symbols = subscriptions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "구독 목록 조회 중 오류 발생");
            return StatusCode(500, new { error = "구독 목록 조회 중 오류 발생" });
        }
    }

    private async Task<UserDto> GetUser()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(email))
            throw new UnauthorizedAccessException();

        var user = await _userService.GetUserByEmailAsync(email);

        if (user == null)
            throw new UnauthorizedAccessException("사용자 정보를 찾을 수 없음");

        return user;
    }
}