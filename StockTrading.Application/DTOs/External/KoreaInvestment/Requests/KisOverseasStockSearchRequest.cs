namespace StockTrading.Application.DTOs.External.KoreaInvestment.Requests;

/// <summary>
/// KIS API 해외주식 조건검색 요청 (내부용)
/// </summary>
public class KisOverseasStockSearchRequest
{
    public string AUTH { get; set; } = "";
    public string EXCD { get; set; } = string.Empty;
    public string? CO_YN_PRICECUR { get; set; }
    public string? CO_ST_PRICECUR { get; set; }
    public string? CO_EN_PRICECUR { get; set; }
    public string? CO_YN_RATE { get; set; }
    public string? CO_ST_RATE { get; set; }
    public string? CO_EN_RATE { get; set; }
    public string? CO_YN_VALX { get; set; }
    public string? CO_ST_VALX { get; set; }
    public string? CO_EN_VALX { get; set; }
    public string? CO_YN_SHAR { get; set; }
    public string? CO_ST_SHAR { get; set; }
    public string? CO_EN_SHAR { get; set; }
    public string? CO_YN_VOLUME { get; set; }
    public string? CO_ST_VOLUME { get; set; }
    public string? CO_EN_VOLUME { get; set; }
    public string? CO_YN_AMT { get; set; }
    public string? CO_ST_AMT { get; set; }
    public string? CO_EN_AMT { get; set; }
    public string? CO_YN_EPS { get; set; }
    public string? CO_ST_EPS { get; set; }
    public string? CO_EN_EPS { get; set; }
    public string? CO_YN_PER { get; set; }
    public string? CO_ST_PER { get; set; }
    public string? CO_EN_PER { get; set; }
    public string KEYB { get; set; } = "";
}