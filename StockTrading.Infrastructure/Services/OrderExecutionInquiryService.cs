using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.Trading.Orders;
using StockTrading.Application.DTOs.Users;
using StockTrading.Application.ExternalServices;
using StockTrading.Application.Services;
using static StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Helpers.KisValidationHelper;

namespace StockTrading.Infrastructure.Services;

public class OrderExecutionInquiryService : IOrderExecutionInquiryService
{
    private readonly IKisOrderApiClient _kisOrderApiClient;

    public OrderExecutionInquiryService(IKisOrderApiClient kisOrderApiClient)
    {
        _kisOrderApiClient = kisOrderApiClient;
    }

    public async Task<OrderExecutionInquiryResponse> GetOrderExecutionsAsync(OrderExecutionInquiryRequest request,
        UserInfo userInfo)
    {
        ValidateUserForKisApi(userInfo);
        ValidateRequest(request);

        return await _kisOrderApiClient.GetOrderExecutionsAsync(request, userInfo);
    }
}