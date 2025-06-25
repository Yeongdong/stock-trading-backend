using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

/// <summary>
/// KIS API 해외 주식 체결 내역 조회 응답
/// </summary>
public class KisOverseasOrderExecutionResponse : KisBaseResponse<List<KisOverseasOrderExecutionData>>
{
}

/// <summary>
/// KIS API 해외 주식 체결 내역 데이터
/// </summary>
public class KisOverseasOrderExecutionData
{
    /// <summary>
    /// 체결번호
    /// </summary>
    [JsonPropertyName("excn_no")]
    public string ExecutionNumber { get; set; } = null!;

    /// <summary>
    /// 주문번호
    /// </summary>
    [JsonPropertyName("odno")]
    public string OrderNumber { get; set; } = null!;

    /// <summary>
    /// 원주문번호
    /// </summary>
    [JsonPropertyName("orgn_odno")]
    public string OriginalOrderNumber { get; set; } = null!;

    /// <summary>
    /// 상품번호 (종목코드)
    /// </summary>
    [JsonPropertyName("pdno")]
    public string StockCode { get; set; } = null!;

    /// <summary>
    /// 상품명
    /// </summary>
    [JsonPropertyName("prdt_name")]
    public string StockName { get; set; } = null!;

    /// <summary>
    /// 체결수량
    /// </summary>
    [JsonPropertyName("ft_ccld_qty")]
    public string ExecutedQuantity { get; set; } = null!;

    /// <summary>
    /// 체결단가
    /// </summary>
    [JsonPropertyName("ft_ccld_unpr")]
    public string ExecutedPrice { get; set; } = null!;

    /// <summary>
    /// 체결금액
    /// </summary>
    [JsonPropertyName("ft_ccld_amt")]
    public string ExecutedAmount { get; set; } = null!;

    /// <summary>
    /// 매도매수구분코드
    /// </summary>
    [JsonPropertyName("sll_buy_dvsn_cd")]
    public string TradeTypeCode { get; set; } = null!;

    /// <summary>
    /// 체결일자
    /// </summary>
    [JsonPropertyName("ccld_dt")]
    public string ExecutionDate { get; set; } = null!;

    /// <summary>
    /// 체결시각
    /// </summary>
    [JsonPropertyName("ccld_tmd")]
    public string ExecutionTime { get; set; } = null!;

    /// <summary>
    /// 거래통화코드
    /// </summary>
    [JsonPropertyName("tr_crcy_cd")]
    public string CurrencyCode { get; set; } = null!;

    /// <summary>
    /// 환율
    /// </summary>
    [JsonPropertyName("ccld_exrt")]
    public string ExchangeRate { get; set; } = null!;
}