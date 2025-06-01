using StockTrading.Application.DTOs.Trading.Inquiry;
using StockTrading.Application.DTOs.Users;

namespace StockTrading.Application.Services;

public interface IBuyableInquiryService
{
    Task<BuyableInquiryResponse> GetBuyableInquiryAsync(BuyableInquiryRequest request, UserInfo userInfo);
}