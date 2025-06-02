using Microsoft.AspNetCore.Mvc;
using StockTrading.API.Services;
using StockTrading.Application.DTOs.Trading.Inquiry;
using StockTrading.Application.DTOs.Trading.Orders;
using StockTrading.Application.Services;

namespace StockTrading.API.Controllers;

[Route("api/[controller]")]
public class StockController : BaseController
{
    private readonly IOrderService _orderService;
    private readonly IBalanceService _balanceService;
    private readonly ICurrentPriceService _currentPriceService;
    private readonly ILogger<StockController> _logger;

    public StockController(IOrderService orderService, IBalanceService balanceService,
        ICurrentPriceService currentPriceService,
        IUserContextService userContextService, ILogger<StockController> logger) : base(userContextService)
    {
        _orderService = orderService;
        _balanceService = balanceService;
        _currentPriceService = currentPriceService;
        _logger = logger;
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        var user = await GetCurrentUserAsync();
        var balance = await _balanceService.GetStockBalanceAsync(user);
        return Ok(balance);
    }

    [HttpPost("order")]
    public async Task<IActionResult> PlaceOrder(OrderRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await GetCurrentUserAsync();

        _logger.LogInformation("주문 시작: 사용자 {UserId}, 종목 {StockCode}", user.Id, request.PDNO);
        var orderResponse = await _orderService.PlaceOrderAsync(request, user);
        var orderNumber = orderResponse?.Output?.OrderNumber ?? "알 수 없음";
        _logger.LogInformation("주문 완료: 사용자 {UserId}, 주문번호 {OrderNumber}", user.Id, orderNumber);

        return Ok(orderResponse);
    }

    [HttpGet("currentPrice")]
    public async Task<IActionResult> GetCurrentPrice([FromQuery] CurrentPriceRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await GetCurrentUserAsync();

        _logger.LogInformation("주식 현재가 조회 요청: 사용자 {UserId}, 종목 {StockCode}", user.Id, request.StockCode);
        var response = await _currentPriceService.GetCurrentPriceAsync(request, user);

        return Ok(response);
    }
}    