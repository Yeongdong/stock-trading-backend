using StockTradingBackend.DataAccess.Enums;

namespace StockTrading.DataAccess.DTOs.OrderDTOs;

public class StockOrderRequest
{
    public string ACNT_PRDT_CD { get; set; } = "01";    // 계좌상품코드
    public string PDNO { get; set; }                    // 종목코드
    public OrderType ORD_DVSN { get; set; }                // 주문구분
    public string ORD_QTY { get; set; }                 // 주문수량
    public string ORD_UNPR { get; set; }                // 주문단가
}