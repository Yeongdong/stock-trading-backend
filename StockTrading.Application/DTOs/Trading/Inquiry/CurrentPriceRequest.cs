using System.ComponentModel.DataAnnotations;

namespace StockTrading.Application.DTOs.Trading.Inquiry;

/// <summary>
/// 주식 현재가 조회 요청 DTO
/// </summary>
public class CurrentPriceRequest
{
    /// <summary>
    /// 종목코드 (6자리)
    /// </summary>
    [Required(ErrorMessage = "종목코드는 필수입니다.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "종목코드는 6자리 숫자여야 합니다.")]
    public string StockCode { get; init; } = string.Empty;
}