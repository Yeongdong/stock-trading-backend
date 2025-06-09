using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.Features.Trading.DTOs.Inquiry;
using StockTrading.Domain.Settings.ExternalServices;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Market.Converters;
using StockTrading.Infrastructure.Utilities;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.RealTime.Converters;

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
        var stockCode = fields[ParsingConfig.FieldIndices.StockCode];

        if (!ParseHelper.IsValidStockCode(stockCode, _realTimeSettings.Parsing.StockCodeLength))
        {
            _logger.LogDebug("레코드 {Index}: 유효하지 않은 종목코드: {StockCode}", recordIndex, stockCode);
            return null;
        }

        var priceData = CreateTransactionInfo(fields, stockCode);

        _logger.LogDebug("레코드 {Index}: 변환 성공 - 종목: {Symbol}, 현재가: {Price}원", recordIndex, stockCode, priceData.Price);

        return priceData;
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
        var tradeTime = fields[ParsingConfig.FieldIndices.TradeTime];
        var currentPrice = ParseHelper.ParseDecimalSafely(fields[ParsingConfig.FieldIndices.CurrentPrice]);
        var changeSign = fields[ParsingConfig.FieldIndices.ChangeSign];
        var priceChange = ParseHelper.ParseDecimalSafely(fields[ParsingConfig.FieldIndices.PriceChange]);
        var changeRate = ParseHelper.ParseDecimalSafely(fields[ParsingConfig.FieldIndices.ChangeRate]);
        var openPrice = ParseHelper.ParseDecimalSafely(fields[ParsingConfig.FieldIndices.OpenPrice]);
        var highPrice = ParseHelper.ParseDecimalSafely(fields[ParsingConfig.FieldIndices.HighPrice]);
        var lowPrice = ParseHelper.ParseDecimalSafely(fields[ParsingConfig.FieldIndices.LowPrice]);
        var volume = ParseHelper.ParseLongSafely(fields[ParsingConfig.FieldIndices.Volume]);
        var totalVolume = ParseHelper.ParseLongSafely(fields[ParsingConfig.FieldIndices.TotalVolume]);

        return new KisTransactionInfo
        {
            Symbol = stockCode,
            Price = currentPrice,
            PriceChange = priceChange,
            ChangeType = PriceDataConverter.ConvertChangeType(changeSign),
            TransactionTime = ParseTradeTime(tradeTime),
            ChangeRate = changeRate,
            Volume = (int)Math.Min(volume, int.MaxValue),
            TotalVolume = totalVolume,
            OpenPrice = openPrice,
            HighPrice = highPrice,
            LowPrice = lowPrice
        };
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