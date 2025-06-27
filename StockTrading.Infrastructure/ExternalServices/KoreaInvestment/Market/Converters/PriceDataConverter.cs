using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.Features.Trading.DTOs.Inquiry;
using StockTrading.Domain.Settings.ExternalServices;
using StockTrading.Infrastructure.Utilities;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Market.Converters;

public class PriceDataConverter
{
    #region 국내 주식 변환

    public DomesticCurrentPriceResponse ConvertToCurrentPriceResponse(KisCurrentPriceData kisData, string stockCode)
    {
        return new DomesticCurrentPriceResponse
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

    public PeriodPriceResponse ConvertToPeriodPriceResponse(KisPeriodPriceResponse kisData, string stockCode)
    {
        var current = kisData.CurrentInfo;

        return new PeriodPriceResponse
        {
            StockCode = stockCode,
            StockName = current?.StockName ?? string.Empty,
            CurrentPrice = ParseHelper.ParseDecimalSafely(current?.CurrentPrice),
            PriceChange = ParseHelper.ParseDecimalSafely(current?.PriceChange),
            ChangeRate = ParseHelper.ParseDecimalSafely(current?.ChangeRate),
            ChangeSign = current?.ChangeSign ?? string.Empty,
            TotalVolume = ParseHelper.ParseLongSafely(current?.AccumulatedVolume),
            TotalTradingValue = ParseHelper.ParseLongSafely(current?.AccumulatedAmount),
            PriceData = kisData.PriceData.Select(p => new PeriodPriceData
            {
                Date = p.BusinessDate,
                OpenPrice = ParseHelper.ParseDecimalSafely(p.OpenPrice),
                HighPrice = ParseHelper.ParseDecimalSafely(p.HighPrice),
                LowPrice = ParseHelper.ParseDecimalSafely(p.LowPrice),
                ClosePrice = ParseHelper.ParseDecimalSafely(p.ClosePrice),
                Volume = ParseHelper.ParseLongSafely(p.Volume),
                TradingValue = ParseHelper.ParseLongSafely(p.TradingAmount),
                PriceChange = ParseHelper.ParseDecimalSafely(p.PriceChange),
                ChangeSign = p.ChangeSign,
                FlagCode = p.FlagCode,
                SplitRate = ParseHelper.ParseDecimalSafely(p.SplitRate)
            }).ToList()
        };
    }

    #endregion

    #region 해외 주식 변환

    public OverseasCurrentPriceResponse ConvertToOverseasCurrentPriceResponse(KisOverseasPriceData kisData,
        string stockCode)
    {
        return new OverseasCurrentPriceResponse
        {
            StockCode = stockCode,
            StockName = kisData.StockName,
            CurrentPrice = ParseHelper.ParseDecimalSafely(kisData.CurrentPrice),
            PriceChange = ParseHelper.ParseDecimalSafely(kisData.PriceChange),
            ChangeRate = ParseHelper.ParseDecimalSafely(kisData.ChangeRate),
            ChangeType = ConvertOverseasChangeType(kisData.PriceChange),
            OpenPrice = ParseHelper.ParseDecimalSafely(kisData.OpenPrice),
            HighPrice = ParseHelper.ParseDecimalSafely(kisData.HighPrice),
            LowPrice = ParseHelper.ParseDecimalSafely(kisData.LowPrice),
            Volume = ParseHelper.ParseLongSafely(kisData.Volume),
            Currency = kisData.Currency,
            MarketStatus = kisData.MarketStatus,
            InquiryTime = DateTime.Now
        };
    }

    public OverseasPeriodPriceResponse ConvertToOverseasPeriodPriceResponse(KisOverseasPeriodPriceResponse kisResponse,
        string stockCode)
    {
        if (!kisResponse.IsSuccess || kisResponse.Output == null)
        {
            return new OverseasPeriodPriceResponse
            {
                StockCode = stockCode,
                StockName = stockCode,
                PriceData = []
            };
        }

        var output = kisResponse.Output;

        return new OverseasPeriodPriceResponse
        {
            StockCode = string.IsNullOrEmpty(output.StockCode) ? stockCode : output.StockCode,
            StockName = string.IsNullOrEmpty(output.StockName) ? stockCode : output.StockName,
            CurrentPrice = ParseHelper.ParseDecimalSafely(output.CurrentPrice),
            PriceChange = ParseHelper.ParseDecimalSafely(output.PreviousDayChange),
            ChangeRate = ParseHelper.ParseDecimalSafely(output.PreviousDayChangeRate),
            ChangeSign = output.PreviousDayChangeSign,
            TotalVolume = ParseHelper.ParseLongSafely(output.AccumulatedVolume),
            PriceData = kisResponse.PriceItems?.Select(ConvertToOverseasPriceData).ToList() ?? []
        };
    }

    #endregion

    #region Helper Methods

    public static string ConvertChangeType(string changeSign)
    {
        if (MessageTypes.ChangeSign.RiseCodes.Contains(changeSign))
            return MessageTypes.ChangeType.Rise;

        return MessageTypes.ChangeSign.FallCodes.Contains(changeSign)
            ? MessageTypes.ChangeType.Fall
            : MessageTypes.ChangeType.Flat;
    }

    private static string ConvertOverseasChangeType(string priceChange)
    {
        var change = ParseHelper.ParseDecimalSafely(priceChange);

        if (change > 0)
            return MessageTypes.ChangeType.Rise;

        return change < 0
            ? MessageTypes.ChangeType.Fall
            : MessageTypes.ChangeType.Flat;
    }

    private OverseasPriceData ConvertToOverseasPriceData(KisOverseasPriceItem item)
    {
        return new OverseasPriceData
        {
            Date = item.BusinessDate,
            OpenPrice = ParseHelper.ParseDecimalSafely(item.OpenPrice),
            HighPrice = ParseHelper.ParseDecimalSafely(item.HighPrice),
            LowPrice = ParseHelper.ParseDecimalSafely(item.LowPrice),
            ClosePrice = ParseHelper.ParseDecimalSafely(item.ClosePrice),
            Volume = ParseHelper.ParseLongSafely(item.Volume)
        };
    }

    #endregion
}