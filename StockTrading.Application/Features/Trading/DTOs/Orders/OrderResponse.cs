using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

namespace StockTrading.Application.Features.Trading.DTOs.Orders;

public class OrderResponse : KisBaseResponse<KisOrderData>
{
    public string? OrderNumber => Output?.OrderNumber;
    public string? OrderTime => Output?.OrderTime;
}
