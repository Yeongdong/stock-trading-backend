using StockTrading.Application.ExternalServices;
using StockTrading.Application.Features.Trading.DTOs.Inquiry;
using StockTrading.Application.Features.Trading.Services;
using StockTrading.Application.Features.Users.DTOs;
using static StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Common.Helpers.KisValidationHelper;

namespace StockTrading.Infrastructure.Services.Trading;

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