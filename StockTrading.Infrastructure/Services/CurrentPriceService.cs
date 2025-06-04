using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.Trading.Inquiry;
using StockTrading.Application.DTOs.Users;
using StockTrading.Application.ExternalServices;
using StockTrading.Application.Services;
using static StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Helpers.KisValidationHelper;

namespace StockTrading.Infrastructure.Services;

public class CurrentPriceService: ICurrentPriceService
{
    private readonly IKisPriceApiClient _kisPriceApiClient;

    public CurrentPriceService(IKisPriceApiClient kisPriceApiClient)
    {
        _kisPriceApiClient = kisPriceApiClient;
    }
    
    public async Task<KisCurrentPriceResponse> GetCurrentPriceAsync(CurrentPriceRequest request, UserInfo userInfo)
    {
        ValidateUserForKisApi(userInfo);
        var response = await _kisPriceApiClient.GetCurrentPriceAsync(request, userInfo);

        return response;
    }
}