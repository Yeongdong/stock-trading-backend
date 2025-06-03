using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KRX.Responses;

/// <summary>
/// KRX API 주식종목 정보 응답 모델
/// </summary>
public class KrxStockItem
{
    /// <summary>ISIN 코드</summary>
    [JsonPropertyName("ISU_CD")]
    public string IsinCode { get; init; } = string.Empty;

    /// <summary>종목코드 (6자리)</summary>
    [JsonPropertyName("ISU_SRT_CD")]
    public string Code { get; init; } = string.Empty;

    /// <summary>종목명 (정식)</summary>
    [JsonPropertyName("ISU_NM")]
    public string FullName { get; init; } = string.Empty;

    /// <summary>종목명 (약어)</summary>
    [JsonPropertyName("ISU_ABBRV")]
    public string Name { get; init; } = string.Empty;

    /// <summary>영문 종목명</summary>
    [JsonPropertyName("ISU_ENG_NM")]
    public string? EnglishName { get; init; }

    /// <summary>상장일</summary>
    [JsonPropertyName("LIST_DD")]
    public string? ListedDate { get; init; }

    /// <summary>증권 그룹</summary>
    [JsonPropertyName("SECUGRP_NM")]
    public string? SecurityGroup { get; init; }

    /// <summary>업종명</summary>
    [JsonPropertyName("SECT_TP_NM")]
    public string? Sector { get; init; }

    /// <summary>주식 종류</summary>
    [JsonPropertyName("KIND_STKCERT_TP_NM")]
    public string? StockType { get; init; }

    /// <summary>액면가</summary>
    [JsonPropertyName("PARVAL")]
    public string? ParValue { get; init; }

    /// <summary>상장 주식 수</summary>
    [JsonPropertyName("LIST_SHRS")]
    public string? ListedShares { get; init; }

    /// <summary>유효성 검사</summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Code) &&
               Code.Length == 6 &&
               Code.All(char.IsDigit) &&
               !string.IsNullOrWhiteSpace(Name);
    }
}