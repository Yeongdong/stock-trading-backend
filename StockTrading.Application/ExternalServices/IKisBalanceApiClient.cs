using StockTrading.Application.DTOs.Trading.Inquiry;
using StockTrading.Application.DTOs.Trading.Portfolio;
using StockTrading.Application.DTOs.Users;

namespace StockTrading.Application.ExternalServices;

public interface IKisBalanceApiClient
{
    Task<AccountBalance> GetStockBalanceAsync(UserInfo user);
    Task<BuyableInquiryResponse> GetBuyableInquiryAsync(BuyableInquiryRequest request, UserInfo user);
}