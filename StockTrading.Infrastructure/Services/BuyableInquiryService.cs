using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.Trading.Inquiry;
using StockTrading.Application.DTOs.Users;
using StockTrading.Application.Services;
using static StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Helpers.KisValidationHelper;

namespace StockTrading.Infrastructure.Services;

public class BuyableInquiryService: IBuyableInquiryService
{
    private readonly IKisApiClient _kisApiClient;

    public BuyableInquiryService(IKisApiClient kisApiClient)
    {
        _kisApiClient = kisApiClient;
    }
    
    public async Task<BuyableInquiryResponse> GetBuyableInquiryAsync(BuyableInquiryRequest request, UserInfo userInfo)
    {
        ValidateUserForKisApi(userInfo);
        return await _kisApiClient.GetBuyableInquiryAsync(request, userInfo);
    }
}