using StockTrading.Application.DTOs.Trading.Orders;
using StockTrading.Application.DTOs.Trading.Portfolio;
using StockTrading.Application.DTOs.Users;

namespace StockTrading.Application.Services;

public interface IKisApiClient
{
    Task<OrderResponse> PlaceOrderAsync(OrderRequest request, UserInfo user);
    Task<AccountBalance> GetStockBalanceAsync(UserInfo user);
    Task<OrderExecutionInquiryResponse> GetOrderExecutionsAsync(OrderExecutionInquiryRequest request, UserInfo user);
}