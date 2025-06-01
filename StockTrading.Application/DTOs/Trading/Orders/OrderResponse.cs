using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

namespace StockTrading.Application.DTOs.Trading.Orders;

/// <summary>
/// 주문 응답 DTO (Application 레이어용)
/// </summary>
public class OrderResponse : KisBaseResponse<KisOrderData>
{
    /// <summary>
    /// 주문번호 (편의 속성)
    /// </summary>
    public string? OrderNumber => Output?.OrderNumber;

    /// <summary>
    /// 주문시간 (편의 속성)  
    /// </summary>
    public string? OrderTime => Output?.OrderTime;
}
