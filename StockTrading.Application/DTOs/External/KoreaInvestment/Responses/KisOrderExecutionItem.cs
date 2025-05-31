using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

public class KisOrderExecutionItem
{
    [JsonPropertyName("ord_dt")] public string OrderDate { get; set; } = string.Empty;

    [JsonPropertyName("odno")] public string OrderNumber { get; set; } = string.Empty;

    [JsonPropertyName("orgn_odno")] public string OriginalOrderNumber { get; set; } = string.Empty;

    [JsonPropertyName("pdno")] public string StockCode { get; set; } = string.Empty;

    [JsonPropertyName("prdt_name")] public string StockName { get; set; } = string.Empty;

    [JsonPropertyName("sll_buy_dvsn_cd")] public string SellBuyDivisionCode { get; set; } = string.Empty;

    [JsonPropertyName("sll_buy_dvsn_cd_name")]
    public string SellBuyDivisionName { get; set; } = string.Empty;

    [JsonPropertyName("ord_qty")] public string OrderQuantity { get; set; } = string.Empty;

    [JsonPropertyName("ord_unpr")] public string OrderPrice { get; set; } = string.Empty;

    [JsonPropertyName("tot_ccld_qty")] public string TotalExecutedQuantity { get; set; } = string.Empty;

    [JsonPropertyName("avg_prvs")] public string AveragePrice { get; set; } = string.Empty;

    [JsonPropertyName("tot_ccld_amt")] public string TotalExecutedAmount { get; set; } = string.Empty;

    [JsonPropertyName("ord_stat_cd")] public string OrderStatusCode { get; set; } = string.Empty;

    [JsonPropertyName("ord_stat_name")] public string OrderStatusName { get; set; } = string.Empty;

    [JsonPropertyName("ord_tmd")] public string OrderTime { get; set; } = string.Empty;
}