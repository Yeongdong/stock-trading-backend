using Microsoft.AspNetCore.Mvc;
using stock_trading_backend.Services;
using StockTrading.DataAccess.DTOs.OrderDTOs;
using StockTrading.DataAccess.Services.Interfaces;

namespace stock_trading_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly IKisService _kisService;
    private readonly IUserContextService _userContextService;
    private readonly ILogger<StockController> _logger;

    public StockController(IKisService kisService, IUserContextService userContextService,
        ILogger<StockController> logger)
    {
        _kisService = kisService;
        _userContextService = userContextService;
        _logger = logger;
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        var user = await _userContextService.GetCurrentUserAsync();
        var balance = await _kisService.GetStockBalanceAsync(user);
        return Ok(balance);
    }

    [HttpPost("order")]
    public async Task<IActionResult> PlaceOrder(StockOrderRequest request)
    {
        var user = await _userContextService.GetCurrentUserAsync();

        _logger.LogInformation("주문 시작: 사용자 {UserId}, 종목 {StockCode}", user.Id, request.PDNO);

        var orderResponse = await _kisService.PlaceOrderAsync(request, user);

        _logger.LogInformation("주문 완료: 사용자 {UserId}, 주문번호 {OrderNumber}", user.Id,
            orderResponse?.output?.ODNO ?? "알 수 없음");

        return Ok(orderResponse);
    }
}