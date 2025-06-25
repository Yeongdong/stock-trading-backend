using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

/// <summary>
/// 해외 주식 잔고 데이터
/// </summary>
public class KisOverseasBalanceData
{
    /// <summary>
    /// 종목코드
    /// </summary>
    [JsonPropertyName("pdno")]
    public string StockCode { get; init; } = string.Empty;

    /// <summary>
    /// 종목명
    /// </summary>
    [JsonPropertyName("prdt_name")]
    public string StockName { get; init; } = string.Empty;

    /// <summary>
    /// 거래소코드
    /// </summary>
    [JsonPropertyName("ovrs_excg_cd")]
    public string ExchangeCode { get; init; } = string.Empty;

    /// <summary>
    /// 보유수량
    /// </summary>
    [JsonPropertyName("ovrs_cblc_qty")]
    public string Quantity { get; init; } = string.Empty;

    /// <summary>
    /// 매입평균가격(현지통화)
    /// </summary>
    [JsonPropertyName("pchs_avg_pric")]
    public string AveragePrice { get; init; } = string.Empty;

    /// <summary>
    /// 현재가(현지통화)
    /// </summary>
    [JsonPropertyName("ovrs_now_pric1")]
    public string CurrentPrice { get; init; } = string.Empty;

    /// <summary>
    /// 매입금액(현지통화)
    /// </summary>
    [JsonPropertyName("frcr_pchs_amt1")]
    public string PurchaseAmount { get; init; } = string.Empty;

    /// <summary>
    /// 평가금액(현지통화)
    /// </summary>
    [JsonPropertyName("ovrs_stck_evlu_amt")]
    public string EvaluationAmount { get; init; } = string.Empty;

    /// <summary>
    /// 평가손익(현지통화)
    /// </summary>
    [JsonPropertyName("frcr_evlu_pfls")]
    public string ProfitLoss { get; init; } = string.Empty;

    /// <summary>
    /// 평가손익률
    /// </summary>
    [JsonPropertyName("evlu_pfls_rt")]
    public string ProfitLossRate { get; init; } = string.Empty;

    /// <summary>
    /// 현지통화코드
    /// </summary>
    [JsonPropertyName("tr_crcy_cd")]
    public string CurrencyCode { get; init; } = string.Empty;

    /// <summary>
    /// 매입금액(원화)
    /// </summary>
    [JsonPropertyName("pchs_amt_smtl_amt")]
    public string PurchaseAmountKrw { get; init; } = string.Empty;

    /// <summary>
    /// 평가금액(원화)
    /// </summary>
    [JsonPropertyName("evlu_amt_smtl_amt")]
    public string EvaluationAmountKrw { get; init; } = string.Empty;

    /// <summary>
    /// 평가손익(원화)
    /// </summary>
    [JsonPropertyName("evlu_pfls_smtl_amt")]
    public string ProfitLossKrw { get; init; } = string.Empty;

    /// <summary>
    /// 환율
    /// </summary>
    [JsonPropertyName("bass_exrt")]
    public string ExchangeRate { get; init; } = string.Empty;
}