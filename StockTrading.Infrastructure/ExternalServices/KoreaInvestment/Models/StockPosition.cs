using System.Text.Json.Serialization;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Models;

public class StockPosition
{
    [JsonPropertyName("pdno")]
    public string StockCode { get; set; }
    
    [JsonPropertyName("prdt_name")]
    public string StockName { get; set; }
    
    [JsonPropertyName("trad_dvsn_name")]
    public string TradingType { get; set; }
    
    [JsonPropertyName("bfdy_buy_qty")]
    public string PreviousDayBuyQuantity { get; set; }
    
    [JsonPropertyName("bfdy_sll_qty")]
    public string PreviousDaySellQuantity { get; set; }
    
    [JsonPropertyName("thdt_buyqty")]
    public string TodayBuyQuantity { get; set; }
    
    [JsonPropertyName("thdt_sll_qty")]
    public string TodaySellQuantity { get; set; }
    
    [JsonPropertyName("hldg_qty")]
    public string Quantity { get; set; }
    
    [JsonPropertyName("ord_psbl_qty")]
    public string OrderableQuantity { get; set; }
    
    [JsonPropertyName("pchs_avg_pric")]
    public string AveragePrice { get; set; }
    
    [JsonPropertyName("pchs_amt")]
    public string PurchaseAmount { get; set; }
    
    [JsonPropertyName("prpr")]
    public string CurrentPrice { get; set; }
    
    [JsonPropertyName("evlu_amt")]
    public string EvaluationAmount { get; set; }
    
    [JsonPropertyName("evlu_pfls_amt")]
    public string ProfitLoss { get; set; }
    
    [JsonPropertyName("evlu_pfls_rt")]
    public string ProfitLossRate { get; set; }
    
    [JsonPropertyName("loan_dt")]
    public string LoanDate { get; set; }
    
    [JsonPropertyName("loan_amt")]
    public string LoanAmount { get; set; }
    
    [JsonPropertyName("stln_slng_chgs")]
    public string ShortSellingCharges { get; set; }
    
    [JsonPropertyName("expd_dt")]
    public string ExpirationDate { get; set; }
    
    [JsonPropertyName("fltt_rt")]
    public string FluctuationRate { get; set; }
    
    [JsonPropertyName("bfdy_cprs_icdc")]
    public string PreviousDayChange { get; set; }
    
    [JsonPropertyName("item_mgna_rt_name")]
    public string MarginRateName { get; set; }
    
    [JsonPropertyName("grta_rt_name")]
    public string GuaranteeRateName { get; set; }
    
    [JsonPropertyName("sbst_pric")]
    public string CollateralPrice { get; set; }
    
    [JsonPropertyName("stck_loan_unpr")]
    public string StockLoanUnitPrice { get; set; }
}