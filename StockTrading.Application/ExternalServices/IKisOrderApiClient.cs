using StockTrading.Application.Features.Trading.DTOs.Orders;
using StockTrading.Application.Features.Users.DTOs;

namespace StockTrading.Application.ExternalServices;

public interface IKisOrderApiClient
{
    Task<OrderResponse> PlaceOrderAsync(OrderRequest request, UserInfo user);
    Task<OrderExecutionInquiryResponse> GetOrderExecutionsAsync(OrderExecutionInquiryRequest request, UserInfo user);
}