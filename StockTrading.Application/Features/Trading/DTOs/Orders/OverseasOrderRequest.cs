using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StockTrading.Application.Features.Trading.DTOs.Orders;

/// <summary>
/// 해외 주식 주문 요청
/// </summary>
public class OverseasOrderRequest
{
    // 계좌 정보
    [JsonIgnore]
    public string? CANO { get; set; } = null!;
    
    [JsonPropertyName("acntPrdtCd")]
    public string ACNT_PRDT_CD { get; set; } = "01";

    // 해외거래소코드
    [JsonPropertyName("ovrsExcgCd")]
    [Required(ErrorMessage = "해외거래소코드는 필수입니다.")]
    public string OVRS_EXCG_CD { get; init; } = null!;

    // 종목코드
    [JsonPropertyName("pdno")]
    [Required(ErrorMessage = "종목코드는 필수입니다.")]
    public string PDNO { get; init; } = null!;

    // 주문수량
    [JsonPropertyName("ordQty")]
    [Required(ErrorMessage = "주문수량은 필수입니다.")]
    public string ORD_QTY { get; init; } = null!;

    // 해외주문단가
    [JsonPropertyName("ovrsOrdUnpr")]
    [Required(ErrorMessage = "해외주문단가는 필수입니다.")]
    public string OVRS_ORD_UNPR { get; set; } = null!;

    // 주문서버구분코드 (고정값)
    [JsonPropertyName("ordSvrDvsnCd")]
    public string ORD_SVR_DVSN_CD { get; init; } = "0";

    // 주문구분
    [JsonPropertyName("ordDvsn")]
    [Required(ErrorMessage = "주문구분은 필수입니다.")]
    public string ORD_DVSN { get; init; } = null!;

    // 주문조건
    [JsonPropertyName("ordCndt")]
    public string ORD_CNDT { get; init; } = "DAY";

    // 거래ID
    [JsonPropertyName("trId")]
    public string tr_id { get; init; } = null!;

    [JsonIgnore]
    public int QuantityAsInt => int.TryParse(ORD_QTY, out var result) ? result : 0;

    [JsonIgnore]
    public decimal PriceAsDecimal => decimal.TryParse(OVRS_ORD_UNPR, out var result) ? result : 0m;

    [JsonIgnore]
    public Domain.Enums.Market Market => GetMarketFromExchangeCode(OVRS_EXCG_CD);

    private Domain.Enums.Market GetMarketFromExchangeCode(string exchangeCode)
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