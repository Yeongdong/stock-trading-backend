using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.Trading.Inquiry;
using StockTrading.Application.DTOs.Users;
using StockTrading.Application.Services;
using static StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Helpers.KisValidationHelper;

namespace StockTrading.Infrastructure.Services;

public class CurrentPriceService: ICurrentPriceService
{
    private readonly IKisApiClient _kisApiClient;
    private readonly ILogger<CurrentPriceService> _logger;

    public CurrentPriceService(IKisApiClient kisApiClient, ILogger<CurrentPriceService> logger)
    {
        _kisApiClient = kisApiClient;
        _logger = logger;
    }
    
    public async Task<CurrentPriceResponse> GetCurrentPriceAsync(CurrentPriceRequest request, UserInfo userInfo)
    {
        ValidateUserForKisApi(userInfo);

        _logger.LogInformation("주식 현재가 조회 시작: 사용자 {UserId}, 종목 {StockCode}", userInfo.Id, request.StockCode);
        var response = await _kisApiClient.GetCurrentPriceAsync(request, userInfo);
        _logger.LogInformation("주식 현재가 조회 완료: 사용자 {UserId}, 종목 {StockCode}, 현재가 {Price}원", userInfo.Id, request.StockCode, response.CurrentPrice);

        return response;
    }
}