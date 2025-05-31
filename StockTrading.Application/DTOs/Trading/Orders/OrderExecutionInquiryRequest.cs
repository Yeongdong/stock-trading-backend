using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.Trading.Orders;

/// <summary>
/// 주문체결조회 요청 DTO
/// </summary>
public class OrderExecutionInquiryRequest
{
    /// <summary>
    /// 조회시작일자 (YYYYMMDD)
    /// </summary>
    [Required(ErrorMessage = "조회시작일자는 필수입니다.")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "조회시작일자는 YYYYMMDD 형식이어야 합니다.")]
    [JsonPropertyName("startDate")]
    public string StartDate { get; init; } = string.Empty;

    /// <summary>
    /// 조회종료일자 (YYYYMMDD)
    /// </summary>
    [Required(ErrorMessage = "조회종료일자는 필수입니다.")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "조회종료일자는 YYYYMMDD 형식이어야 합니다.")]
    [JsonPropertyName("endDate")]
    public string EndDate { get; init; } = string.Empty;

    /// <summary>
    /// 종목코드 (선택사항, 6자리)
    /// </summary>
    [RegularExpression(@"^\d{6}$", ErrorMessage = "종목코드는 6자리 숫자여야 합니다.")]
    [JsonPropertyName("stockCode")]
    public string? StockCode { get; init; }

    /// <summary>
    /// 주문구분 (01:매도, 02:매수, 00:전체)
    /// </summary>
    [JsonPropertyName("orderType")]
    public string OrderType { get; init; } = "00"; // 기본값: 전체
}