using Microsoft.AspNetCore.Mvc;
using StockTrading.API.Services;
using StockTrading.Application.Features.Market.Services;
using StockTrading.Application.Features.Trading.DTOs.Inquiry;
using StockTrading.Application.Features.Trading.Services;

namespace StockTrading.API.Controllers.Market;

[Route("api/market/[controller]")]
public class StockController : BaseController
{
    private readonly IStockService _stockService;
    private readonly IPeriodPriceService _periodPriceService;
    private readonly ICurrentPriceService _currentPriceService;
    private readonly IStockCacheService _stockCacheService;
    private readonly ILogger<StockController> _logger;

    public StockController(IStockService stockService, IPeriodPriceService periodPriceService,
        ICurrentPriceService currentPriceService, IStockCacheService stockCacheService,
        IUserContextService userContextService, ILogger<StockController> logger) : base(userContextService)
    {
        _stockService = stockService;
        _periodPriceService = periodPriceService;
        _currentPriceService = currentPriceService;
        _stockCacheService = stockCacheService;
        _logger = logger;
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchStocks([FromQuery] string searchTerm, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return BadRequest("검색어를 입력해주세요.");

        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var results = await _stockService.SearchStocksAsync(searchTerm, page, pageSize);
        return Ok(results);
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetStockByCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6)
            return BadRequest("유효한 6자리 종목코드를 입력해주세요.");

        var stock = await _stockService.GetStockByCodeAsync(code);

        if (stock == null)
            return NotFound($"종목코드 {code}를 찾을 수 없습니다.");

        return Ok(stock);
    }

    [HttpGet("current-price")]
    public async Task<IActionResult> GetCurrentPrice([FromQuery] CurrentPriceRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await GetCurrentUserAsync();

        _logger.LogInformation("주식 현재가 조회 요청: 사용자 {UserId}, 종목 {StockCode}", user.Id, request.StockCode);
        var response = await _currentPriceService.GetCurrentPriceAsync(request, user);

        return Ok(response);
    }

    [HttpGet("search/summary")]
    public async Task<IActionResult> GetSearchSummary()
    {
        var summary = await _stockService.GetSearchSummaryAsync();
        return Ok(summary);
    }

    /// <summary>
    /// KRX 데이터 업데이트 (관리자용)
    /// </summary>
    [HttpPost("update-from-krx")]
    public async Task<IActionResult> UpdateStockDataFromKrx()
    {
        await _stockService.UpdateStockDataFromKrxAsync();
        return Ok(new { message = "KRX 데이터 업데이트가 완료되었습니다." });
    }

    /// <summary>
    /// 종목 데이터 동기화 (관리자용)
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> SyncStockData()
    {
        _logger.LogInformation("수동 종목 데이터 동기화 요청");

        var startTime = DateTime.Now;
        await _stockService.UpdateStockDataFromKrxAsync();
        var metrics = await _stockCacheService.GetCacheMetricsAsync();
        var duration = DateTime.Now - startTime;

        return Ok(new
        {
            message = "종목 데이터 동기화 완료",
            syncTime = startTime.ToString("yyyy-MM-dd HH:mm:ss"),
            duration = duration.TotalSeconds,
            cacheMetrics = new
            {
                hitRatio = metrics.HitRatio,
                totalRequests = metrics.TotalHits + metrics.TotalMisses
            }
        });
    }

    [HttpGet("periodPrice")]
    public async Task<IActionResult> GetPeriodPrice([FromQuery] PeriodPriceRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await GetCurrentUserAsync();

        _logger.LogInformation("기간별 시세 조회 요청: 사용자 {UserId}, 종목 {StockCode}, 기간 {Period}", user.Id, request.StockCode,
            request.PeriodDivCode);
        var response = await _periodPriceService.GetPeriodPriceAsync(request, user);

        return Ok(response);
    }
}