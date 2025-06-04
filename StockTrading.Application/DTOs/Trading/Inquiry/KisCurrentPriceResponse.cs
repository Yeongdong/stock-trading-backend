namespace StockTrading.Application.DTOs.Trading.Inquiry;

/// <summary>
/// 주식 현재가 조회 응답 DTO
/// </summary>
public class KisCurrentPriceResponse
{
    /// <summary>종목코드</summary>
    public string StockCode { get; init; } = string.Empty;

    /// <summary>종목명</summary>
    public string StockName { get; init; } = string.Empty;

    /// <summary>현재가</summary>
    public decimal CurrentPrice { get; init; }

    /// <summary>전일대비</summary>
    public decimal PriceChange { get; init; }

    /// <summary>등락률</summary>
    public decimal ChangeRate { get; init; }

    /// <summary>등락구분 (상승/하락/보합)</summary>
    public string ChangeType { get; init; } = string.Empty;

    /// <summary>시가</summary>
    public decimal OpenPrice { get; init; }

    /// <summary>고가</summary>
    public decimal HighPrice { get; init; }

    /// <summary>저가</summary>
    public decimal LowPrice { get; init; }

    /// <summary>거래량</summary>
    public long Volume { get; init; }

    /// <summary>조회시간</summary>
    public DateTime InquiryTime { get; init; } = DateTime.Now;
}