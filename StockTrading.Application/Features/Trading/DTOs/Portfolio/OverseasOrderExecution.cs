namespace StockTrading.Application.Features.Trading.DTOs.Portfolio;

/// <summary>
/// 해외 주식 주문 체결 내역
/// </summary>
public class OverseasOrderExecution
{
    /// <summary>
    /// 체결 번호
    /// </summary>
    public string ExecutionNumber { get; set; } = null!;

    /// <summary>
    /// 주문 번호
    /// </summary>
    public string OrderNumber { get; set; } = null!;

    /// <summary>
    /// 체결 시간
    /// </summary>
    public DateTime ExecutionTime { get; set; }

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
    /// 주문 구분 (매수/매도)
    /// </summary>
    public string TradeType { get; set; } = null!;

    /// <summary>
    /// 체결 수량
    /// </summary>
    public int ExecutedQuantity { get; set; }

    /// <summary>
    /// 체결 단가
    /// </summary>
    public decimal ExecutedPrice { get; set; }

    /// <summary>
    /// 체결 금액
    /// </summary>
    public decimal ExecutedAmount { get; set; }

    /// <summary>
    /// 통화
    /// </summary>
    public string Currency { get; set; } = null!;

    /// <summary>
    /// 수수료
    /// </summary>
    public decimal Commission { get; set; }

    /// <summary>
    /// 세금
    /// </summary>
    public decimal Tax { get; set; }

    /// <summary>
    /// 환율
    /// </summary>
    public decimal ExchangeRate { get; set; }
}