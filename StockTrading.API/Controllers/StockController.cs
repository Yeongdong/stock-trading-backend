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
        var user = await GetUser();

        try
        {
            var balance = await _kisService.GetStockBalanceAsync(user);
            return Ok(balance);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "잔고 조회 중 오류 발생");
        }
    }

    [HttpPost("order")]
    public async Task<ActionResult<StockOrderResponse>> PlaceOrder(StockOrderRequest request)
    {
        var user = await GetUser();
        
        try
        {
            _logger.LogInformation("주문 시작: {@Request}", request); // 요청 로깅
            var orderState = await _kisService.PlaceOrderAsync(request, user);
            _logger.LogInformation("주문 완료: {@Response}", orderState); // 응답 로깅
            return Ok(orderState);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "주문 실패");
        }
    }

    private async Task<UserDto> GetUser()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(email))
        {
            throw new UnauthorizedAccessException();
        }

        var user = await _userService.GetUserByEmail(email);
        return user;
    }
}