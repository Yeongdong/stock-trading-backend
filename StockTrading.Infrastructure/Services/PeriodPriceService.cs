using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.Trading.Inquiry;
using StockTrading.Application.DTOs.Users;
using StockTrading.Application.ExternalServices;
using StockTrading.Application.Services;

namespace StockTrading.Infrastructure.Services;

public class PeriodPriceService : IPeriodPriceService
{
    private readonly IKisPriceApiClient _kisPriceApiClient;
    private readonly ILogger<PeriodPriceService> _logger;

    public PeriodPriceService(IKisPriceApiClient kisPriceApiClient, ILogger<PeriodPriceService> logger)
    {
        _kisPriceApiClient = kisPriceApiClient;
        _logger = logger;
    }

    public async Task<PeriodPriceResponse> GetPeriodPriceAsync(PeriodPriceRequest request, UserInfo user)
    {
        ValidateRequest(request);

        var response = await _kisPriceApiClient.GetPeriodPriceAsync(request, user);
        _logger.LogInformation("기간별 시세 조회 완료: 사용자 {UserId}, 종목 {StockCode}, 데이터 건수 {Count}", user.Id, request.StockCode,
            response.PriceData.Count);

        return response;
    }

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

        // API 스펙: 최대 100개 데이터
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
}