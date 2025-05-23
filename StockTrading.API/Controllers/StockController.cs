using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.DTOs.OrderDTOs;
using StockTrading.DataAccess.Services.Interfaces;

namespace stock_trading_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly IKisService _kisService;
    private readonly IUserService _userService;
    private readonly ILogger<StockController> _logger;

    public StockController(IKisService kisService, IUserService userService, ILogger<StockController> logger)
    {
        _kisService = kisService;
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("balance")]
    public async Task<ActionResult<StockBalance>> GetBalance()
    {
        try
        {
            var userResult = await GetUserAsync();
            if (userResult.Result != null)
                return userResult.Result;

            var user = userResult.Value;
            var balance = await _kisService.GetStockBalanceAsync(user);

            return Ok(balance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "잔고 조회 중 오류 발생");
            return StatusCode(500, "잔고 조회 중 오류 발생");
        }
    }

    [HttpPost("order")]
    public async Task<ActionResult<StockOrderResponse>> PlaceOrder(StockOrderRequest request)
    {
        try
        {
            var userResult = await GetUserAsync();
            if (userResult.Result != null)
                return userResult.Result; // 에러 응답 반환

            var user = userResult.Value;

            _logger.LogInformation("주문 시작: {@Request}", request);
            var orderResponse = await _kisService.PlaceOrderAsync(request, user);
            _logger.LogInformation("주문 완료: {@Response}", orderResponse);
            
            return Ok(orderResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "주문 실행 중 오류 발생");
            return StatusCode(500, "주문 실패");
        }
    }

    private async Task<ActionResult<UserDto>> GetUserAsync()
    {
        try
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("인증된 사용자의 이메일 정보를 찾을 수 없음");
                return Unauthorized("사용자 인증 정보가 유효하지 않습니다.");
            }

            var user = await _userService.GetUserByEmailAsync(email);

            if (user == null)
            {
                _logger.LogWarning("사용자 정보를 찾을 수 없음: {Email}", email);
                return NotFound("사용자 정보를 찾을 수 없습니다.");
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "사용자 정보 조회 중 오류 발생");
            return StatusCode(500, "사용자 정보 조회 실패");
        }
    }
}