using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.Features.Trading.DTOs.Orders;
using StockTrading.Application.Features.Users.DTOs;

namespace StockTrading.Application.ExternalServices;

public interface IKisOverseasTradingApiClient
{
    Task<OverseasOrderResponse> PlaceOverseasOrderAsync(OverseasOrderRequest request, UserInfo user);
    Task<OverseasOrderResponse> PlaceScheduledOverseasOrderAsync(ScheduledOverseasOrderRequest request, UserInfo user);
    Task<KisOverseasOrderExecutionResponse> GetOverseasOrderExecutionsAsync(string startDate, string endDate, UserInfo user);
}