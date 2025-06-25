namespace StockTrading.Application.Features.Trading.DTOs.Portfolio;

/// <summary>
/// 해외 예수금 정보
/// </summary>
public class OverseasDepositInfo
{
    /// <summary>
    /// 예수금 총액 (해외통화)
    /// </summary>
    public decimal TotalDepositAmount { get; init; }

    /// <summary>
    /// 주문가능금액 (해외통화)
    /// </summary>
    public decimal OrderableAmount { get; init; }

    /// <summary>
    /// 통화 코드
    /// </summary>
    public string CurrencyCode { get; init; } = string.Empty;

    /// <summary>
    /// 환율
    /// </summary>
    public decimal ExchangeRate { get; init; }

    /// <summary>
    /// 예수금 총액 (원화환산)
    /// </summary>
    public decimal TotalDepositAmountKrw { get; init; }

    /// <summary>
    /// 조회 시간
    /// </summary>
    public DateTime InquiryTime { get; init; }
}