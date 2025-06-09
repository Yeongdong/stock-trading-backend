using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.Features.Trading.DTOs.Orders;
using StockTrading.Domain.Settings.ExternalServices;
using StockTrading.Infrastructure.Utilities;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Trading.Converters;

public class OrderDataConverter
{
    public OrderExecutionInquiryResponse ConvertToOrderExecutionResponse(KisOrderExecutionInquiryResponse kisResponse)
    {
        var executionItems = kisResponse.ExecutionItems.Select(item => new OrderExecutionItem
        {
            OrderDate = item.OrderDate,
            OrderNumber = item.OrderNumber,
            StockCode = item.StockCode,
            StockName = item.StockName,
            OrderSide = item.SellBuyDivisionName,
            OrderQuantity = ParseHelper.ParseIntSafely(item.OrderQuantity),
            OrderPrice = ParseHelper.ParseDecimalSafely(item.OrderPrice),
            ExecutedQuantity = ParseHelper.ParseIntSafely(item.TotalExecutedQuantity),
            ExecutedPrice = ParseHelper.ParseDecimalSafely(item.AveragePrice),
            ExecutedAmount = ParseHelper.ParseDecimalSafely(item.TotalExecutedAmount),
            OrderStatus = item.OrderStatusName,
            ExecutionTime = item.OrderTime
        }).ToList();

        return new OrderExecutionInquiryResponse
        {
            ExecutionItems = executionItems,
            TotalCount = executionItems.Count,
            HasMore = !string.IsNullOrEmpty(kisResponse.CtxAreaNk100)
        };
    }

    public string ConvertOrderTypeToKisCode(string orderType, KoreaInvestmentSettings settings)
    {
        var defaults = settings.DefaultValues;
        return orderType switch
        {
            "01" => defaults.SellOrderCode,
            "02" => defaults.BuyOrderCode,
            _ => defaults.AllOrderCode
        };
    }
}