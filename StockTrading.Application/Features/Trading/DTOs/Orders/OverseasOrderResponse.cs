namespace StockTrading.Application.Features.Trading.DTOs.Orders;

/// <summary>
/// 해외 주식 주문 응답
/// </summary>
public class OverseasOrderResponse
{
    /// <summary>
    /// 주문 번호
    /// </summary>
    public string OrderNumber { get; set; } = null!;

    /// <summary>
    /// 주문 시간
    /// </summary>
    public string OrderTime { get; set; } = null!;

    /// <summary>
    /// 종목 코드
    /// </summary>
    public string StockCode { get; set; } = null!;

    /// <summary>
    /// 종목명
    /// </summary>
    public string StockName { get; set; } = null!;

    /// <summary>
    /// 시장 구분
    /// </summary>
    public StockTrading.Domain.Enums.Market Market { get; set; }

    /// <summary>
    /// 주문 구분
    /// </summary>
    public string TradeType { get; set; } = null!;

    /// <summary>
    /// 주문 방법
    /// </summary>
    public string OrderDivision { get; set; } = null!;

    /// <summary>
    /// 주문 수량
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// 주문 단가
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// 주문 조건
    /// </summary>
    public string OrderCondition { get; set; } = null!;

    /// <summary>
    /// 통화
    /// </summary>
    public string Currency { get; set; } = null!;

    /// <summary>
    /// 주문 상태
    /// </summary>
    public string OrderStatus { get; set; } = null!;

    /// <summary>
    /// 메시지
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 주문 성공 여부
    /// </summary>
    public bool IsSuccess { get; set; }
}