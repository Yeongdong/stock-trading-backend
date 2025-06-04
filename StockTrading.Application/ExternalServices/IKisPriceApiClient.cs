using StockTrading.Application.DTOs.Trading.Inquiry;
using StockTrading.Application.DTOs.Users;

namespace StockTrading.Application.ExternalServices;

public interface IKisPriceApiClient
{
    Task<KisCurrentPriceResponse> GetCurrentPriceAsync(CurrentPriceRequest request, UserInfo user);
}