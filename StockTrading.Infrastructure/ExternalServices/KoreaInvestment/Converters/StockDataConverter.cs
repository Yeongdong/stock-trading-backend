using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.DTOs.Trading.Inquiry;
using StockTrading.Application.DTOs.Trading.Orders;
using StockTrading.Domain.Settings;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Constants;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Converters;

/// <summary>
/// 주식 데이터 변환기
/// </summary>
public class StockDataConverter
{
    private readonly ILogger<StockDataConverter> _logger;

    public StockDataConverter(ILogger<StockDataConverter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 필드 배열을 KisTransactionInfo로 변환
    /// </summary>
    public KisTransactionInfo? ConvertToTransactionInfo(string[] fields, int recordIndex)
    {
        var stockCode = fields[KisRealTimeConstants.FieldIndices.StockCode];

        if (!IsValidStockCode(stockCode))
        {
            _logger.LogDebug("레코드 {Index}: 유효하지 않은 종목코드: {StockCode}", recordIndex, stockCode);
            return null;
        }

        var priceData = CreateTransactionInfo(fields, stockCode);

        _logger.LogDebug("레코드 {Index}: 변환 성공 - 종목: {Symbol}, 현재가: {Price}원",
            recordIndex, stockCode, priceData.Price);

        return priceData;
    }

    /// <summary>
    /// KIS 현재가 조회 응답을 Application DTO로 변환
    /// </summary>
    public CurrentPriceResponse ConvertToStockPriceResponse(KisCurrentPriceData kisData, string stockCode)
    {
        return new CurrentPriceResponse
        {
            StockCode = stockCode,
            StockName = kisData.StockName,
            CurrentPrice = ParseDecimalSafely(kisData.CurrentPrice),
            PriceChange = ParseDecimalSafely(kisData.PriceChange),
            ChangeRate = ParseDecimalSafely(kisData.ChangeRate),
            ChangeType = ConvertChangeType(kisData.ChangeSign),
            OpenPrice = ParseDecimalSafely(kisData.OpenPrice),
            HighPrice = ParseDecimalSafely(kisData.HighPrice),
            LowPrice = ParseDecimalSafely(kisData.LowPrice),
            Volume = ParseLongSafely(kisData.Volume),
            InquiryTime = DateTime.Now
        };
    }

    public string ConvertOrderTypeToKisCode(string orderType, KisApiSettings settings)
    {
        return orderType switch
        {
            "01" => settings.Defaults.SellOrderCode, // 매도
            "02" => settings.Defaults.BuyOrderCode, // 매수  
            _ => settings.Defaults.AllOrderCode // 전체
        };
    }

    public OrderExecutionInquiryResponse ConvertToOrderExecutionResponse(
        KisOrderExecutionInquiryResponse kisResponse)
    {
        var executionItems = kisResponse.ExecutionItems.Select(item => new OrderExecutionItem
        {
            OrderDate = item.OrderDate,
            OrderNumber = item.OrderNumber,
            StockCode = item.StockCode,
            StockName = item.StockName,
            OrderSide = item.SellBuyDivisionName,
            OrderQuantity = ParseIntSafely(item.OrderQuantity),
            OrderPrice = ParseDecimalSafely(item.OrderPrice),
            ExecutedQuantity = ParseIntSafely(item.TotalExecutedQuantity),
            ExecutedPrice = ParseDecimalSafely(item.AveragePrice),
            ExecutedAmount = ParseDecimalSafely(item.TotalExecutedAmount),
            OrderStatus = item.OrderStatusName,
            ExecutionTime = item.OrderTime
        }).ToList();

        return new OrderExecutionInquiryResponse
        {
            ExecutionItems = executionItems,
            TotalCount = executionItems.Count,
            HasMore = !string.IsNullOrEmpty(kisResponse.CtxAreaNk100)
        };
    }

    public BuyableInquiryResponse ConvertToBuyableInquiryResponse(KisBuyableInquiryData kisData,
        decimal orderPrice, string stockCode)
    {
        var buyableAmount = ParseDecimalSafely(kisData.BuyableAmount);
        var calculatedQuantity = orderPrice > 0 ? (int)(buyableAmount / orderPrice) : 0;

        return new BuyableInquiryResponse
        {
            StockCode = stockCode,
            StockName = $"종목{stockCode}",
            BuyableAmount = buyableAmount,
            BuyableQuantity = Math.Max(calculatedQuantity, ParseIntSafely(kisData.BuyableQuantity)),
            OrderableAmount = buyableAmount,
            CashBalance = buyableAmount,
            OrderPrice = orderPrice,
            CurrentPrice = ParseDecimalSafely(kisData.CalculationPrice),
            UnitQuantity = 1
        };
    }

    private KisTransactionInfo CreateTransactionInfo(string[] fields, string stockCode)
    {
        var tradeTime = fields[KisRealTimeConstants.FieldIndices.TradeTime];
        var currentPrice = ParseDecimalSafely(fields[KisRealTimeConstants.FieldIndices.CurrentPrice]);
        var changeSign = fields[KisRealTimeConstants.FieldIndices.ChangeSign];
        var priceChange = ParseDecimalSafely(fields[KisRealTimeConstants.FieldIndices.PriceChange]);
        var changeRate = ParseDecimalSafely(fields[KisRealTimeConstants.FieldIndices.ChangeRate]);
        var openPrice = ParseDecimalSafely(fields[KisRealTimeConstants.FieldIndices.OpenPrice]);
        var highPrice = ParseDecimalSafely(fields[KisRealTimeConstants.FieldIndices.HighPrice]);
        var lowPrice = ParseDecimalSafely(fields[KisRealTimeConstants.FieldIndices.LowPrice]);
        var volume = ParseLongSafely(fields[KisRealTimeConstants.FieldIndices.Volume]);
        var totalVolume = ParseLongSafely(fields[KisRealTimeConstants.FieldIndices.TotalVolume]);

        return new KisTransactionInfo
        {
            Symbol = stockCode,
            Price = currentPrice,
            PriceChange = priceChange,
            ChangeType = ConvertChangeType(changeSign),
            TransactionTime = ParseTradeTime(tradeTime),
            ChangeRate = changeRate,
            Volume = (int)Math.Min(volume, int.MaxValue),
            TotalVolume = totalVolume,
            OpenPrice = openPrice,
            HighPrice = highPrice,
            LowPrice = lowPrice
        };
    }

    private string ConvertChangeType(string changeSign)
    {
        if (KisRealTimeConstants.ChangeTypes.RiseCodes.Contains(changeSign))
            return KisRealTimeConstants.ChangeTypes.Rise;

        return KisRealTimeConstants.ChangeTypes.FallCodes.Contains(changeSign)
            ? KisRealTimeConstants.ChangeTypes.Fall
            : KisRealTimeConstants.ChangeTypes.Unchanged;
    }

    private DateTime ParseTradeTime(string tradeTime)
    {
        if (tradeTime.Length != KisRealTimeConstants.Parsing.TradeTimeLength) return DateTime.Now;

        var hour = int.Parse(tradeTime[..2]);
        var minute = int.Parse(tradeTime.Substring(2, 2));
        var second = int.Parse(tradeTime.Substring(4, 2));

        return DateTime.Today
            .AddHours(hour)
            .AddMinutes(minute)
            .AddSeconds(second);
    }

    private long ParseLongSafely(string value)
    {
        if (decimal.TryParse(value, out var decimalResult))
            return (long)decimalResult;

        return 0L;
    }

    private int ParseIntSafely(string value)
    {
        return int.TryParse(value, out var result) ? result : 0;
    }

    private decimal ParseDecimalSafely(string value)
    {
        return decimal.TryParse(value, out var result) ? result : 0m;
    }

    private bool IsValidStockCode(string stockCode)
    {
        return !string.IsNullOrWhiteSpace(stockCode) &&
               stockCode.Length == KisRealTimeConstants.Parsing.StockCodeLength &&
               stockCode.All(char.IsDigit);
    }
}