using StockTrading.Application.ExternalServices;
using StockTrading.Application.Features.Trading.DTOs.Orders;
using StockTrading.Application.Features.Trading.Services;
using StockTrading.Application.Features.Users.DTOs;
using static StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Common.Helpers.KisValidationHelper;

namespace StockTrading.Infrastructure.Services.Trading;

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