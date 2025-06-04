using StockTrading.Application.DTOs.Trading.Orders;
using StockTrading.Application.DTOs.Users;

namespace StockTrading.Application.ExternalServices;

public interface IKisOrderApiClient
{
    Task<OrderResponse> PlaceOrderAsync(OrderRequest request, UserInfo user);
    Task<OrderExecutionInquiryResponse> GetOrderExecutionsAsync(OrderExecutionInquiryRequest request, UserInfo user);
}