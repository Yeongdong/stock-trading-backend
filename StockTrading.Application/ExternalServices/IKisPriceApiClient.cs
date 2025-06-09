using StockTrading.Application.Features.Trading.DTOs.Inquiry;
using StockTrading.Application.Features.Users.DTOs;

namespace StockTrading.Application.ExternalServices;

public interface IKisPriceApiClient
{
    Task<KisCurrentPriceResponse> GetCurrentPriceAsync(CurrentPriceRequest request, UserInfo user);
    Task<PeriodPriceResponse> GetPeriodPriceAsync(PeriodPriceRequest request, UserInfo user);
}