using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StockTrading.Application.Features.Trading.DTOs.Inquiry;

public class PeriodPriceRequest
{
    [Required(ErrorMessage = "종목코드는 필수입니다.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "종목코드는 6자리여야 합니다.")]
    [JsonPropertyName("stockCode")]
    public string StockCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "기간분류코드는 필수입니다.")]
    [RegularExpression("^(D|W|M|Y)$", ErrorMessage = "기간분류코드는 D, W, M, Y 중 하나여야 합니다.")]
    [JsonPropertyName("periodDivCode")]
    public string PeriodDivCode { get; set; } = "D";

    [Required(ErrorMessage = "조회 시작일자는 필수입니다.")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "조회 시작일자는 YYYYMMDD 형식이어야 합니다.")]
    [JsonPropertyName("startDate")]
    public string StartDate { get; set; } = string.Empty;

    [Required(ErrorMessage = "조회 종료일자는 필수입니다.")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "조회 종료일자는 YYYYMMDD 형식이어야 합니다.")]
    [JsonPropertyName("endDate")]
    public string EndDate { get; set; } = string.Empty;

    [RegularExpression("^(0|1)$", ErrorMessage = "수정주가 여부는 0 또는 1이어야 합니다.")]
    [JsonPropertyName("orgAdjPrc")]
    public string OrgAdjPrc { get; set; } = "0";

    [RegularExpression("^(J|NX|UN)$", ErrorMessage = "시장 분류 코드는 J, NX, UN 중 하나여야 합니다.")]
    [JsonPropertyName("marketDivCode")]
    public string MarketDivCode { get; set; } = "J";
}