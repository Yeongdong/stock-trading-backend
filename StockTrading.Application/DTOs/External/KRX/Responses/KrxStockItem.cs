using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KRX.Responses;

public class KrxStockItem
{
    [JsonPropertyName("ISU_SRT_CD")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("ISU_CD")]
    public string IsinCode { get; init; } = string.Empty;

    [JsonPropertyName("ISU_ABBRV")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("ISU_NM")]
    public string FullName { get; init; } = string.Empty;

    [JsonPropertyName("ISU_ENG_NM")]
    public string? EnglishName { get; init; }

    [JsonPropertyName("LIST_DD")]
    public string? ListedDate { get; init; }

    [JsonPropertyName("SECUGRP_NM")]
    public string? SecurityGroup { get; init; }

    [JsonPropertyName("SECT_TP_NM")]
    public string? Sector { get; init; }

    [JsonPropertyName("LIST_SHRS")]
    public string? StockTypedShares { get; init; }

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Code) &&
               Code.Length == 6 &&
               Code.All(char.IsDigit);
    }
}