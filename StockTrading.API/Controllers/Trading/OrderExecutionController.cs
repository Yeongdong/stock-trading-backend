using Microsoft.AspNetCore.Mvc;
using StockTrading.API.Services;
using StockTrading.Application.Features.Trading.DTOs.Orders;
using StockTrading.Application.Features.Trading.Services;

namespace StockTrading.API.Controllers.Trading;

[Route("api/trading/[controller]")]
public class OrderExecutionController : BaseController
{
    private readonly IOrderExecutionInquiryService _orderExecutionService;

    public OrderExecutionController(IOrderExecutionInquiryService orderExecutionService,
        IUserContextService userContextService) : base(userContextService)
    {
        _orderExecutionService = orderExecutionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrderExecutions([FromQuery] OrderExecutionInquiryRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await GetCurrentUserAsync();
        var response = await _orderExecutionService.GetOrderExecutionsAsync(request, user);

        return Ok(response);
    }
}