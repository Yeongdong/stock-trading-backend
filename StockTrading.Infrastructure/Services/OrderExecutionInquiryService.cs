using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.Trading.Orders;
using StockTrading.Application.DTOs.Users;
using StockTrading.Application.Services;
using static StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Helpers.KisValidationHelper;

namespace StockTrading.Infrastructure.Services;

public class OrderExecutionInquiryService : IOrderExecutionInquiryService
{
    private readonly IKisApiClient _kisApiClient;

    public OrderExecutionInquiryService(IKisApiClient kisApiClient)
    {
        _kisApiClient = kisApiClient;
    }

    public async Task<OrderExecutionInquiryResponse> GetOrderExecutionsAsync(OrderExecutionInquiryRequest request,
        UserInfo userInfo)
    {
        ValidateUserForKisApi(userInfo);
        ValidateRequest(request);

        return await _kisApiClient.GetOrderExecutionsAsync(request, userInfo);
    }
}