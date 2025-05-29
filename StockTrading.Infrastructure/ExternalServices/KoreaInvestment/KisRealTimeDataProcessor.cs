using System.Text.Json;
using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.External.KoreaInvestment;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.Services;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Constants;
using static StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Constants.KisRealTimeConstants.MessageTypes;

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

        // 성공/오류 응답 체크
        if (root.TryGetProperty("body", out var body) &&
            body.TryGetProperty("rt_cd", out var rtCd))
        {
            var returnCode = rtCd.GetString();
            if (returnCode == "0") return;
            var msg1 = body.TryGetProperty("msg1", out var msg1Element) ? msg1Element.GetString() : "";


            _logger.LogWarning("KIS API 오류: {Message}", msg1);
            return;
        }

        // 실시간 데이터 메시지 처리
        if (!root.TryGetProperty("header", out var header) ||
            !header.TryGetProperty("tr_id", out var trId)) return;
        var trIdValue = trId.GetString();

        switch (trIdValue)
        {
            case PingPong:
                // 연결 상태 확인용, 별도 처리 불필요
                break;
            case StockAskBid:
            case StockExecution:
                ProcessStockPrice(root);
                break;

            case TradeNotification:
            case TradeNotificationDemo:
                ProcessTradeExecution(root);
                break;
        }
    }

    private void ProcessStockPrice(JsonElement root)
    {
        if (!root.TryGetProperty("body", out var body)) return;

        // 필수 필드 확인
        if (!body.TryGetProperty("mksc_shrn_iscd", out var stockCodeElement) ||
            !body.TryGetProperty("stck_prpr", out var priceElement))
            return;

        var stockCode = stockCodeElement.GetString();
        var currentPrice = decimal.Parse(priceElement.GetString());
        var change = body.TryGetProperty("prdy_vrss", out var changeElement)
            ? decimal.Parse(changeElement.GetString())
            : 0;
        var changeRate = body.TryGetProperty("prdy_ctrt", out var rateElement)
            ? decimal.Parse(rateElement.GetString())
            : 0;
        var volume = body.TryGetProperty("acml_vol", out var volumeElement)
            ? long.Parse(volumeElement.GetString())
            : 0;

        var priceData = new KisTransactionInfo
        {
            Symbol = stockCode,
            Price = currentPrice,
            PriceChange = change,
            ChangeType = change >= 0 ? KisRealTimeConstants.ChangeTypes.Rise : KisRealTimeConstants.ChangeTypes.Fall,
            TransactionTime = DateTime.Now,
            Volume = (int)volume
        };

        StockPriceReceived?.Invoke(this, priceData);
        _logger.LogInformation("실시간 주가: {Symbol} {Price}원", stockCode, currentPrice);
    }

    private void ProcessTradeExecution(JsonElement root)
    {
        if (!root.TryGetProperty("body", out var body)) return;

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

        TradeExecutionReceived?.Invoke(this, executionData);
        _logger.LogInformation("체결: {Symbol} {Quantity}주 {Price}원", stockCode, quantity, price);
    }
}