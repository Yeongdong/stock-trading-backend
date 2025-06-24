using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.External.KoreaInvestment.Requests;
using StockTrading.Application.ExternalServices;
using StockTrading.Application.Features.Market.Services;
using StockTrading.Application.Features.Trading.DTOs.Inquiry;
using StockTrading.Application.Features.Users.DTOs;
using static StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Common.Helpers.KisValidationHelper;

namespace StockTrading.Infrastructure.Services.Market;

public class PriceService : IPriceService
{
    private readonly IKisPriceApiClient _kisPriceApiClient;
    private readonly ILogger<PriceService> _logger;

    public PriceService(IKisPriceApiClient kisPriceApiClient, ILogger<PriceService> logger)
    {
        _kisPriceApiClient = kisPriceApiClient;
        _logger = logger;
    }

    #region 국내 주식 시세

    public async Task<DomesticCurrentPriceResponse> GetDomesticCurrentPriceAsync(CurrentPriceRequest request,
        UserInfo userInfo)
    {
        ValidateUserForKisApi(userInfo);
        var response = await _kisPriceApiClient.GetDomesticCurrentPriceAsync(request, userInfo);
        return response;
    }

    public async Task<PeriodPriceResponse> GetDomesticPeriodPriceAsync(PeriodPriceRequest request, UserInfo user)
    {
        ValidateUserForKisApi(user);
        ValidateRequest(request);

        var response = await _kisPriceApiClient.GetDomesticPeriodPriceAsync(request, user);
        _logger.LogInformation("기간별 시세 조회 완료: 사용자 {UserId}, 종목 {StockCode}, 데이터 건수 {Count}",
            user.Id, request.StockCode, response.PriceData.Count);

        return response;
    }

    #endregion

    #region 해외 주식 시세

    public async Task<OverseasCurrentPriceResponse> GetOverseasCurrentPriceAsync(string stockCode,
        StockTrading.Domain.Enums.Market market, UserInfo userInfo)
    {
        ValidateUserForKisApi(userInfo);

        var request = new OverseasPriceRequest
        {
            StockCode = stockCode,
            MarketCode = GetMarketCode(market)
        };

        var response = await _kisPriceApiClient.GetOverseasCurrentPriceAsync(request, userInfo);
        return response;
    }

    #endregion

    #region Private Helper Methods

    private void ValidateRequest(PeriodPriceRequest request)
    {
        ValidateDateRange(request.StartDate, request.EndDate);
        ValidateMaxDataRange(request.StartDate, request.EndDate, request.PeriodDivCode);
    }

    private void ValidateDateRange(string startDate, string endDate)
    {
        if (!DateTime.TryParseExact(startDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None,
                out var start))
            throw new ArgumentException("시작일자 형식이 올바르지 않습니다.");

        if (!DateTime.TryParseExact(endDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var end))
            throw new ArgumentException("종료일자 형식이 올바르지 않습니다.");

        if (start > end)
            throw new ArgumentException("시작일자는 종료일자보다 이전이어야 합니다.");

        if (end > DateTime.Now.Date)
            throw new ArgumentException("종료일자는 현재 날짜보다 이후일 수 없습니다.");
    }

    private void ValidateMaxDataRange(string startDate, string endDate, string periodDivCode)
    {
        var start = DateTime.ParseExact(startDate, "yyyyMMdd", null);
        var end = DateTime.ParseExact(endDate, "yyyyMMdd", null);
        var daysDifference = (end - start).Days;

        var maxDays = periodDivCode switch
        {
            "D" => 100, // 일봉: 100일
            "W" => 700, // 주봉: 약 100주 (700일)
            "M" => 3000, // 월봉: 약 100개월 (3000일)
            "Y" => 36500, // 년봉: 약 100년 (36500일)
            _ => 100
        };

        if (daysDifference > maxDays)
            throw new ArgumentException($"조회 기간이 너무 깁니다. {periodDivCode} 기간으로는 최대 {maxDays}일까지 조회 가능합니다.");
    }

    private static string GetMarketCode(StockTrading.Domain.Enums.Market market)
    {
        return market switch
        {
            Domain.Enums.Market.Nasdaq => "NAS",
            Domain.Enums.Market.Nyse => "NYS",
            Domain.Enums.Market.Tokyo => "TSE",
            Domain.Enums.Market.London => "LSE",
            Domain.Enums.Market.Hongkong => "HKSE",
            _ => throw new ArgumentException($"지원하지 않는 해외 시장: {market}")
        };
    }

    #endregion
}