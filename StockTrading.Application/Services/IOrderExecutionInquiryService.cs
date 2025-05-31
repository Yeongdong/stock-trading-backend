using StockTrading.Application.DTOs.Trading.Orders;
using StockTrading.Application.DTOs.Users;

namespace StockTrading.Application.Services;

/// <summary>
/// 주문체결조회 서비스 인터페이스
/// </summary>
public interface IOrderExecutionInquiryService
{
    /// <summary>
    /// 주문체결내역 조회
    /// </summary>
    Task<OrderExecutionInquiryResponse> GetOrderExecutionsAsync(OrderExecutionInquiryRequest request, UserInfo userInfo);
}