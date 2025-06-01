using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

/// <summary>
/// 매수가능조회 응답 데이터
/// </summary>
public class KisBuyableInquiryData
{
    /// <summary>
    /// 주문가능현금 (매수가능금액)
    /// </summary>
    [JsonPropertyName("ord_psbl_cash")]
    public string BuyableAmount { get; init; } = string.Empty;

    /// <summary>
    /// 주문가능대용
    /// </summary>
    [JsonPropertyName("ord_psbl_sbst")]
    public string OrderableSubstitute { get; init; } = string.Empty;

    /// <summary>
    /// 재사용가능금액
    /// </summary>
    [JsonPropertyName("ruse_psbl_amt")]
    public string ReusableAmount { get; init; } = string.Empty;

    /// <summary>
    /// 펀드환매대금
    /// </summary>
    [JsonPropertyName("fund_rpch_chgs")]
    public string FundRedemptionAmount { get; init; } = string.Empty;

    /// <summary>
    /// 가능수량계산단가
    /// </summary>
    [JsonPropertyName("psbl_qty_calc_unpr")]
    public string CalculationPrice { get; init; } = string.Empty;

    /// <summary>
    /// 익일수령매수금액
    /// </summary>
    [JsonPropertyName("nrcvb_buy_amt")]
    public string NextDayBuyAmount { get; init; } = string.Empty;

    /// <summary>
    /// 익일수령매수수량
    /// </summary>
    [JsonPropertyName("nrcvb_buy_qty")]
    public string BuyableQuantity { get; init; } = string.Empty;

    /// <summary>
    /// 최대매수금액
    /// </summary>
    [JsonPropertyName("max_buy_amt")]
    public string MaxBuyAmount { get; init; } = string.Empty;

    /// <summary>
    /// 현금부족분
    /// </summary>
    [JsonPropertyName("mty")]
    public string CashShortfall { get; init; } = string.Empty;

    /// <summary>
    /// CMA평가금액
    /// </summary>
    [JsonPropertyName("cma_evlu_amt")]
    public string CmaEvaluationAmount { get; init; } = string.Empty;

    /// <summary>
    /// 해외재사용금액원화
    /// </summary>
    [JsonPropertyName("ovrs_re_use_amt_wcrc")]
    public string OverseasReuseAmount { get; init; } = string.Empty;

    /// <summary>
    /// 주문가능외화금액원화
    /// </summary>
    [JsonPropertyName("ord_psbl_frcr_amt_wcrc")]
    public string OrderableForeignCurrencyAmount { get; init; } = string.Empty;
}