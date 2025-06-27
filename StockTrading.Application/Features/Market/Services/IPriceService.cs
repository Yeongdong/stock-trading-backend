using StockTrading.Application.Features.Trading.DTOs.Inquiry;
using StockTrading.Application.Features.Users.DTOs;

namespace StockTrading.Application.Features.Market.Services;

public interface IPriceService
{
    // 국내 주식 시세
    Task<DomesticCurrentPriceResponse> GetDomesticCurrentPriceAsync(CurrentPriceRequest request, UserInfo userInfo);
    Task<PeriodPriceResponse> GetDomesticPeriodPriceAsync(PeriodPriceRequest request, UserInfo user);

    // 해외 주식 시세
    Task<OverseasCurrentPriceResponse> GetOverseasCurrentPriceAsync(string stockCode,
        StockTrading.Domain.Enums.Market market, UserInfo userInfo);
    Task<OverseasPeriodPriceResponse> GetOverseasPeriodPriceAsync(OverseasPeriodPriceRequest request, UserInfo user);
}