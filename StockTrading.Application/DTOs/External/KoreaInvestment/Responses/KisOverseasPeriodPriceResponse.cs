using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

/// <summary>
/// KIS API 해외주식 기간별시세 응답
/// </summary>
public class KisOverseasPeriodPriceResponse : KisBaseResponse<KisOverseasPeriodPriceData>
{
    /// <summary>
    /// 일자별 정보
    /// </summary>
    [JsonPropertyName("output2")]
    public List<KisOverseasPriceItem> PriceItems { get; set; } = [];
}

/// <summary>
/// KIS API 해외주식 기간별시세 기본정보 (output1)
/// </summary>
public class KisOverseasPeriodPriceData
{
    /// <summary>
    /// 전일 대비
    /// </summary>
    [JsonPropertyName("ovrs_nmix_prdy_vrss")]
    public string PreviousDayChange { get; set; } = string.Empty;

    /// <summary>
    /// 전일 대비 부호
    /// </summary>
    [JsonPropertyName("prdy_vrss_sign")]
    public string PreviousDayChangeSign { get; set; } = string.Empty;

    /// <summary>
    /// 전일 대비율
    /// </summary>
    [JsonPropertyName("prdy_ctrt")]
    public string PreviousDayChangeRate { get; set; } = string.Empty;

    /// <summary>
    /// 전일 종가
    /// </summary>
    [JsonPropertyName("ovrs_nmix_prdy_clpr")]
    public string PreviousClosePrice { get; set; } = string.Empty;

    /// <summary>
    /// 누적 거래량
    /// </summary>
    [JsonPropertyName("acml_vol")]
    public string AccumulatedVolume { get; set; } = string.Empty;

    /// <summary>
    /// HTS 한글 종목명
    /// </summary>
    [JsonPropertyName("hts_kor_isnm")]
    public string StockName { get; set; } = string.Empty;

    /// <summary>
    /// 현재가
    /// </summary>
    [JsonPropertyName("ovrs_nmix_prpr")]
    public string CurrentPrice { get; set; } = string.Empty;

    /// <summary>
    /// 단축 종목코드
    /// </summary>
    [JsonPropertyName("stck_shrn_iscd")]
    public string StockCode { get; set; } = string.Empty;

    /// <summary>
    /// 시가
    /// </summary>
    [JsonPropertyName("ovrs_prod_oprc")]
    public string OpenPrice { get; set; } = string.Empty;

    /// <summary>
    /// 최고가
    /// </summary>
    [JsonPropertyName("ovrs_prod_hgpr")]
    public string HighPrice { get; set; } = string.Empty;

    /// <summary>
    /// 최저가
    /// </summary>
    [JsonPropertyName("ovrs_prod_lwpr")]
    public string LowPrice { get; set; } = string.Empty;
}

/// <summary>
/// KIS API 해외주식 일자별 정보 (output2)
/// </summary>
public class KisOverseasPriceItem
{
    /// <summary>
    /// 영업 일자
    /// </summary>
    [JsonPropertyName("stck_bsop_date")]
    public string BusinessDate { get; set; } = string.Empty;

    /// <summary>
    /// 현재가 (종가)
    /// </summary>
    [JsonPropertyName("ovrs_nmix_prpr")]
    public string ClosePrice { get; set; } = string.Empty;

    /// <summary>
    /// 시가
    /// </summary>
    [JsonPropertyName("ovrs_nmix_oprc")]
    public string OpenPrice { get; set; } = string.Empty;

    /// <summary>
    /// 최고가
    /// </summary>
    [JsonPropertyName("ovrs_nmix_hgpr")]
    public string HighPrice { get; set; } = string.Empty;

    /// <summary>
    /// 최저가
    /// </summary>
    [JsonPropertyName("ovrs_nmix_lwpr")]
    public string LowPrice { get; set; } = string.Empty;

    /// <summary>
    /// 누적 거래량
    /// </summary>
    [JsonPropertyName("acml_vol")]
    public string Volume { get; set; } = string.Empty;

    /// <summary>
    /// 변경 여부
    /// </summary>
    [JsonPropertyName("mod_yn")]
    public string ModifiedYn { get; set; } = string.Empty;
}