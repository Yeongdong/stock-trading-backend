using Microsoft.AspNetCore.Authorization;
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
    private readonly IStockService _stockService;
    private readonly IPeriodPriceService _periodPriceService;
    private readonly ICurrentPriceService _currentPriceService;
    private readonly IStockCacheService _stockCacheService;
    private readonly ILogger<StockController> _logger;

    public StockController(IOrderService orderService, IBalanceService balanceService, IStockService stockService,
        IPeriodPriceService periodPriceService, ICurrentPriceService currentPriceService,
        IStockCacheService stockCacheService,
        IUserContextService userContextService, ILogger<StockController> logger) : base(userContextService)
    {
        _orderService = orderService;
        _balanceService = balanceService;
        _stockService = stockService;
        _periodPriceService = periodPriceService;
        _stockCacheService = stockCacheService;
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

    [HttpPost("update-from-krx")]
    [AllowAnonymous]
    public async Task<IActionResult> UpdateStockDataFromKrx()
    {
        await _stockService.UpdateStockDataFromKrxAsync();
        return Ok(new { message = "KRX 데이터 업데이트가 완료되었습니다." });
    }

    [HttpPost("/admin/sync")]
    public async Task<IActionResult> SyncStockData()
    {
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