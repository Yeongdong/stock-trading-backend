using StockTrading.Application.DTOs.External.KoreaInvestment.Requests;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.Features.Trading.DTOs.Inquiry;
using StockTrading.Application.Features.Trading.DTOs.Portfolio;
using StockTrading.Application.Features.Users.DTOs;

namespace StockTrading.Application.ExternalServices;

public interface IKisBalanceApiClient
{
    // 국내 주식 잔고
    Task<AccountBalance> GetStockBalanceAsync(UserInfo user, CancellationToken cancellationToken = default);
    Task<BuyableInquiryResponse> GetBuyableInquiryAsync(BuyableInquiryRequest request, UserInfo user);
    
    // 해외 주식 잔고
    Task<OverseasAccountBalance> GetOverseasStockBalanceAsync(UserInfo user);
    Task<KisOverseasStockSearchResponse> SearchOverseasStocksAsync(KisOverseasStockSearchRequest request, UserInfo user);
}