using Microsoft.AspNetCore.Mvc;
using StockTrading.API.Services;
using StockTrading.Application.Features.Market.DTOs.Stock;
using StockTrading.Application.Features.Market.Services;

namespace StockTrading.API.Controllers.Market;

[Route("api/market/[controller]")]
public class StockController : BaseController
{
    private readonly IStockService _stockService;
    private readonly IStockCacheService _stockCacheService;
    private readonly ILogger<StockController> _logger;

    public StockController(IStockService stockService, IStockCacheService stockCacheService,
        IUserContextService userContextService, ILogger<StockController> logger) : base(userContextService)
    {
        _stockService = stockService;
        _stockCacheService = stockCacheService;
        _logger = logger;
    }

    #region 주식 검색 및 조회

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

    [HttpGet("search/summary")]
    public async Task<IActionResult> GetSearchSummary()
    {
        var summary = await _stockService.GetSearchSummaryAsync();
        return Ok(summary);
    }

    #endregion

    #region 해외 주식

    [HttpGet("overseas/search")]
    public async Task<IActionResult> SearchForeignStocks([FromQuery] string market, [FromQuery] string query,
        [FromQuery] int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(market))
            return BadRequest("거래소 시장을 입력해주세요. (예: nasdaq, nyse, tokyo 등)");

        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("검색어를 입력해주세요.");

        if (limit is < 1 or > 100)
            limit = 50;

        var request = new ForeignStockSearchRequest
        {
            Market = market,
            Query = query,
            Limit = limit
        };

        var userInfo = await GetCurrentUserAsync();
        var result = await _stockService.SearchForeignStocksAsync(request, userInfo);

        return Ok(result);
    }

    [HttpGet("overseas/markets/{market}")]
    public async Task<IActionResult> GetStocksByMarket(string market)
    {
        if (!Enum.TryParse<StockTrading.Domain.Enums.Market>(market, true, out var marketEnum))
            return BadRequest("지원하지 않는 시장입니다. (nasdaq, nyse, tokyo, london, hongkong)");

        var stocks = await _stockService.GetStocksByMarketAsync(marketEnum);
        return Ok(new { market = market, stocks });
    }

    #endregion

    #region 데이터 동기화 (관리자용)

    [HttpPost("sync/domestic")]
    public async Task<IActionResult> SyncStockData()
    {
        _logger.LogInformation("수동 종목 데이터 동기화 요청");

        var startTime = DateTime.Now;
        await _stockService.SyncDomesticStockDataAsync();
        var metrics = _stockCacheService.GetCacheStats();
        var duration = DateTime.Now - startTime;

        return Ok(new
        {
            message = "종목 데이터 동기화 완료",
            syncTime = startTime.ToString("yyyy-MM-dd HH:mm:ss"),
            duration = duration.TotalSeconds,
            hitRatio = metrics.HitRatio,
        });
    }

    #endregion
}