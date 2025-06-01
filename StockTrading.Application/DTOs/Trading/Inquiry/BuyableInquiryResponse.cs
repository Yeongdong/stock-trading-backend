namespace StockTrading.Application.DTOs.Trading.Inquiry;

/// <summary>
/// 매수가능조회 응답 DTO
/// </summary>
public class BuyableInquiryResponse
{
    /// <summary>
    /// 종목코드
    /// </summary>
    public string StockCode { get; init; } = string.Empty;

    /// <summary>
    /// 종목명
    /// </summary>
    public string StockName { get; init; } = string.Empty;

    /// <summary>
    /// 매수가능금액
    /// </summary>
    public decimal BuyableAmount { get; init; }

    /// <summary>
    /// 매수가능수량
    /// </summary>
    public int BuyableQuantity { get; init; }

    /// <summary>
    /// 주문가능금액
    /// </summary>
    public decimal OrderableAmount { get; init; }

    /// <summary>
    /// 현금잔고
    /// </summary>
    public decimal CashBalance { get; init; }

    /// <summary>
    /// 주문단가
    /// </summary>
    public decimal OrderPrice { get; init; }

    /// <summary>
    /// 현재가
    /// </summary>
    public decimal CurrentPrice { get; init; }

    /// <summary>
    /// 단위수량
    /// </summary>
    public int UnitQuantity { get; init; } = 1;
}