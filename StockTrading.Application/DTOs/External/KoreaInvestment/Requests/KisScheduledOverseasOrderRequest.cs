using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Requests;

/// <summary>
/// KIS API 해외주식 예약주문 요청
/// </summary>
public class KisScheduledOverseasOrderRequest
{
    /// <summary>
    /// 종합계좌번호
    /// </summary>
    [JsonPropertyName("CANO")]
    public string CANO { get; set; } = null!;

    /// <summary>
    /// 계좌상품코드
    /// </summary>
    [JsonPropertyName("ACNT_PRDT_CD")]
    public string ACNT_PRDT_CD { get; set; } = null!;

    /// <summary>
    /// 매도매수구분코드 (TTTS3013U용)
    /// </summary>
    [JsonPropertyName("SLL_BUY_DVSN_CD")]
    public string? SLL_BUY_DVSN_CD { get; set; }

    /// <summary>
    /// 정정취소구분코드 (TTTS3013U용)
    /// </summary>
    [JsonPropertyName("RVSE_CNCL_DVSN_CD")]
    public string? RVSE_CNCL_DVSN_CD { get; set; }

    /// <summary>
    /// 상품번호 (종목코드)
    /// </summary>
    [JsonPropertyName("PDNO")]
    public string PDNO { get; set; } = null!;

    /// <summary>
    /// 상품유형코드 (TTTS3013U용)
    /// </summary>
    [JsonPropertyName("PRDT_TYPE_CD")]
    public string? PRDT_TYPE_CD { get; set; }

    /// <summary>
    /// 해외거래소코드
    /// </summary>
    [JsonPropertyName("OVRS_EXCG_CD")]
    public string OVRS_EXCG_CD { get; set; } = null!;

    /// <summary>
    /// FT주문수량
    /// </summary>
    [JsonPropertyName("FT_ORD_QTY")]
    public string FT_ORD_QTY { get; set; } = null!;

    /// <summary>
    /// FT주문단가3
    /// </summary>
    [JsonPropertyName("FT_ORD_UNPR3")]
    public string FT_ORD_UNPR3 { get; set; } = null!;

    /// <summary>
    /// 주문서버구분코드
    /// </summary>
    [JsonPropertyName("ORD_SVR_DVSN_CD")]
    public string ORD_SVR_DVSN_CD { get; set; } = "0";

    /// <summary>
    /// 예약주문접수일자 (TTTS3013U용)
    /// </summary>
    [JsonPropertyName("RSVN_ORD_RCIT_DT")]
    public string? RSVN_ORD_RCIT_DT { get; set; }

    /// <summary>
    /// 주문구분 (미국주문용)
    /// </summary>
    [JsonPropertyName("ORD_DVSN")]
    public string? ORD_DVSN { get; set; }

    /// <summary>
    /// 해외예약주문번호 (TTTS3013U용)
    /// </summary>
    [JsonPropertyName("OVRS_RSVN_ODNO")]
    public string? OVRS_RSVN_ODNO { get; set; }

    /// <summary>
    /// 알고리즘주문시간구분코드
    /// </summary>
    [JsonPropertyName("ALGO_ORD_TMD_DVSN_CD")]
    public string? ALGO_ORD_TMD_DVSN_CD { get; set; }
}