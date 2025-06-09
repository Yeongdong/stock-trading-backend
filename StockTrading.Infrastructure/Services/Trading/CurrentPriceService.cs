using StockTrading.Application.ExternalServices;
using StockTrading.Application.Features.Market.Services;
using StockTrading.Application.Features.Trading.DTOs.Inquiry;
using StockTrading.Application.Features.Users.DTOs;
using static StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Common.Helpers.KisValidationHelper;

namespace StockTrading.Infrastructure.Services.Trading;

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