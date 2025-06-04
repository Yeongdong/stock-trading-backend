using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.Trading.Inquiry;
using StockTrading.Application.DTOs.Users;
using StockTrading.Application.Services;
using static StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Helpers.KisValidationHelper;

namespace StockTrading.Infrastructure.Services;

public class CurrentPriceService: ICurrentPriceService
{
    private readonly IKisApiClient _kisApiClient;

    public CurrentPriceService(IKisApiClient kisApiClient)
    {
        _kisApiClient = kisApiClient;
    }
    
    public async Task<CurrentPriceResponse> GetCurrentPriceAsync(CurrentPriceRequest request, UserInfo userInfo)
    {
        ValidateUserForKisApi(userInfo);
        var response = await _kisApiClient.GetCurrentPriceAsync(request, userInfo);

        return response;
    }
}