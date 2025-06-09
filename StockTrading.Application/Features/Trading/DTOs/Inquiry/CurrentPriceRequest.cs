using System.ComponentModel.DataAnnotations;

namespace StockTrading.Application.Features.Trading.DTOs.Inquiry;

public class CurrentPriceRequest
{
    [Required(ErrorMessage = "종목코드는 필수입니다.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "종목코드는 6자리 숫자여야 합니다.")]
    public string StockCode { get; init; } = string.Empty;
}