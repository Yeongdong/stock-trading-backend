using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.DTOs.Trading.Inquiry;
using StockTrading.Application.DTOs.Trading.Orders;
using StockTrading.Domain.Settings;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Converters;

public class StockDataConverter
{
    private readonly ILogger<StockDataConverter> _logger;
    private readonly RealTimeDataSettings _realTimeSettings;

    public StockDataConverter(ILogger<StockDataConverter> logger, IOptions<RealTimeDataSettings> realTimeSettings)
    {
        _logger = logger;
        _realTimeSettings = realTimeSettings.Value;
    }

    public KisTransactionInfo? ConvertToTransactionInfo(string[] fields, int recordIndex)
    {
        var parsing = _realTimeSettings.Parsing;
        var stockCode = fields[FieldIndices.StockCode];

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

    public string ConvertOrderTypeToKisCode(string orderType, KoreaInvestmentSettings settings)
    {
        var defaults = settings.DefaultValues;
        return orderType switch
        {
            "01" => defaults.SellOrderCode, // 매도
            "02" => defaults.BuyOrderCode, // 매수  
            _ => defaults.AllOrderCode // 전체
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

    #region Private Helper Methods

    private KisTransactionInfo CreateTransactionInfo(string[] fields, string stockCode)
    {
        var tradeTime = fields[FieldIndices.TradeTime];
        var currentPrice = ParseDecimalSafely(fields[FieldIndices.CurrentPrice]);
        var changeSign = fields[FieldIndices.ChangeSign];
        var priceChange = ParseDecimalSafely(fields[FieldIndices.PriceChange]);
        var changeRate = ParseDecimalSafely(fields[FieldIndices.ChangeRate]);
        var openPrice = ParseDecimalSafely(fields[FieldIndices.OpenPrice]);
        var highPrice = ParseDecimalSafely(fields[FieldIndices.HighPrice]);
        var lowPrice = ParseDecimalSafely(fields[FieldIndices.LowPrice]);
        var volume = ParseLongSafely(fields[FieldIndices.Volume]);
        var totalVolume = ParseLongSafely(fields[FieldIndices.TotalVolume]);

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
        var riseCode = new[] { "1", "2" };
        var fallCodes = new[] { "4", "5" };

        if (riseCode.Contains(changeSign))
            return "상승";

        return fallCodes.Contains(changeSign) ? "하락" : "보합";
    }

    private DateTime ParseTradeTime(string tradeTime)
    {
        var parsing = _realTimeSettings.Parsing;

        if (tradeTime.Length != parsing.TradeTimeLength)
            return DateTime.Now;

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
        var parsing = _realTimeSettings.Parsing;
        return !string.IsNullOrWhiteSpace(stockCode) &&
               stockCode.Length == parsing.StockCodeLength &&
               stockCode.All(char.IsDigit);
    }

    #endregion

    #region Field Indices (H0STCNT0 기준)

    private static class FieldIndices
    {
        public const int StockCode = 0;
        public const int TradeTime = 1;
        public const int CurrentPrice = 2;
        public const int ChangeSign = 3;
        public const int PriceChange = 4;
        public const int ChangeRate = 5;
        public const int WeightedAvgPrice = 6;
        public const int OpenPrice = 7;
        public const int HighPrice = 8;
        public const int LowPrice = 9;
        public const int AskPrice1 = 10;
        public const int BidPrice1 = 11;
        public const int Volume = 12;
        public const int TotalVolume = 13;
        public const int TotalTradeAmount = 14;
    }

    #endregion
}