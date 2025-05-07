using System.Text.Json;
using Microsoft.Extensions.Logging;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Models;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

/*
 * 수신된 메시지 처리 및 파싱
 */
public class KisRealTimeDataProcessor
{
    private readonly ILogger<KisRealTimeDataProcessor> _logger;

    // 이벤트를 통해 파싱된 실시간 데이터 전달
    public event EventHandler<StockTransaction> StockPriceReceived;
    public event EventHandler<object> TradeExecutionReceived;

    public KisRealTimeDataProcessor(ILogger<KisRealTimeDataProcessor> logger)
    {
        _logger = logger;
    }

    public void ProcessMessage(string messageJson)
    {
        try
        {
            _logger.LogDebug($"수신된 WebSocket 메시지: {messageJson}");

            var jsonDoc = JsonDocument.Parse(messageJson);
            var root = jsonDoc.RootElement;

            // 메시지 타입 확인
            if (root.TryGetProperty("header", out var header) &&
                header.TryGetProperty("tr_id", out var trId))
            {
                string trIdValue = trId.GetString();

                switch (trIdValue)
                {
                    case "H0STASP0":
                    case "H0STCNT0":
                        ProcessStockPrice(root);
                        break;
                    case "H0STCNI0":
                    case "H0STCNI9":
                        ProcessTradeExecution(root);
                        break;
                    case "PINGPONG":
                        break;
                    default:
                        _logger.LogWarning($"알 수 없는 메시지 타입: {trIdValue}");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket 메시지 처리 오류");
        }
    }

    private void ProcessStockPrice(JsonElement root)
    {
        try
        {
            if (root.TryGetProperty("body", out var body))
            {
                // 종목코드
                string stockCode = body.GetProperty("mksc_shrn_iscd").GetString();
                // 현재가
                decimal currentPrice = decimal.Parse(body.GetProperty("stck_prpr").GetString());
                // 전일 대비
                decimal change = decimal.Parse(body.GetProperty("prdy_vrss").GetString());
                // 등락률
                decimal changeRate = decimal.Parse(body.GetProperty("prdy_ctrt").GetString());
                // 거래량
                long volume = long.Parse(body.GetProperty("acml_vol").GetString());

                // 실시간 가격 정보 객체 생성
                var priceData = new StockTransaction
                {
                    Symbol = stockCode,
                    Price = currentPrice,
                    PriceChange = change,
                    ChangeType = change >= 0 ? "상승" : "하락",
                    TransactionTime = DateTime.Now,
                    Volume = (int)volume
                };

                // 이벤트 발생
                OnStockPriceReceived(priceData);
                _logger.LogInformation($"실시간 시세 처리: {stockCode}, 가격: {currentPrice}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "실시간 시세 처리 오류");
        }
    }

    private void ProcessTradeExecution(JsonElement root)
    {
        try
        {
            if (root.TryGetProperty("body", out var body))
            {
                // 체결 정보 처리
                string orderId = body.GetProperty("odno").GetString();
                string stockCode = body.GetProperty("pdno").GetString();
                int quantity = int.Parse(body.GetProperty("execqty").GetString());
                decimal price = decimal.Parse(body.GetProperty("execprc").GetString());

                // 체결 정보 객체 생성
                var executionData = new
                {
                    OrderId = orderId,
                    StockCode = stockCode,
                    Quantity = quantity,
                    Price = price,
                    ExecutionTime = DateTime.Now
                };

                // 이벤트 발생
                OnTradeExecutionReceived(executionData);
                _logger.LogInformation($"체결 정보 처리: 주문번호 {orderId}, 종목 {stockCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "체결 정보 처리 오류");
        }
    }

    protected virtual void OnStockPriceReceived(StockTransaction data)
    {
        StockPriceReceived?.Invoke(this, data);
    }

    protected virtual void OnTradeExecutionReceived(object data)
    {
        TradeExecutionReceived?.Invoke(this, data);
    }
}