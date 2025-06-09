using Microsoft.AspNetCore.Mvc;
using StockTrading.API.Services;
using StockTrading.Application.Features.Trading.DTOs.Inquiry;
using StockTrading.Application.Features.Trading.Services;

namespace StockTrading.API.Controllers.Trading;

[Route("api/[controller]")]
public class BuyableInquiryController : BaseController
{
    private readonly IBuyableInquiryService _buyableInquiryService;

    public BuyableInquiryController(IBuyableInquiryService buyableInquiryService,
        IUserContextService userContextService)
        : base(userContextService)
    {
        _buyableInquiryService = buyableInquiryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetBuyableInquiry([FromQuery] BuyableInquiryRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await GetCurrentUserAsync();
        var response = await _buyableInquiryService.GetBuyableInquiryAsync(request, user);

        return Ok(response);
    }
}