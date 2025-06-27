using StockTrading.Application.DTOs.External.KoreaInvestment.Requests;
using StockTrading.Application.Features.Trading.DTOs.Inquiry;
using StockTrading.Application.Features.Users.DTOs;

namespace StockTrading.Application.ExternalServices;

public interface IKisPriceApiClient
{
    // 국내 주식
    Task<DomesticCurrentPriceResponse> GetDomesticCurrentPriceAsync(CurrentPriceRequest request, UserInfo user);
    Task<PeriodPriceResponse> GetDomesticPeriodPriceAsync(PeriodPriceRequest request, UserInfo user);

    // 해외 주식
    Task<OverseasCurrentPriceResponse> GetOverseasCurrentPriceAsync(OverseasPriceRequest request, UserInfo user);
    Task<OverseasPeriodPriceResponse> GetOverseasPeriodPriceAsync(OverseasPeriodPriceRequest request, UserInfo user);
}