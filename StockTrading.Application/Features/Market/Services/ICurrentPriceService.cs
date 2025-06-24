using StockTrading.Application.Features.Trading.DTOs.Inquiry;
using StockTrading.Application.Features.Users.DTOs;

namespace StockTrading.Application.Features.Market.Services;

public interface ICurrentPriceService
{
    // 국내 주식
    Task<DomesticCurrentPriceResponse> GetDomesticCurrentPriceAsync(CurrentPriceRequest request, UserInfo userInfo);
    
    // 해외 주식  
    Task<OverseasCurrentPriceResponse> GetOverseasCurrentPriceAsync(string stockCode, Domain.Enums.Market market, UserInfo userInfo);
}