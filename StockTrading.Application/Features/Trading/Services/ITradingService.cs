using StockTrading.Application.Features.Trading.DTOs.Inquiry;
using StockTrading.Application.Features.Trading.DTOs.Orders;
using StockTrading.Application.Features.Trading.DTOs.Portfolio;
using StockTrading.Application.Features.Users.DTOs;

namespace StockTrading.Application.Features.Trading.Services;

public interface ITradingService
{
    // 국내 주식 주문 관리
    Task<OrderResponse> PlaceOrderAsync(OrderRequest order, UserInfo user);
    Task<BuyableInquiryResponse> GetBuyableInquiryAsync(BuyableInquiryRequest request, UserInfo userInfo);

    // 국내 주식 조회
    Task<AccountBalance> GetStockBalanceAsync(UserInfo user);

    Task<OrderExecutionInquiryResponse>
        GetOrderExecutionsAsync(OrderExecutionInquiryRequest request, UserInfo userInfo);

    // 해외 주식 주문 관리
    Task<OverseasOrderResponse> PlaceOverseasOrderAsync(OverseasOrderRequest order, UserInfo user);

    // 해외 주식 조회
    Task<List<OverseasOrderExecution>> GetOverseasOrderExecutionsAsync(string startDate, string endDate, UserInfo user);
}