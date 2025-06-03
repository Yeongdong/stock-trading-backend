namespace StockTrading.Domain.Settings;

public class KisApiEndpoints
{
    public string TokenPath { get; set; } = "/oauth2/tokenP";
    public string WebSocketApprovalPath { get; set; } = "/oauth2/Approval";
    public string OrderPath { get; init; } = "/uapi/domestic-stock/v1/trading/order-cash";
    public string BalancePath { get; init; } = "/uapi/domestic-stock/v1/trading/inquire-balance";
    public string OrderExecutionPath { get; set; } = "/uapi/domestic-stock/v1/trading/inquire-daily-ccld";
    public string BuyableInquiryPath { get; set; } = "/uapi/domestic-stock/v1/trading/inquire-psbl-order";
    public string CurrentPricePath { get; set; } = "/uapi/domestic-stock/v1/quotations/inquire-price";
}