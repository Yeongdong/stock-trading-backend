using Microsoft.AspNetCore.Mvc;
using StockTrading.API.Services;
using StockTrading.Application.Features.Trading.DTOs.Inquiry;
using StockTrading.Application.Features.Trading.DTOs.Orders;
using StockTrading.Application.Features.Trading.Services;

namespace StockTrading.API.Controllers.Trading;

[Route("api/{controller}")]
public class TradingController : BaseController
{
    private readonly ITradingService _tradingService;
    private readonly ILogger<TradingController> _logger;

    public TradingController(ITradingService tradingService, IUserContextService userContextService,
        ILogger<TradingController> logger) : base(userContextService)
    {
        _tradingService = tradingService;
        _logger = logger;
    }

    #region 주문 관리

    [HttpPost("order")]
    public async Task<IActionResult> PlaceOrder(OrderRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await GetCurrentUserAsync();

        _logger.LogInformation("주문 시작: 사용자 {UserId}, 종목 {StockCode}", user.Id, request.PDNO);
        var orderResponse = await _tradingService.PlaceOrderAsync(request, user);

        return Ok(orderResponse);
    }

    [HttpGet("buyable-inquiry")]
    public async Task<IActionResult> GetBuyableInquiry([FromQuery] BuyableInquiryRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await GetCurrentUserAsync();
        var response = await _tradingService.GetBuyableInquiryAsync(request, user);

        return Ok(response);
    }

    #endregion

    #region 조회

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        var user = await GetCurrentUserAsync();
        var balance = await _tradingService.GetStockBalanceAsync(user);
        return Ok(balance);
    }

    [HttpGet("executions")]
    public async Task<IActionResult> GetOrderExecutions([FromQuery] OrderExecutionInquiryRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await GetCurrentUserAsync();
        var response = await _tradingService.GetOrderExecutionsAsync(request, user);

        return Ok(response);
    }

    #endregion
}