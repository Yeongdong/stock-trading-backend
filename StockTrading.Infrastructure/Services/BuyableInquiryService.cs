using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.Trading.Inquiry;
using StockTrading.Application.DTOs.Users;
using StockTrading.Application.Services;
using static StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Helpers.KisValidationHelper;

namespace StockTrading.Infrastructure.Services;

public class BuyableInquiryService: IBuyableInquiryService
{
    private readonly IKisApiClient _kisApiClient;
    private readonly ILogger<BuyableInquiryService> _logger;

    public BuyableInquiryService(IKisApiClient kisApiClient, ILogger<BuyableInquiryService> logger)
    {
        _kisApiClient = kisApiClient;
        _logger = logger;
    }
    
    public async Task<BuyableInquiryResponse> GetBuyableInquiryAsync(BuyableInquiryRequest request, UserInfo userInfo)
    {
        ValidateUserForKisApi(userInfo);

        _logger.LogInformation("매수가능조회 시작: 사용자 {UserId}, 종목 {StockCode}", userInfo.Id, request.StockCode);
        var response = await _kisApiClient.GetBuyableInquiryAsync(request, userInfo);
        _logger.LogInformation("매수가능조회 완료: 사용자 {UserId}, 종목 {StockCode}", userInfo.Id, request.StockCode);

        return response;
    }
}