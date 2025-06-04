using Microsoft.AspNetCore.Mvc;
using StockTrading.API.Services;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.Services;

namespace StockTrading.API.Controllers;

[Route("api/[controller]")]
public class RealTimeController : BaseController
{
    private readonly IRealTimeService _realTimeService;
    private readonly ILogger<RealTimeController> _logger;
    private readonly IRealTimeDataBroadcaster _broadcaster;

    public RealTimeController(IRealTimeService realTimeService, IUserContextService userContextService,
        ILogger<RealTimeController> logger, IRealTimeDataBroadcaster broadcaster) : base(userContextService)
    {
        _realTimeService = realTimeService;
        _logger = logger;
        _broadcaster = broadcaster;
    }

    /// <summary>
    /// 실시간 데이터 서비스 시작
    /// </summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartRealTimeService()
    {
        var user = await GetCurrentUserAsync();

        if (string.IsNullOrEmpty(user.WebSocketToken))
            return BadRequest("WebSocket 토큰이 필요합니다. KIS 토큰을 먼저 발급받아주세요.");

        await _realTimeService.StartAsync(user);
        return Ok(new { message = "실시간 데이터 서비스 시작" });
    }

    /// <summary>
    /// 실시간 데이터 서비스 중지
    /// </summary>
    [HttpPost("stop")]
    public async Task<IActionResult> StopRealTimeService()
    {
        await _realTimeService.StopAsync();

        return Ok(new { message = "실시간 데이터 서비스 중지" });
    }

    /// <summary>
    /// 종목 구독 추가
    /// </summary>
    [HttpPost("subscribe/{symbol}")]
    public async Task<IActionResult> SubscribeSymbol(string symbol)
    {
        if (string.IsNullOrEmpty(symbol) || symbol.Length != 6)
            return BadRequest("유효하지 않은 종목 코드. 6자리 코드 필요");

        try
        {
            await _realTimeService.SubscribeSymbolAsync(symbol);
            return Ok(new { message = $"종목 {symbol} 구독 완료" });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("실시간 서비스가 시작되지 않았습니다"))
        {
            return BadRequest("실시간 서비스를 먼저 시작해야 합니다. POST /api/realtime/start를 호출하세요.");
        }
    }

