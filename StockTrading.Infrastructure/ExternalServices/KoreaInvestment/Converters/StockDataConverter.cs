using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.DTOs.Trading.Inquiry;
using StockTrading.Application.DTOs.Trading.Orders;
using StockTrading.Domain.Settings;
using StockTrading.Infrastructure.Utilities;

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
        var stockCode = fields[ParsingSettings.FieldIndices.StockCode];

        if (!ParseHelper.IsValidStockCode(stockCode, _realTimeSettings.Parsing.StockCodeLength))
        {
            _logger.LogDebug("레코드 {Index}: 유효하지 않은 종목코드: {StockCode}", recordIndex, stockCode);
            return null;
        }

        var priceData = CreateTransactionInfo(fields, stockCode);

        _logger.LogDebug("레코드 {Index}: 변환 성공 - 종목: {Symbol}, 현재가: {Price}원", recordIndex, stockCode, priceData.Price);

        return priceData;
    }

    public KisCurrentPriceResponse ConvertToStockPriceResponse(KisCurrentPriceData kisData, string stockCode)
    {
        return new KisCurrentPriceResponse
        {
            StockCode = stockCode,
            StockName = kisData.StockName,
            CurrentPrice = ParseHelper.ParseDecimalSafely(kisData.CurrentPrice),
            PriceChange = ParseHelper.ParseDecimalSafely(kisData.PriceChange),
            ChangeRate = ParseHelper.ParseDecimalSafely(kisData.ChangeRate),
            ChangeType = ConvertChangeType(kisData.ChangeSign),
            OpenPrice = ParseHelper.ParseDecimalSafely(kisData.OpenPrice),
            HighPrice = ParseHelper.ParseDecimalSafely(kisData.HighPrice),
            LowPrice = ParseHelper.ParseDecimalSafely(kisData.LowPrice),
            Volume = ParseHelper.ParseLongSafely(kisData.Volume),
            InquiryTime = DateTime.Now
        };
    }

    public string ConvertOrderTypeToKisCode(string orderType, KoreaInvestmentSettings settings)
    {
        var defaults = settings.DefaultValues;
        return orderType switch
        {
            "01" => defaults.SellOrderCode,
            "02" => defaults.BuyOrderCode,
            _ => defaults.AllOrderCode
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
            OrderQuantity = ParseHelper.ParseIntSafely(item.OrderQuantity),
            OrderPrice = ParseHelper.ParseDecimalSafely(item.OrderPrice),
            ExecutedQuantity = ParseHelper.ParseIntSafely(item.TotalExecutedQuantity),
            ExecutedPrice = ParseHelper.ParseDecimalSafely(item.AveragePrice),
            ExecutedAmount = ParseHelper.ParseDecimalSafely(item.TotalExecutedAmount),
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
        var buyableAmount = ParseHelper.ParseDecimalSafely(kisData.BuyableAmount);
        var calculatedQuantity = orderPrice > 0 ? (int)(buyableAmount / orderPrice) : 0;

        return new BuyableInquiryResponse
        {
            StockCode = stockCode,
            StockName = $"종목{stockCode}",
            BuyableAmount = buyableAmount,
            BuyableQuantity = Math.Max(calculatedQuantity, ParseHelper.ParseIntSafely(kisData.BuyableQuantity)),
            OrderableAmount = buyableAmount,
            CashBalance = buyableAmount,
            OrderPrice = orderPrice,
            CurrentPrice = ParseHelper.ParseDecimalSafely(kisData.CalculationPrice),
            UnitQuantity = 1
        };
    }

    public PeriodPriceResponse ConvertToPeriodPriceResponse(KisPeriodPriceResponse kisData, string stockCode)
    {
        var currentInfo = kisData.CurrentInfo;

        return new PeriodPriceResponse
        {
            StockCode = stockCode,
            StockName = currentInfo?.StockName ?? string.Empty,
            CurrentPrice = ConvertToDecimal(currentInfo?.CurrentPrice),
            PriceChange = ConvertToDecimal(currentInfo?.PriceChange),
            ChangeRate = ConvertToDecimal(currentInfo?.ChangeRate),
            ChangeSign = currentInfo?.ChangeSign ?? string.Empty,
            TotalVolume = ConvertToLong(currentInfo?.AccumulatedVolume),
            TotalTradingValue = ConvertToLong(currentInfo?.AccumulatedAmount),
            PriceData = kisData.PriceData.Select(ConvertToPeriodPriceData).ToList()
        };
    }

    #region Private Helper Methods

    private KisTransactionInfo CreateTransactionInfo(string[] fields, string stockCode)
    {
        var tradeTime = fields[ParsingSettings.FieldIndices.TradeTime];
        var currentPrice = ParseHelper.ParseDecimalSafely(fields[ParsingSettings.FieldIndices.CurrentPrice]);
        var changeSign = fields[ParsingSettings.FieldIndices.ChangeSign];
        var priceChange = ParseHelper.ParseDecimalSafely(fields[ParsingSettings.FieldIndices.PriceChange]);
        var changeRate = ParseHelper.ParseDecimalSafely(fields[ParsingSettings.FieldIndices.ChangeRate]);
        var openPrice = ParseHelper.ParseDecimalSafely(fields[ParsingSettings.FieldIndices.OpenPrice]);
        var highPrice = ParseHelper.ParseDecimalSafely(fields[ParsingSettings.FieldIndices.HighPrice]);
        var lowPrice = ParseHelper.ParseDecimalSafely(fields[ParsingSettings.FieldIndices.LowPrice]);
        var volume = ParseHelper.ParseLongSafely(fields[ParsingSettings.FieldIndices.Volume]);
        var totalVolume = ParseHelper.ParseLongSafely(fields[ParsingSettings.FieldIndices.TotalVolume]);

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
        if (MessageTypeSettings.ChangeSign.RiseCodes.Contains(changeSign))
            return MessageTypeSettings.ChangeType.Rise;

        return MessageTypeSettings.ChangeSign.FallCodes.Contains(changeSign)
            ? MessageTypeSettings.ChangeType.Fall
            : MessageTypeSettings.ChangeType.Flat;
    }


    private DateTime ParseTradeTime(string tradeTime)
    {
        var parsing = _realTimeSettings.Parsing;

        if (tradeTime.Length != parsing.TradeTimeLength)
            return DateTime.Now;

        var hour = ParseHelper.ParseIntSafely(tradeTime[..2]);
        var minute = ParseHelper.ParseIntSafely(tradeTime.Substring(2, 2));
        var second = ParseHelper.ParseIntSafely(tradeTime.Substring(4, 2));

        return DateTime.Today
            .AddHours(hour)
            .AddMinutes(minute)
            .AddSeconds(second);
    }

    private PeriodPriceData ConvertToPeriodPriceData(KisPeriodPriceOutput2 kisData)
    {
        return new PeriodPriceData
        {
            Date = kisData.BusinessDate,
            OpenPrice = ConvertToDecimal(kisData.OpenPrice),
            HighPrice = ConvertToDecimal(kisData.HighPrice),
            LowPrice = ConvertToDecimal(kisData.LowPrice),
            ClosePrice = ConvertToDecimal(kisData.ClosePrice),
            Volume = ConvertToLong(kisData.Volume),
            TradingValue = ConvertToLong(kisData.TradingAmount),
            PriceChange = ConvertToDecimal(kisData.PriceChange),
            ChangeSign = kisData.ChangeSign,
            FlagCode = kisData.FlagCode,
            SplitRate = ConvertToDecimal(kisData.SplitRate)
        };
    }

    private decimal ConvertToDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        return decimal.TryParse(value, out var result) ? result : 0;
    }

    private long ConvertToLong(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        return long.TryParse(value, out var result) ? result : 0;
    }

    #endregion
}