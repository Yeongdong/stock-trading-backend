using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

/// <summary>
/// 해외 주식 잔고 데이터
/// </summary>
public class KisOverseasBalanceData
{
    /// <summary>
    /// 해외상품번호 (종목코드)
    /// </summary>
    [JsonPropertyName("ovrs_pdno")]
    public string StockCode { get; init; } = string.Empty;

    /// <summary>
    /// 해외종목명
    /// </summary>
    [JsonPropertyName("ovrs_item_name")]
    public string StockName { get; init; } = string.Empty;

    /// <summary>
    /// 해외거래소코드
    /// </summary>
    [JsonPropertyName("ovrs_excg_cd")]
    public string ExchangeCode { get; init; } = string.Empty;

    /// <summary>
    /// 해외잔고수량
    /// </summary>
    [JsonPropertyName("ovrs_cblc_qty")]
    public string Quantity { get; init; } = string.Empty;

    /// <summary>
    /// 매입평균가격
    /// </summary>
    [JsonPropertyName("pchs_avg_pric")]
    public string AveragePrice { get; init; } = string.Empty;

    /// <summary>
    /// 현재가격2
    /// </summary>
    [JsonPropertyName("now_pric2")]
    public string CurrentPrice { get; init; } = string.Empty;

    /// <summary>
    /// 외화매입금액1
    /// </summary>
    [JsonPropertyName("frcr_pchs_amt1")]
    public string PurchaseAmount { get; init; } = string.Empty;

    /// <summary>
    /// 해외주식평가금액
    /// </summary>
    [JsonPropertyName("ovrs_stck_evlu_amt")]
    public string EvaluationAmount { get; init; } = string.Empty;

    /// <summary>
    /// 외화평가손익금액
    /// </summary>
    [JsonPropertyName("frcr_evlu_pfls_amt")]
    public string ProfitLoss { get; init; } = string.Empty;

    /// <summary>
    /// 평가손익율
    /// </summary>
    [JsonPropertyName("evlu_pfls_rt")]
    public string ProfitLossRate { get; init; } = string.Empty;

    /// <summary>
    /// 거래통화코드
    /// </summary>
    [JsonPropertyName("tr_crcy_cd")]
    public string CurrencyCode { get; init; } = string.Empty;

    /// <summary>
    /// 주문가능수량
    /// </summary>
    [JsonPropertyName("ord_psbl_qty")]
    public string OrderableQuantity { get; init; } = string.Empty;

    /// <summary>
    /// 대출유형코드
    /// </summary>
    [JsonPropertyName("loan_type_cd")]
    public string LoanTypeCode { get; init; } = string.Empty;

    /// <summary>
    /// 대출일자
    /// </summary>
    [JsonPropertyName("loan_dt")]
    public string LoanDate { get; init; } = string.Empty;

    /// <summary>
    /// 만기일자
    /// </summary>
    [JsonPropertyName("expd_dt")]
    public string ExpiryDate { get; init; } = string.Empty;
}