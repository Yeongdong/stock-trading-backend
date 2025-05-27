using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

public class KisAccountSummaryResponse
{
    [JsonPropertyName("dnca_tot_amt")]
    public string TotalDeposit { get; init; }
    
    [JsonPropertyName("nxdy_excc_amt")]
    public string NextDaySettlementAmount { get; set; }
    
    [JsonPropertyName("prvs_rcdl_excc_amt")]
    public string D2SettlementAmount { get; set; }
    
    [JsonPropertyName("cma_evlu_amt")]
    public string CmaEvaluationAmount { get; set; }
    
    [JsonPropertyName("bfdy_buy_amt")]
    public string PreviousDayBuyAmount { get; set; }
    
    [JsonPropertyName("thdt_buy_amt")]
    public string TodayBuyAmount { get; set; }
    
    [JsonPropertyName("nxdy_auto_rdpt_amt")]
    public string NextDayAutoRedemptionAmount { get; set; }
    
    [JsonPropertyName("bfdy_sll_amt")]
    public string PreviousDaySellAmount { get; set; }
    
    [JsonPropertyName("thdt_sll_amt")]
    public string TodaySellAmount { get; set; }
    
    [JsonPropertyName("d2_auto_rdpt_amt")]
    public string D2AutoRedemptionAmount { get; set; }
    
    [JsonPropertyName("bfdy_tlex_amt")]
    public string PreviousDayExpenseAmount { get; set; }
    
    [JsonPropertyName("thdt_tlex_amt")]
    public string TodayExpenseAmount { get; set; }
    
    [JsonPropertyName("tot_loan_amt")]
    public string TotalLoanAmount { get; set; }
    
    [JsonPropertyName("scts_evlu_amt")]
    public string StockEvaluation { get; set; }
    
    [JsonPropertyName("tot_evlu_amt")]
    public string TotalEvaluation { get; set; }
    
    [JsonPropertyName("nass_amt")]
    public string NetAssetAmount { get; set; }
    
    [JsonPropertyName("fncg_gld_auto_rdpt_yn")]
    public string FinancingGoldAutoRedemptionYn { get; set; }
    
    [JsonPropertyName("pchs_amt_smtl_amt")]
    public string TotalPurchaseAmount { get; set; }
    
    [JsonPropertyName("evlu_amt_smtl_amt")]
    public string TotalEvaluationAmount { get; set; }
    
    [JsonPropertyName("evlu_pfls_smtl_amt")]
    public string TotalProfitLossAmount { get; set; }
    
    [JsonPropertyName("tot_stln_slng_chgs")]
    public string TotalShortSellingCharges { get; set; }
    
    [JsonPropertyName("bfdy_tot_asst_evlu_amt")]
    public string PreviousDayTotalAssetEvaluation { get; set; }
    
    [JsonPropertyName("asst_icdc_amt")]
    public string AssetChangeAmount { get; set; }
    
    [JsonPropertyName("asst_icdc_erng_rt")]
    public string AssetChangeRate { get; set; }
}