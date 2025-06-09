using System.ComponentModel.DataAnnotations;

namespace StockTrading.Application.Features.Trading.DTOs.Inquiry;

public class BuyableInquiryRequest
{
    [Required(ErrorMessage = "종목코드는 필수입니다.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "종목코드는 6자리 숫자여야 합니다.")]
    public string StockCode { get; init; } = string.Empty;

    [Required(ErrorMessage = "주문단가는 필수입니다.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "주문단가는 0보다 커야 합니다.")]
    public decimal OrderPrice { get; init; }

    public string OrderType { get; init; } = "00"; // 기본값: 지정가
}