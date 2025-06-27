namespace StockTrading.Application.Features.Trading.DTOs.Inquiry;

/// <summary>
/// 해외주식 기간별시세 응답
/// </summary>
public class OverseasPeriodPriceResponse
{
    /// <summary>
    /// 종목코드
    /// </summary>
    public string StockCode { get; set; } = string.Empty;

    /// <summary>
    /// 종목명
    /// </summary>
    public string StockName { get; set; } = string.Empty;

    /// <summary>
    /// 현재가
    /// </summary>
    public decimal CurrentPrice { get; set; }

    /// <summary>
    /// 전일대비
    /// </summary>
    public decimal PriceChange { get; set; }

    /// <summary>
    /// 전일대비율
    /// </summary>
    public decimal ChangeRate { get; set; }

    /// <summary>
    /// 전일대비 부호
    /// </summary>
    public string ChangeSign { get; set; } = string.Empty;

    /// <summary>
    /// 총거래량
    /// </summary>
    public long TotalVolume { get; set; }

    /// <summary>
    /// 기간별 가격 데이터
    /// </summary>
    public List<OverseasPriceData> PriceData { get; set; } = [];
}

/// <summary>
/// 해외주식 기간별 가격 데이터
/// </summary>
public class OverseasPriceData
{
    /// <summary>
    /// 날짜 (YYYYMMDD)
    /// </summary>
    public string Date { get; set; } = string.Empty;

    /// <summary>
    /// 시가
    /// </summary>
    public decimal OpenPrice { get; set; }

    /// <summary>
    /// 고가
    /// </summary>
    public decimal HighPrice { get; set; }

    /// <summary>
    /// 저가
    /// </summary>
    public decimal LowPrice { get; set; }

    /// <summary>
    /// 종가
    /// </summary>
    public decimal ClosePrice { get; set; }

    /// <summary>
    /// 거래량
    /// </summary>
    public long Volume { get; set; }
}