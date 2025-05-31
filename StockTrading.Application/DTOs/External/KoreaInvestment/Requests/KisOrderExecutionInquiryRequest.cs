namespace StockTrading.Application.DTOs.External.KoreaInvestment.Requests;

/// <summary>
/// KIS API 주문체결조회 요청용 DTO
/// </summary>
public class KisOrderExecutionInquiryRequest
{
    public string CANO { get; set; } = string.Empty; // 종합계좌번호
    public string ACNT_PRDT_CD { get; set; } = "01"; // 계좌상품코드
    public string INQR_STRT_DT { get; set; } = string.Empty; // 조회시작일자
    public string INQR_END_DT { get; set; } = string.Empty; // 조회종료일자
    public string SLL_BUY_DVSN_CD { get; set; } = "00"; // 매도매수구분코드
    public string INQR_DVSN { get; set; } = "00"; // 조회구분
    public string PDNO { get; set; } = string.Empty; // 상품번호(종목코드)
    public string CCLD_DVSN { get; set; } = "01"; // 체결구분
    public string ORD_GNO_BRNO { get; set; } = string.Empty; // 주문채번지점번호
    public string ODNO { get; set; } = string.Empty; // 주문번호
    public string INQR_DVSN_3 { get; set; } = "00"; // 조회구분3
    public string INQR_DVSN_1 { get; set; } = string.Empty; // 조회구분1
    public string CTX_AREA_FK100 { get; set; } = string.Empty; // 연속조회검색조건100
    public string CTX_AREA_NK100 { get; set; } = string.Empty; // 연속조회키100
}