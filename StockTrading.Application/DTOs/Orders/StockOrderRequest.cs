using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.Orders;

public class StockOrderRequest
{
    [JsonPropertyName("acntPrdtCd")]
    public string ACNT_PRDT_CD { get; set; } = "01";
    
    [JsonPropertyName("trId")]
    [Required(ErrorMessage = "거래구분은 필수입니다.")]
    public string tr_id { get; set; }

    [JsonPropertyName("pdno")]
    [Required(ErrorMessage = "종목코드는 필수입니다.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "종목코드는 6자리 숫자여야 합니다.")]
    public string PDNO { get; set; }
    
    [JsonPropertyName("ordDvsn")]
    [Required(ErrorMessage = "주문구분은 필수입니다.")]
    public string ORD_DVSN { get; set; }
    
    [JsonPropertyName("ordQty")]
    [Required(ErrorMessage = "주문수량은 필수입니다.")]
    [Range(1, int.MaxValue, ErrorMessage = "주문수량은 1 이상이어야 합니다.")]
    public int ORD_QTY { get; set; }
    
    [JsonPropertyName("ordUnpr")]
    [Required(ErrorMessage = "주문단가는 필수입니다.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "주문단가는 0보다 커야 합니다.")]
    public decimal ORD_UNPR { get; set; }
}