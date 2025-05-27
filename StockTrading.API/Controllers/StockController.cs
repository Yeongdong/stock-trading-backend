using Microsoft.AspNetCore.Mvc;
using StockTrading.API.Services;
using StockTrading.Application.DTOs.Orders;
using StockTrading.Application.Services;

namespace StockTrading.API.Controllers;

[Route("api/[controller]")]
public class StockController : BaseController
{
    private readonly IKisService _kisService;
    private readonly ILogger<StockController> _logger;

    public StockController(IKisService kisService, IUserContextService userContextService,
        ILogger<StockController> logger) : base(userContextService)
    {
        _kisService = kisService;
        _logger = logger;
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        var user = await GetCurrentUserAsync();
        var balance = await _kisService.GetStockBalanceAsync(user);
        return Ok(balance);
    }

    [HttpPost("order")]
    public async Task<IActionResult> PlaceOrder(StockOrderRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await GetCurrentUserAsync();

        _logger.LogInformation("주문 시작: 사용자 {UserId}, 종목 {StockCode}", user.Id, request.PDNO);

        var orderResponse = await _kisService.PlaceOrderAsync(request, user);

        _logger.LogInformation("주문 완료: 사용자 {UserId}, 주문번호 {OrderNumber}", user.Id,
            orderResponse?.output?.ODNO ?? "알 수 없음");

        return Ok(orderResponse);
    }
}