using Microsoft.AspNetCore.Mvc;
using StockTrading.API.Services;
using StockTrading.Application.Features.Trading.DTOs.Orders;
using StockTrading.Application.Features.Trading.Services;

namespace StockTrading.API.Controllers.Trading;

[Route("api/trading/[controller]")]
public class OrderController : BaseController
{
    private readonly IOrderService _orderService;
    private readonly IBalanceService _balanceService;
    private readonly ILogger<OrderController> _logger;

    public OrderController(IOrderService orderService, IBalanceService balanceService,
        IUserContextService userContextService, ILogger<OrderController> logger) : base(userContextService)
    {
        _orderService = orderService;
        _balanceService = balanceService;
        _logger = logger;
    }

    [HttpPost]
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

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        var user = await GetCurrentUserAsync();
        var balance = await _balanceService.GetStockBalanceAsync(user);
        return Ok(balance);
    }
}