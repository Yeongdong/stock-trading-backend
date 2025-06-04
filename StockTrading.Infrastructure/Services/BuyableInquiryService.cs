using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.Trading.Inquiry;
using StockTrading.Application.DTOs.Users;
using StockTrading.Application.ExternalServices;
using StockTrading.Application.Services;
using static StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Helpers.KisValidationHelper;

namespace StockTrading.Infrastructure.Services;

public class BuyableInquiryService: IBuyableInquiryService
{
    private readonly IKisBalanceApiClient _kisBalanceApiClient;

    public BuyableInquiryService(IKisBalanceApiClient kisBalanceApiClient)
    {
        _kisBalanceApiClient = kisBalanceApiClient;
    }
    
    public async Task<BuyableInquiryResponse> GetBuyableInquiryAsync(BuyableInquiryRequest request, UserInfo userInfo)
    {
        ValidateUserForKisApi(userInfo);
        return await _kisBalanceApiClient.GetBuyableInquiryAsync(request, userInfo);
    }
}