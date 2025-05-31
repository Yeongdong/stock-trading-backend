namespace StockTrading.Application.DTOs.Trading.Orders;

/// <summary>
/// 개별 체결내역 DTO
/// </summary>
public class OrderExecutionItem
{
    /// <summary>주문일자</summary>
    public string OrderDate { get; init; } = string.Empty;

    /// <summary>주문번호</summary>
    public string OrderNumber { get; init; } = string.Empty;

    /// <summary>종목코드</summary>
    public string StockCode { get; init; } = string.Empty;

    /// <summary>종목명</summary>
    public string StockName { get; init; } = string.Empty;

    /// <summary>매도매수구분</summary>
    public string OrderSide { get; init; } = string.Empty;

    /// <summary>주문수량</summary>
    public int OrderQuantity { get; init; }

    /// <summary>주문가격</summary>
    public decimal OrderPrice { get; init; }

    /// <summary>체결수량</summary>
    public int ExecutedQuantity { get; init; }

    /// <summary>체결가격</summary>
    public decimal ExecutedPrice { get; init; }

    /// <summary>체결금액</summary>
    public decimal ExecutedAmount { get; init; }

    /// <summary>주문상태</summary>
    public string OrderStatus { get; init; } = string.Empty;

    /// <summary>체결시간</summary>
    public string ExecutionTime { get; init; } = string.Empty;
}