using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StockTrading.Application.Features.Trading.DTOs.Orders;

public class OrderExecutionInquiryRequest
{
    [Required(ErrorMessage = "조회시작일자는 필수입니다.")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "조회시작일자는 YYYYMMDD 형식이어야 합니다.")]
    [JsonPropertyName("startDate")]
    public string StartDate { get; init; } = string.Empty;

    [Required(ErrorMessage = "조회종료일자는 필수입니다.")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "조회종료일자는 YYYYMMDD 형식이어야 합니다.")]
    [JsonPropertyName("endDate")]
    public string EndDate { get; init; } = string.Empty;

    [RegularExpression(@"^\d{6}$", ErrorMessage = "종목코드는 6자리 숫자여야 합니다.")]
    [JsonPropertyName("stockCode")]
    public string? StockCode { get; init; }

    [JsonPropertyName("orderType")]
    public string OrderType { get; init; } = "00"; // 기본값: 전체
}