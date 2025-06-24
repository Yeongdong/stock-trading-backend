using System.ComponentModel.DataAnnotations;

namespace StockTrading.Application.Features.Trading.DTOs.Orders;

/// <summary>
/// 해외 주식 주문 요청
/// </summary>
public class OverseasOrderRequest
{
    /// <summary>
    /// 종목 코드 (예: AAPL, TSLA)
    /// </summary>
    [Required]
    [StringLength(10)]
    public string StockCode { get; set; } = null!;

    /// <summary>
    /// 시장 구분 (nasdaq, nyse, tokyo, london, hongkong)
    /// </summary>
    [Required]
    public StockTrading.Domain.Enums.Market Market { get; set; }

    /// <summary>
    /// 주문 구분 (매수: VTTT1002U, 매도: VTTT1001U)
    /// </summary>
    [Required]
    public string TradeType { get; set; } = null!;

    /// <summary>
    /// 주문 방법 (00: 지정가, 01: 시장가)
    /// </summary>
    [Required]
    [RegularExpression("^(00|01)$")]
    public string OrderDivision { get; set; } = null!;

    /// <summary>
    /// 주문 수량
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    /// <summary>
    /// 주문 단가 (시장가일 경우 0)
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    /// <summary>
    /// 주문 조건 (FTC: Fill or Kill, DAY: 당일)
    /// </summary>
    [Required]
    [RegularExpression("^(FTC|DAY)$")]
    public string OrderCondition { get; set; } = "DAY";
}