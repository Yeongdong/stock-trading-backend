namespace StockTrading.Application.Features.Trading.DTOs.Inquiry;

/// <summary>
/// 해외주식 기간별시세 요청
/// </summary>
public class OverseasPeriodPriceRequest
{
    /// <summary>
    /// 종목코드 (예: .DJI, AAPL)
    /// </summary>
    public string StockCode { get; set; } = string.Empty;

    /// <summary>
    /// 시장 구분 (N: 해외지수, X: 환율)
    /// </summary>
    public string MarketDivCode { get; set; } = "N";

    /// <summary>
    /// 기간 구분 (D:일, W:주, M:월, Y:년)
    /// </summary>
    public string PeriodDivCode { get; set; } = "D";

    /// <summary>
    /// 시작일자 (YYYYMMDD)
    /// </summary>
    public string StartDate { get; set; } = string.Empty;

    /// <summary>
    /// 종료일자 (YYYYMMDD)
    /// </summary>
    public string EndDate { get; set; } = string.Empty;
}