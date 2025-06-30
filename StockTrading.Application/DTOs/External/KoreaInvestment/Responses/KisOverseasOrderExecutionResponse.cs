using System.Text.Json.Serialization;
using StockTrading.Application.DTOs.External.KoreaInvestment.Converters;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

/// <summary>
/// KIS API 해외 주식 체결 내역 조회 응답
/// </summary>
public class KisOverseasOrderExecutionResponse : KisBaseResponse<List<KisOverseasOrderExecutionData>>
{
    /// <summary>
    /// 연속조회검색조건200
    /// </summary>
    [JsonPropertyName("ctx_area_fk200")]
    public string CtxAreaFk200 { get; init; } = string.Empty;

    /// <summary>
    /// 연속조회키200
    /// </summary>
    [JsonPropertyName("ctx_area_nk200")]
    public string CtxAreaNk200 { get; init; } = string.Empty;

    /// <summary>
    /// 응답 데이터 - 배열 또는 단일 객체 모두 처리
    /// </summary>
    [JsonPropertyName("output")]
    [JsonConverter(typeof(FlexibleOutputJsonConverter))]
    public new List<KisOverseasOrderExecutionData>? Output { get; init; }
}

/// <summary>
/// KIS API 해외 주식 체결 내역 데이터
/// </summary>
public class KisOverseasOrderExecutionData
{
    /// <summary>
    /// 주문일자
    /// </summary>
    [JsonPropertyName("ord_dt")]
    public string OrderDate { get; set; } = string.Empty;

    /// <summary>
    /// 주문지점번호
    /// </summary>
    [JsonPropertyName("ord_gno_brno")]
    public string OrderBranchNumber { get; set; } = string.Empty;

    /// <summary>
    /// 주문번호
    /// </summary>
    [JsonPropertyName("odno")]
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// 원주문번호
    /// </summary>
    [JsonPropertyName("orgn_odno")]
    public string OriginalOrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// 매도매수구분코드
    /// </summary>
    [JsonPropertyName("sll_buy_dvsn_cd")]
    public string TradeTypeCode { get; set; } = string.Empty;

    /// <summary>
    /// 매도매수구분명
    /// </summary>
    [JsonPropertyName("sll_buy_dvsn_cd_name")]
    public string TradeTypeName { get; set; } = string.Empty;

    /// <summary>
    /// 취소정정구분
    /// </summary>
    [JsonPropertyName("rvse_cncl_dvsn")]
    public string RevisionCancelDivision { get; set; } = string.Empty;

    /// <summary>
    /// 취소정정구분명
    /// </summary>
    [JsonPropertyName("rvse_cncl_dvsn_name")]
    public string RevisionCancelDivisionName { get; set; } = string.Empty;

    /// <summary>
    /// 상품번호 (종목코드)
    /// </summary>
    [JsonPropertyName("pdno")]
    public string StockCode { get; set; } = string.Empty;

    /// <summary>
    /// 상품명
    /// </summary>
    [JsonPropertyName("prdt_name")]
    public string StockName { get; set; } = string.Empty;

    /// <summary>
    /// 주문수량
    /// </summary>
    [JsonPropertyName("ft_ord_qty")]
    public string OrderQuantity { get; set; } = string.Empty;

    /// <summary>
    /// 주문단가
    /// </summary>
    [JsonPropertyName("ft_ord_unpr3")]
    public string OrderPrice { get; set; } = string.Empty;

    /// <summary>
    /// 체결수량
    /// </summary>
    [JsonPropertyName("ft_ccld_qty")]
    public string ExecutedQuantity { get; set; } = string.Empty;

    /// <summary>
    /// 체결단가
    /// </summary>
    [JsonPropertyName("ft_ccld_unpr3")]
    public string ExecutedPrice { get; set; } = string.Empty;

    /// <summary>
    /// 체결금액
    /// </summary>
    [JsonPropertyName("ft_ccld_amt3")]
    public string ExecutedAmount { get; set; } = string.Empty;

    /// <summary>
    /// 미체결수량
    /// </summary>
    [JsonPropertyName("nccs_qty")]
    public string UnexecutedQuantity { get; set; } = string.Empty;

    /// <summary>
    /// 처리상태명
    /// </summary>
    [JsonPropertyName("prcs_stat_name")]
    public string ProcessStatusName { get; set; } = string.Empty;

    /// <summary>
    /// 거부사유
    /// </summary>
    [JsonPropertyName("rjct_rson")]
    public string RejectionReason { get; set; } = string.Empty;

    /// <summary>
    /// 거부사유명
    /// </summary>
    [JsonPropertyName("rjct_rson_name")]
    public string RejectionReasonName { get; set; } = string.Empty;

    /// <summary>
    /// 주문시각
    /// </summary>
    [JsonPropertyName("ord_tmd")]
    public string OrderTime { get; set; } = string.Empty;

    /// <summary>
    /// 거래시장명
    /// </summary>
    [JsonPropertyName("tr_mket_name")]
    public string MarketName { get; set; } = string.Empty;

    /// <summary>
    /// 거래국가
    /// </summary>
    [JsonPropertyName("tr_natn")]
    public string TradeNation { get; set; } = string.Empty;

    /// <summary>
    /// 거래국가명
    /// </summary>
    [JsonPropertyName("tr_natn_name")]
    public string TradeNationName { get; set; } = string.Empty;

    /// <summary>
    /// 해외거래소코드
    /// </summary>
    [JsonPropertyName("ovrs_excg_cd")]
    public string ExchangeCode { get; set; } = string.Empty;

    /// <summary>
    /// 거래통화코드
    /// </summary>
    [JsonPropertyName("tr_crcy_cd")]
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// 국내주문일자
    /// </summary>
    [JsonPropertyName("dmst_ord_dt")]
    public string DomesticOrderDate { get; set; } = string.Empty;

    /// <summary>
    /// 당사주문시각
    /// </summary>
    [JsonPropertyName("thco_ord_tmd")]
    public string CompanyOrderTime { get; set; } = string.Empty;

    /// <summary>
    /// 대출유형코드
    /// </summary>
    [JsonPropertyName("loan_type_cd")]
    public string LoanTypeCode { get; set; } = string.Empty;

    /// <summary>
    /// 대출일자
    /// </summary>
    [JsonPropertyName("loan_dt")]
    public string LoanDate { get; set; } = string.Empty;

    /// <summary>
    /// 매체구분명
    /// </summary>
    [JsonPropertyName("mdia_dvsn_name")]
    public string MediaDivisionName { get; set; } = string.Empty;

    /// <summary>
    /// 미국애프터마켓연장신청여부
    /// </summary>
    [JsonPropertyName("usa_amk_exts_rqst_yn")]
    public string UsAfterMarketExtensionRequestYn { get; set; } = string.Empty;

    /// <summary>
    /// 분할매수/매도속성명
    /// </summary>
    [JsonPropertyName("splt_buy_attr_name")]
    public string SplitBuyAttributeName { get; set; } = string.Empty;
}