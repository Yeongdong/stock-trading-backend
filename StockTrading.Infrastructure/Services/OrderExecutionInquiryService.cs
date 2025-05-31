using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.Trading.Orders;
using StockTrading.Application.DTOs.Users;
using StockTrading.Application.Services;
using static StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Helpers.KisValidationHelper;

namespace StockTrading.Infrastructure.Services;

public class OrderExecutionInquiryService : IOrderExecutionInquiryService
{
    private readonly IKisApiClient _kisApiClient;
    private readonly ILogger<OrderExecutionInquiryService> _logger;

    public OrderExecutionInquiryService(IKisApiClient kisApiClient, ILogger<OrderExecutionInquiryService> logger)
    {
        _kisApiClient = kisApiClient;
        _logger = logger;
    }

    public async Task<OrderExecutionInquiryResponse> GetOrderExecutionsAsync(OrderExecutionInquiryRequest request,
        UserInfo userInfo)
    {
        ValidateUserForKisApi(userInfo);
        ValidateRequest(request);

        _logger.LogInformation("주문체결조회 시작: 사용자 {UserId}, 기간 {StartDate}~{EndDate}", userInfo.Id, request.StartDate,
            request.EndDate);
        var response = await _kisApiClient.GetOrderExecutionsAsync(request, userInfo);
        _logger.LogInformation("주문체결조회 완료: 사용자 {UserId}, 총 {Count}건", userInfo.Id, response.TotalCount);

        return response;
    }
}