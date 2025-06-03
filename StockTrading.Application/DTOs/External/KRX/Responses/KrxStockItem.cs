using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KRX.Responses;

public class KrxStockItem
{
    /// <summary>종목코드</summary>
    [JsonPropertyName("ISU_SRT_CD")]
    public string Code { get; init; } = string.Empty;

    /// <summary>종목명</summary>
    [JsonPropertyName("ISU_ABBRV")]
    public string Name { get; init; } = string.Empty;

    /// <summary>영문종목명</summary>
    [JsonPropertyName("ISU_ENG_NM")]
    public string? EnglishName { get; init; }

    /// <summary>시장구분</summary>
    [JsonPropertyName("MKT_NM")]
    public string Market { get; init; } = string.Empty;

    /// <summary>업종명</summary>
    [JsonPropertyName("SECT_TP_NM")]
    public string Sector { get; init; } = string.Empty;
}