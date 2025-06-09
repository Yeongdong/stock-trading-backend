using StockTrading.Application.Features.Trading.DTOs.Inquiry;
using StockTrading.Application.Features.Users.DTOs;

namespace StockTrading.Application.Features.Trading.Services;

public interface IPeriodPriceService
{
    Task<PeriodPriceResponse> GetPeriodPriceAsync(PeriodPriceRequest request, UserInfo user);
}