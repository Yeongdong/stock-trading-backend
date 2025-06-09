using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.Features.Trading.DTOs.Inquiry;
using StockTrading.Domain.Settings;
using StockTrading.Domain.Settings.ExternalServices;
using StockTrading.Infrastructure.Utilities;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Market.Converters;

public class PriceDataConverter
{
    public KisCurrentPriceResponse ConvertToCurrentPriceResponse(KisCurrentPriceData kisData, string stockCode)
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


    public static string ConvertChangeType(string changeSign)
    {
        if (MessageTypes.ChangeSign.RiseCodes.Contains(changeSign))
            return MessageTypes.ChangeType.Rise;

        return MessageTypes.ChangeSign.FallCodes.Contains(changeSign)
            ? MessageTypes.ChangeType.Fall
            : MessageTypes.ChangeType.Flat;
    }
}