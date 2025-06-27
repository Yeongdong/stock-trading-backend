using Microsoft.AspNetCore.Mvc;
using StockTrading.API.Services;
using StockTrading.Application.Features.Market.Services;
using StockTrading.Application.Features.Trading.DTOs.Inquiry;

namespace StockTrading.API.Controllers.Market;

[Route("api/market/[controller]")]
public class PriceController : BaseController
{
    private readonly IPriceService _priceService;
    private readonly ILogger<PriceController> _logger;

    public PriceController(IPriceService priceService, IUserContextService userContextService,
        ILogger<PriceController> logger) : base(userContextService)
    {
        _priceService = priceService;
        _logger = logger;
    }

    #region 국내 주식 시세

    [HttpGet("domestic/current-price")]
    public async Task<IActionResult> GetCurrentPrice([FromQuery] CurrentPriceRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await GetCurrentUserAsync();

        _logger.LogInformation("주식 현재가 조회 요청: 사용자 {UserId}, 종목 {StockCode}", user.Id, request.StockCode);
        var response = await _priceService.GetDomesticCurrentPriceAsync(request, user);

        return Ok(response);
    }

    [HttpGet("domestic/period-price")]
    public async Task<IActionResult> GetPeriodPrice([FromQuery] PeriodPriceRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await GetCurrentUserAsync();

        _logger.LogInformation("기간별 시세 조회 요청: 사용자 {UserId}, 종목 {StockCode}, 기간 {Period}",
            user.Id, request.StockCode, request.PeriodDivCode);
        var response = await _priceService.GetDomesticPeriodPriceAsync(request, user);

        return Ok(response);
    }

    #endregion

    #region 해외 주식 시세

    [HttpGet("overseas/current-price/{stockCode}")]
    public async Task<IActionResult> GetOverseasCurrentPrice(string stockCode, [FromQuery] string market)
    {
        if (string.IsNullOrWhiteSpace(stockCode))
            return BadRequest("종목코드를 입력해주세요.");

        if (string.IsNullOrWhiteSpace(market))
            return BadRequest("시장 정보를 입력해주세요. (예: nasdaq, nyse)");

        if (!Enum.TryParse<StockTrading.Domain.Enums.Market>(market, true, out var marketEnum))
            return BadRequest("지원하지 않는 시장입니다. (nasdaq, nyse, tokyo, london, hongkong)");

        var user = await GetCurrentUserAsync();

        _logger.LogInformation("해외 주식 현재가 조회: 사용자 {UserId}, 종목 {StockCode}, 시장 {Market}",
            user.Id, stockCode, market);

        var response = await _priceService.GetOverseasCurrentPriceAsync(stockCode, marketEnum, user);

        return Ok(response);
    }

    [HttpGet("overseas/period-price")]
    public async Task<IActionResult> GetOverseasPeriodPrice([FromQuery] OverseasPeriodPriceRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await GetCurrentUserAsync();

        _logger.LogInformation("해외 주식 기간별시세 조회 요청: 사용자 {UserId}, 종목 {StockCode}, 기간 {Period}",
            user.Id, request.StockCode, request.PeriodDivCode);

        var response = await _priceService.GetOverseasPeriodPriceAsync(request, user);

        return Ok(response);
    }

    #endregion
}