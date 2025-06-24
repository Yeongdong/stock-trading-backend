using StockTrading.Application.Features.Trading.DTOs.Orders;
using StockTrading.Application.Features.Trading.DTOs.Portfolio;
using StockTrading.Application.Features.Users.DTOs;

namespace StockTrading.Application.ExternalServices;

public interface IKisOverseasTradingApiClient
{
    Task<OverseasOrderResponse> PlaceOverseasOrderAsync(OverseasOrderRequest request, UserInfo user);
    Task<List<OverseasOrderExecution>> GetOverseasOrderExecutionsAsync(string startDate, string endDate, UserInfo user);
}