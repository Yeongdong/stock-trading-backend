using System.Text.Json;
using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.External.KoreaInvestment;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.Services;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Constants;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

/*
 * 수신된 메시지 처리 및 파싱
 */
public class KisRealTimeDataProcessor : IKisRealTimeDataProcessor
{
    private readonly ILogger<KisRealTimeDataProcessor> _logger;

    public event EventHandler<KisTransactionInfo> StockPriceReceived;
    public event EventHandler<object> TradeExecutionReceived;

    public KisRealTimeDataProcessor(ILogger<KisRealTimeDataProcessor> logger)
    {
        _logger = logger;
    }

    public void ProcessMessage(string messageJson)
    {
        var jsonDoc = JsonDocument.Parse(messageJson);
        var root = jsonDoc.RootElement;

        if (root.TryGetProperty("header", out var header) &&
            header.TryGetProperty("tr_id", out var trId))
        {
            string trIdValue = trId.GetString();
            _logger.LogInformation($"메시지 유형: {trIdValue}");

            switch (trIdValue)
            {
                case KisRealTimeConstants.MessageTypes.StockAskBid:
                case KisRealTimeConstants.MessageTypes.StockExecution:
                    ProcessStockPrice(root);
                    break;

                case KisRealTimeConstants.MessageTypes.TradeNotification:
                case KisRealTimeConstants.MessageTypes.TradeNotificationDemo:
                    ProcessTradeExecution(root);
                    break;

                case KisRealTimeConstants.MessageTypes.PingPong:
                    // PING 메시지는 이벤트 발생 X (별도 처리)
                    break;

                default:
                    _logger.LogWarning("알 수 없는 메시지 타입: {TrId}", trIdValue);
                    break;
            }
        }
        else
        {
            _logger.LogWarning("메시지에 tr_id가 없습니다.");
        }
    }

    private void ProcessStockPrice(JsonElement root)
    {
        if (!root.TryGetProperty("body", out var body)) return;
        // 종목코드
        var stockCode = body.GetProperty("mksc_shrn_iscd").GetString();
        // 현재가
        var currentPrice = decimal.Parse(body.GetProperty("stck_prpr").GetString());
        // 전일 대비
        var change = decimal.Parse(body.GetProperty("prdy_vrss").GetString());
        // 등락률
        var changeRate = decimal.Parse(body.GetProperty("prdy_ctrt").GetString());
        // 거래량
        var volume = long.Parse(body.GetProperty("acml_vol").GetString());

        // 실시간 가격 정보 객체 생성
        var priceData = new KisTransactionInfo
        {
            Symbol = stockCode,
            Price = currentPrice,
            PriceChange = change,
            ChangeType = change >= 0 ? KisRealTimeConstants.ChangeTypes.Rise : KisRealTimeConstants.ChangeTypes.Fall,
            TransactionTime = DateTime.Now,
            Volume = (int)volume
        };

        OnStockPriceReceived(priceData);
        _logger.LogInformation($"실시간 시세 처리: {stockCode}, 가격: {currentPrice}");
    }

    private void ProcessTradeExecution(JsonElement root)
    {
        if (!root.TryGetProperty("body", out var body)) return;
        _logger.LogInformation("체결 정보 처리 시작");

        var orderId = body.GetProperty("odno").GetString();
        var stockCode = body.GetProperty("pdno").GetString();
        var quantity = int.Parse(body.GetProperty("cntg_qty").GetString());
        var price = decimal.Parse(body.GetProperty("cntg_pric").GetString());

        var executionData = new
        {
            OrderId = orderId,
            StockCode = stockCode,
            Quantity = quantity,
            Price = price,
            ExecutionTime = DateTime.Now
        };

        OnTradeExecutionReceived(executionData);
        _logger.LogInformation($"체결 정보 처리: 주문번호 {orderId}, 종목 {stockCode}");
    }

    protected virtual void OnStockPriceReceived(KisTransactionInfo data)
    {
        _logger.LogDebug("StockPriceReceived 이벤트 발생");
        StockPriceReceived?.Invoke(this, data);
    }

    protected virtual void OnTradeExecutionReceived(object data)
    {
        _logger.LogDebug("TradeExecutionReceived 이벤트 발생");
        TradeExecutionReceived?.Invoke(this, data);
    }
}