using Microsoft.AspNetCore.Mvc;
using StockTrading.API.Services;
using StockTrading.Application.DTOs.Trading.Orders;
using StockTrading.Application.Services;

namespace StockTrading.API.Controllers;

[Route("api/[controller]")]
public class OrderExecutionController : BaseController
{
    private readonly IOrderExecutionInquiryService _orderExecutionService;
    private readonly ILogger<OrderExecutionController> _logger;

    public OrderExecutionController(IOrderExecutionInquiryService orderExecutionService,
        IUserContextService userContextService, ILogger<OrderExecutionController> logger) : base(userContextService)
    {
        _orderExecutionService = orderExecutionService;
        _logger = logger;
    }

    /// <summary>
    /// 주문체결내역 조회
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetOrderExecutions([FromQuery] OrderExecutionInquiryRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await GetCurrentUserAsync();

        _logger.LogInformation("주문체결조회 요청: 사용자 {UserId}, 기간 {StartDate}~{EndDate}", user.Id, request.StartDate, request.EndDate);
        var response = await _orderExecutionService.GetOrderExecutionsAsync(request, user);
        _logger.LogInformation("주문체결조회 완료: 사용자 {UserId}, 총 {Count}건", user.Id, response.TotalCount);

        return Ok(response);
    }
}