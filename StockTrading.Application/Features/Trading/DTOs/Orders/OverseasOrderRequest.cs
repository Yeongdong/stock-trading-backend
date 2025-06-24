using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StockTrading.Application.Features.Trading.DTOs.Orders;

/// <summary>
/// 해외 주식 주문 요청
/// </summary>
public class OverseasOrderRequest
{
    [JsonPropertyName("acntPrdtCd")]
    public string ACNT_PRDT_CD { get; set; } = "01";
    
    [JsonPropertyName("trId")]
    [Required(ErrorMessage = "거래구분은 필수입니다.")]
    public string tr_id { get; init; } = null!; // VTTT1002U(매수) 또는 VTTT1001U(매도)

    [JsonPropertyName("pdno")]
    [Required(ErrorMessage = "종목코드는 필수입니다.")]
    [StringLength(10)]
    public string PDNO { get; init; } = null!; // 해외 종목코드 (예: AAPL)
    
    [JsonPropertyName("ordDvsn")]
    [Required(ErrorMessage = "주문구분은 필수입니다.")]
    [RegularExpression("^(00|01)$")]
    public string ORD_DVSN { get; init; } = null!; // 00: 지정가, 01: 시장가
    
    [JsonPropertyName("ordQty")]
    [Required(ErrorMessage = "주문수량은 필수입니다.")]
    public string ORD_QTY { get; init; } = null!;
    
    [JsonPropertyName("ordUnpr")]
    [Required(ErrorMessage = "주문단가는 필수입니다.")]
    public string ORD_UNPR { get; set; } = null!;

    [JsonPropertyName("ovsExcgCd")]
    [Required(ErrorMessage = "해외거래소코드는 필수입니다.")]
    public string OVRS_EXCG_CD { get; init; } = null!; // NASD, NYSE, TKSE 등

    [JsonPropertyName("ordCndt")]
    public string ORD_CNDT { get; init; } = "DAY"; // DAY, FTC

    // Helper Properties
    [JsonIgnore]
    public int QuantityAsInt => int.TryParse(ORD_QTY, out var result) ? result : 0;

    [JsonIgnore]
    public decimal PriceAsDecimal => decimal.TryParse(ORD_UNPR, out var result) ? result : 0m;
    
    [JsonIgnore]
    public StockTrading.Domain.Enums.Market Market => GetMarketFromExchangeCode(OVRS_EXCG_CD);

    private StockTrading.Domain.Enums.Market GetMarketFromExchangeCode(string exchangeCode)
    {
        return exchangeCode switch
        {
            "NASD" => Domain.Enums.Market.Nasdaq,
            "NYSE" => Domain.Enums.Market.Nyse,
            "TKSE" => Domain.Enums.Market.Tokyo,
            "LNSE" => Domain.Enums.Market.London,
            "HKEX" => Domain.Enums.Market.Hongkong,
            _ => Domain.Enums.Market.Nasdaq
        };
    }
}