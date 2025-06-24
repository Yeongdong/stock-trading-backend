using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Requests;

/// <summary>
/// KIS API 해외 주식 주문 요청
/// </summary>
public class KisOverseasOrderRequest
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
    /// 해외거래소코드 (NASD: 나스닥, NYSE: 뉴욕, TKSE: 도쿄 등)
    /// </summary>
    [JsonPropertyName("OVRS_EXCG_CD")]
    public string OVRS_EXCG_CD { get; set; } = null!;

    /// <summary>
    /// 상품번호 (종목코드)
    /// </summary>
    [JsonPropertyName("PDNO")]
    public string PDNO { get; set; } = null!;

    /// <summary>
    /// 주문수량
    /// </summary>
    [JsonPropertyName("ORD_QTY")]
    public string ORD_QTY { get; set; } = null!;

    /// <summary>
    /// 해외주문단가
    /// </summary>
    [JsonPropertyName("OVRS_ORD_UNPR")]
    public string OVRS_ORD_UNPR { get; set; } = null!;

    /// <summary>
    /// 주문서버구분코드 (00: 지정가, 01: 시장가)
    /// </summary>
    [JsonPropertyName("ORD_SVR_DVSN_CD")]
    public string ORD_SVR_DVSN_CD { get; set; } = null!;

    /// <summary>
    /// 주문구분 (FTC: Fill or Kill, DAY: 당일)
    /// </summary>
    [JsonPropertyName("ORD_DVSN")]
    public string ORD_DVSN { get; set; } = null!;
}