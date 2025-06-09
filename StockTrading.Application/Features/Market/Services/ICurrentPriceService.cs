using StockTrading.Application.Features.Trading.DTOs.Inquiry;
using StockTrading.Application.Features.Users.DTOs;

namespace StockTrading.Application.Features.Market.Services;

public interface ICurrentPriceService
{
    Task<KisCurrentPriceResponse> GetCurrentPriceAsync(CurrentPriceRequest request, UserInfo userInfo);
}