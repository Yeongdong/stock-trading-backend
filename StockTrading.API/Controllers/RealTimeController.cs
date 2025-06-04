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

    public RealTimeController(IRealTimeService realTimeService, IUserContextService userContextService,
        ILogger<RealTimeController> logger) : base(userContextService)
    {
        _realTimeService = realTimeService;
        _logger = logger;
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

    [HttpPost("debug/test-kis-message")]
    public async Task<IActionResult> TestKisMessage(string symbol = "005930")
    {
        try
        {
            _logger.LogInformation("🧪 [Debug] KIS 메시지 시뮬레이션 시작: {Symbol}", symbol);

            // 실제 KIS WebSocket에서 오는 메시지 형태 시뮬레이션
            // 1. JSON 형태 (구독 응답)
            var subscriptionResponse = @"{
            ""header"": {
                ""tr_id"": ""H0STASP0"",
                ""tr_key"": """ + symbol + @""",
                ""encrypt"": ""N""
            },
            ""body"": {
                ""rt_cd"": ""0"",
                ""msg_cd"": ""MCA00000"",
                ""msg1"": ""SUBSCRIBE SUCCESS""
            }
        }";

            // 2. 파이프 구분 형태 (실제 실시간 데이터)
            var currentTime = DateTime.Now.ToString("HHmmss");
            var realTimeData =
                $"0|H0STCNT0|1|{symbol}^{currentTime}^76800^2^300^0.39^76800^76500^77200^76200^76750^76850^45000^3500000^268800000000";

            var processor = HttpContext.RequestServices.GetService<IRealTimeDataProcessor>();
            if (processor == null)
            {
                return BadRequest("RealTimeDataProcessor를 찾을 수 없습니다.");
            }

            _logger.LogInformation("📤 [Debug] 구독 응답 처리: {Message}", subscriptionResponse);
            processor.ProcessMessage(subscriptionResponse);

            await Task.Delay(500); // 메시지 처리 간격

            _logger.LogInformation("📤 [Debug] 실시간 데이터 처리: {Message}", realTimeData);
            processor.ProcessMessage(realTimeData);

            _logger.LogInformation("✅ [Debug] KIS 메시지 시뮬레이션 완료");

            return Ok(new
            {
                message = "KIS 메시지 시뮬레이션 완료",
                subscriptionResponse = subscriptionResponse,
                realTimeData = realTimeData,
                symbol = symbol,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Debug] KIS 메시지 시뮬레이션 실패");
            return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpPost("debug/test-websocket-message")]
    public async Task<IActionResult> TestWebSocketMessage()
    {
        try
        {
            _logger.LogInformation("🧪 [Debug] WebSocket 메시지 직접 주입 시작");

            var webSocketClient = HttpContext.RequestServices.GetService<IWebSocketClient>();
            if (webSocketClient == null)
            {
                return BadRequest("WebSocketClient를 찾을 수 없습니다.");
            }

            // WebSocketClient의 MessageReceived 이벤트를 직접 발생시키기
            var testMessage = @"{
            ""header"": {
                ""tr_id"": ""H0STCNT0"",
                ""tr_key"": ""005930""
            },
            ""body"": {
                ""mksc_shrn_iscd"": ""005930"",
                ""stck_prpr"": ""76800"",
                ""prdy_vrss"": ""300"",
                ""prdy_ctrt"": ""0.39""
            }
        }";

            _logger.LogInformation("📤 [Debug] 테스트 메시지 주입: {Message}", testMessage);

            // Reflection을 사용해서 MessageReceived 이벤트를 직접 발생시킴
            var messageReceivedField = webSocketClient.GetType()
                .GetEvent("MessageReceived");

            if (messageReceivedField != null)
            {
                // 이벤트 핸들러 직접 호출
                var eventField = webSocketClient.GetType()
                    .GetField("MessageReceived",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (eventField?.GetValue(webSocketClient) is EventHandler<string> eventHandler)
                {
                    eventHandler.Invoke(webSocketClient, testMessage);
                    _logger.LogInformation("✅ [Debug] MessageReceived 이벤트 직접 발생 완료");
                }
                else
                {
                    _logger.LogWarning("⚠️ [Debug] MessageReceived 이벤트 핸들러를 찾을 수 없음");
                }
            }

            return Ok(new
            {
                message = "WebSocket 메시지 직접 주입 완료",
                testMessage = testMessage,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Debug] WebSocket 메시지 주입 실패");
            return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpGet("debug/check-services")]
    public IActionResult CheckServices()
    {
        try
        {
            var services = new List<object>();

            // 각 서비스가 제대로 등록되었는지 확인
            var webSocketClient = HttpContext.RequestServices.GetService<IWebSocketClient>();
            var processor = HttpContext.RequestServices.GetService<IRealTimeDataProcessor>();
            var broadcaster = HttpContext.RequestServices.GetService<IRealTimeDataBroadcaster>();
            var subscriptionManager = HttpContext.RequestServices.GetService<ISubscriptionManager>();
            var realTimeService = HttpContext.RequestServices.GetService<IRealTimeService>();

            _logger.LogInformation("🔍 [Debug] 서비스 상태 확인:");
            _logger.LogInformation("- WebSocketClient: {Status}", webSocketClient != null ? "등록됨" : "누락");
            _logger.LogInformation("- RealTimeDataProcessor: {Status}", processor != null ? "등록됨" : "누락");
            _logger.LogInformation("- RealTimeDataBroadcaster: {Status}", broadcaster != null ? "등록됨" : "누락");
            _logger.LogInformation("- SubscriptionManager: {Status}", subscriptionManager != null ? "등록됨" : "누락");
            _logger.LogInformation("- RealTimeService: {Status}", realTimeService != null ? "등록됨" : "누락");

            return Ok(new
            {
                services = new
                {
                    webSocketClient = webSocketClient != null,
                    processor = processor != null,
                    broadcaster = broadcaster != null,
                    subscriptionManager = subscriptionManager != null,
                    realTimeService = realTimeService != null
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Debug] 서비스 확인 중 오류");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("debug/test-various-subscriptions/{symbol}")]
    public async Task<IActionResult> TestVariousSubscriptions(string symbol = "005930")
    {
        try
        {
            var subscriptionManager = HttpContext.RequestServices.GetService<ISubscriptionManager>();
            if (subscriptionManager == null)
            {
                return BadRequest("SubscriptionManager를 찾을 수 없습니다.");
            }

            _logger.LogInformation("🧪 [Debug] 다양한 구독 방식 테스트 시작: {Symbol}", symbol);

            // SubscriptionManager의 TestVariousSubscriptionsAsync 메서드 호출
            var method = subscriptionManager.GetType().GetMethod("TestVariousSubscriptionsAsync");
            if (method != null)
            {
                await (Task)method.Invoke(subscriptionManager, new object[] { symbol });
            }

            return Ok(new
            {
                message = "다양한 구독 방식 테스트 완료",
                symbol = symbol,
                timestamp = DateTime.UtcNow,
                note = "H0STCNT0, H0STASP0, H0STCNI0 순서로 테스트"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Debug] 다양한 구독 테스트 실패");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("debug/wait-for-kis-data/{symbol}")]
    public async Task<IActionResult> WaitForKisData(string symbol = "005930", int waitSeconds = 30)
    {
        try
        {
            _logger.LogInformation("⏰ [Debug] KIS 데이터 대기 시작: {Symbol}, {Seconds}초", symbol, waitSeconds);

            // 실제 구독 실행
            await _realTimeService.SubscribeSymbolAsync(symbol);

            _logger.LogInformation("📡 [Debug] 구독 완료. {Seconds}초 동안 데이터 대기 중...", waitSeconds);

            // 지정된 시간만큼 대기하면서 로그 모니터링
            for (int i = 0; i < waitSeconds; i++)
            {
                await Task.Delay(1000);

                if (i % 5 == 0) // 5초마다 상태 로그
                {
                    _logger.LogInformation("⏳ [Debug] 대기 중... {Current}/{Total}초", i + 1, waitSeconds);
                }
            }

            _logger.LogInformation("✅ [Debug] 대기 완료. KIS 데이터 수신 여부를 로그에서 확인하세요.");

            return Ok(new
            {
                message = "KIS 데이터 대기 완료",
                symbol = symbol,
                waitedSeconds = waitSeconds,
                timestamp = DateTime.UtcNow,
                note = "실제 KIS 데이터가 수신되었는지 로그를 확인하세요."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Debug] KIS 데이터 대기 중 오류");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("debug/subscription-status")]
    public IActionResult GetSubscriptionStatus()
    {
        try
        {
            var subscribedSymbols = _realTimeService.GetSubscribedSymbols();

            _logger.LogInformation("📊 [Debug] 현재 구독 상태 확인");
            _logger.LogInformation("- 구독 중인 종목 수: {Count}", subscribedSymbols.Count);

            foreach (var symbol in subscribedSymbols)
            {
                _logger.LogInformation("- 구독 종목: {Symbol}", symbol);
            }

            return Ok(new
            {
                subscribedSymbols = subscribedSymbols,
                subscribedCount = subscribedSymbols.Count,
                timestamp = DateTime.UtcNow,
                status = subscribedSymbols.Count > 0 ? "구독 중" : "구독 없음"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Debug] 구독 상태 확인 중 오류");
            return BadRequest(new { error = ex.Message });
        }
    }
}