    /// <summary>
    /// 종목 구독 해제
    /// </summary>
    [HttpDelete("subscribe/{symbol}")]
    public async Task<IActionResult> UnsubscribeSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol) || symbol.Length != 6)
            throw new ArgumentException("유효하지 않은 종목 코드. 6자리 코드 필요");

        await _realTimeService.UnsubscribeSymbolAsync(symbol);

        return Ok(new { message = $"종목 {symbol} 구독 해제" });
    }


    /// <summary>
    /// 구독 중인 종목 목록 조회
    /// </summary>
    [HttpGet("subscriptions")]
    public IActionResult GetSubscriptions()
    {
        var subscriptions = _realTimeService.GetSubscribedSymbols();
        return Ok(new { symbols = subscriptions });
    }
    
    [HttpGet("status")]
    public IActionResult GetRealTimeStatus()
    {
        var subscribedSymbols = _realTimeService.GetSubscribedSymbols();
    
        return Ok(new
        {
            subscribedSymbols = subscribedSymbols,
            subscribedCount = subscribedSymbols.Count,
            serviceStatus = "running",
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("test-data/{symbol}")]
    public async Task<IActionResult> SendTestData(string symbol = "005930")
    {
        try
        {
            _logger.LogInformation("🧪 [RealTimeController] 테스트 데이터 전송 시작: {Symbol}", symbol);
        
            // 테스트 데이터 생성
            var testData = new KisTransactionInfo
            {
                Symbol = symbol,
                Price = 76000 + new Random().Next(-2000, 2000),
                PriceChange = new Random().Next(-1000, 1000),
                ChangeType = "상승",
                ChangeRate = 1.33m,
                Volume = 100000,
                TotalVolume = 5000000,
                TransactionTime = DateTime.Now
            };

            _logger.LogInformation("📊 [RealTimeController] 테스트 데이터: {@TestData}", testData);

            // 브로드캐스터를 통해 전송
            await _broadcaster.BroadcastStockPriceAsync(testData);
        
            _logger.LogInformation("✅ [RealTimeController] 테스트 데이터 전송 완료");
        
            return Ok(new { 
                message = "테스트 데이터 전송 완료", 
                data = testData,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [RealTimeController] 테스트 데이터 전송 실패");
            return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpGet("debug/kis-status")]
    public IActionResult GetKisWebSocketStatus()
    {
        // KIS WebSocket 상태 확인 (추후 구현)
        return Ok(new
        {
            message = "KIS WebSocket 상태 확인용 엔드포인트",
            timestamp = DateTime.UtcNow,
            note = "실제 KIS 연결 상태는 RealTimeService에서 확인 필요"
        });
    }
    
    [HttpGet("debug/kis-connection")]
    public IActionResult GetKisConnectionStatus()
    {
        try
        {
            // RealTimeService의 실제 상태를 확인하기 위한 정보 수집
            var subscribedSymbols = _realTimeService.GetSubscribedSymbols();
        
            return Ok(new
            {
                subscribedSymbols = subscribedSymbols,
                subscribedCount = subscribedSymbols.Count,
                kisWebSocketUrl = "ws://ops.koreainvestment.com:31000",
                serviceStatus = "running",
                timestamp = DateTime.UtcNow,
                note = "KIS WebSocket 연결 상태 - 실제 데이터 수신 여부 확인 필요"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KIS 연결 상태 확인 중 오류");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("debug/force-kis-data")]
    public async Task<IActionResult> ForceKisDataSimulation()
    {
        try
        {
            _logger.LogInformation("🧪 [Debug] KIS 데이터 시뮬레이션 시작");
        
            // KIS 형태의 파이프 구분 메시지 시뮬레이션
            var kisSimulatedMessage = "0|H0STCNT0|1|005930^090000^76500^2^500^0.66^76500^76000^77000^75500^76400^76600^150000^5000000^380000000000";
        
            // RealTimeDataProcessor에 직접 메시지 전달
            var processor = HttpContext.RequestServices.GetService<IRealTimeDataProcessor>();
        
            if (processor != null)
            {
                processor.ProcessMessage(kisSimulatedMessage);
                _logger.LogInformation("✅ [Debug] KIS 시뮬레이션 메시지 처리 완료");
            
                return Ok(new 
                { 
                    message = "KIS 데이터 시뮬레이션 완료",
                    simulatedMessage = kisSimulatedMessage,
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                return BadRequest("RealTimeDataProcessor를 찾을 수 없습니다.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Debug] KIS 데이터 시뮬레이션 실패");
            return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }
    
    [HttpPost("debug/test-processor")]
    public async Task<IActionResult> TestProcessorDirectly()
    {
        try
        {
            _logger.LogInformation("🧪 [Debug] Processor 직접 테스트 시작");
        
            var processor = HttpContext.RequestServices.GetService<IRealTimeDataProcessor>();
        
            if (processor == null)
            {
                return BadRequest("RealTimeDataProcessor를 찾을 수 없습니다.");
            }
        
            // KIS 형태의 실제 메시지 시뮬레이션
            var kisMessage = "0|H0STCNT0|1|005930^153000^76800^2^300^0.39^76800^76500^77200^76200^76750^76850^45000^3500000^268800000000";
        
            _logger.LogInformation("📤 [Debug] Processor에 메시지 전달: {Message}", kisMessage);
        
            // 메시지 처리
            processor.ProcessMessage(kisMessage);
        
            _logger.LogInformation("✅ [Debug] Processor 테스트 완료");
        
            return Ok(new 
            { 
                message = "Processor 직접 테스트 완료",
                kisMessage = kisMessage,
                timestamp = DateTime.UtcNow,
                note = "이 테스트는 KIS 메시지를 직접 처리하여 브로드캐스터까지 연결되는지 확인합니다."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Debug] Processor 테스트 실패");
            return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }
    
    [HttpPost("debug/test-full-pipeline")]
    public async Task<IActionResult> TestFullPipeline(string symbol = "005930")
    {
        try
        {
            _logger.LogInformation("🧪 [Debug] 전체 파이프라인 테스트 시작: {Symbol}", symbol);
        
            // 1. Processor 직접 테스트
            var processor = HttpContext.RequestServices.GetService<IRealTimeDataProcessor>();
            if (processor == null)
            {
                return BadRequest("RealTimeDataProcessor를 찾을 수 없습니다.");
            }
        
            // 2. KIS 형태의 파이프 메시지 생성 (실제 형태와 동일)
            var currentTime = DateTime.Now.ToString("HHmmss");
            var kisMessage = $"0|H0STCNT0|1|{symbol}^{currentTime}^76800^2^300^0.39^76800^76500^77200^76200^76750^76850^45000^3500000^268800000000";
        
            _logger.LogInformation("📤 [Debug] KIS 시뮬레이션 메시지: {Message}", kisMessage);
        
            // 3. Processor로 메시지 처리
            processor.ProcessMessage(kisMessage);
        
            _logger.LogInformation("✅ [Debug] 파이프라인 테스트 완료");
        
            return Ok(new 
            { 
                message = "전체 파이프라인 테스트 완료",
                kisMessage = kisMessage,
                symbol = symbol,
                timestamp = DateTime.UtcNow,
                note = "이 테스트는 KIS 메시지 → Processor → Broadcaster → SignalR 전체 흐름을 확인합니다."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Debug] 파이프라인 테스트 실패");
            return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }
}