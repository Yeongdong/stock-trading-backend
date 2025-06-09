using StockTrading.Application.Features.Trading.DTOs.Inquiry;
using StockTrading.Application.Features.Trading.DTOs.Portfolio;
using StockTrading.Application.Features.Users.DTOs;

namespace StockTrading.Application.ExternalServices;

public interface IKisBalanceApiClient
{
    Task<AccountBalance> GetStockBalanceAsync(UserInfo user);
    Task<BuyableInquiryResponse> GetBuyableInquiryAsync(BuyableInquiryRequest request, UserInfo user);
}