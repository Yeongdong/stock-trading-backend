using StockTrading.Application.DTOs.Trading.Inquiry;
using StockTrading.Application.DTOs.Users;

namespace StockTrading.Application.Services;

public interface IPeriodPriceService
{
    Task<PeriodPriceResponse> GetPeriodPriceAsync(PeriodPriceRequest request, UserInfo user);
}