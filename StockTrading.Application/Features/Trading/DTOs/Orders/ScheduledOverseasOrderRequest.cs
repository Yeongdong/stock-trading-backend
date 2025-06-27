namespace StockTrading.Application.Features.Trading.DTOs.Orders;

/// <summary>
/// 해외주식 예약주문 요청
/// </summary>
public class ScheduledOverseasOrderRequest : OverseasOrderRequest
{
    /// <summary>
    /// 예약 실행 시간 (KIS에서 자동 처리)
    /// </summary>
    public DateTime ScheduledExecutionTime { get; set; }

    /// <summary>
    /// 예약주문 여부
    /// </summary>
    public bool IsScheduled => true;
}