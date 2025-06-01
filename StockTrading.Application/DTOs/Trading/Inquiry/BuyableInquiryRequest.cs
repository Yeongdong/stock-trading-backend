using System.ComponentModel.DataAnnotations;

namespace StockTrading.Application.DTOs.Trading.Inquiry;

/// <summary>
/// 매수가능조회 요청 DTO
/// </summary>
public class BuyableInquiryRequest
{
    /// <summary>
    /// 종목코드 (6자리)
    /// </summary>
    [Required(ErrorMessage = "종목코드는 필수입니다.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "종목코드는 6자리 숫자여야 합니다.")]
    public string StockCode { get; init; } = string.Empty;

    /// <summary>
    /// 주문단가
    /// </summary>
    [Required(ErrorMessage = "주문단가는 필수입니다.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "주문단가는 0보다 커야 합니다.")]
    public decimal OrderPrice { get; init; }

    /// <summary>
    /// 주문구분 (00:지정가, 01:시장가 등)
    /// </summary>
    public string OrderType { get; init; } = "00"; // 기본값: 지정가
}