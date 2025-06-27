namespace StockTrading.Domain.Enums;

/// <summary>
/// 주문 상태
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// 대기중 (즉시주문의 초기 상태)
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 예약됨 (예약주문이 접수된 상태)
    /// </summary>
    Scheduled = 1,

    /// <summary>
    /// 실행됨 (주문이 실제로 체결된 상태)
    /// </summary>
    Executed = 2,

    /// <summary>
    /// 실패 (주문 처리 중 오류 발생)
    /// </summary>
    Failed = 3,

    /// <summary>
    /// 취소됨 (사용자가 취소하거나 시스템에서 자동 취소)
    /// </summary>
    Cancelled = 4
}