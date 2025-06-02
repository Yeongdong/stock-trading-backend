using StockTrading.Application.DTOs.Trading.Inquiry;
using StockTrading.Application.DTOs.Users;

namespace StockTrading.Application.Services;

/// <summary>
/// 주식 현재가 조회 서비스 인터페이스
/// </summary>
public interface ICurrentPriceService
{
    /// <summary>
    /// 주식 현재가 조회
    /// </summary>
    Task<CurrentPriceResponse> GetCurrentPriceAsync(CurrentPriceRequest request, UserInfo userInfo);
}