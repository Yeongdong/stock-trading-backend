using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Requests;

/// <summary>
/// KIS API 해외주식 기간별시세 요청
/// </summary>
public class KisOverseasPeriodPriceRequest
{
    /// <summary>
    /// FID 조건 시장 분류 코드 (N: 해외지수, X: 환율, I: 국채, S: 금선물)
    /// </summary>
    [JsonPropertyName("FID_COND_MRKT_DIV_CODE")]
    public string FidCondMrktDivCode { get; set; } = "N";

    /// <summary>
    /// FID 입력 종목코드 (예: .DJI, AAPL 등)
    /// </summary>
    [JsonPropertyName("FID_INPUT_ISCD")]
    public string FidInputIscd { get; set; } = string.Empty;

    /// <summary>
    /// FID 입력 날짜1 (시작일자 YYYYMMDD)
    /// </summary>
    [JsonPropertyName("FID_INPUT_DATE_1")]
    public string FidInputDate1 { get; set; } = string.Empty;

    /// <summary>
    /// FID 입력 날짜2 (종료일자 YYYYMMDD)
    /// </summary>
    [JsonPropertyName("FID_INPUT_DATE_2")]
    public string FidInputDate2 { get; set; } = string.Empty;

    /// <summary>
    /// FID 기간 분류 코드 (D:일, W:주, M:월, Y:년)
    /// </summary>
    [JsonPropertyName("FID_PERIOD_DIV_CODE")]
    public string FidPeriodDivCode { get; set; } = "D";
}