using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

/// <summary>
/// KIS API 공통 응답 기본 클래스 - 리스트 응답
/// </summary>
/// <typeparam name="T">응답 데이터 타입</typeparam>
public abstract class KisBaseListResponse<T>
{
    [JsonPropertyName("rt_cd")]
    public string ReturnCode { get; init; } = string.Empty;

    [JsonPropertyName("msg_cd")]
    public string MessageCode { get; init; } = string.Empty;

    [JsonPropertyName("msg1")]
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 리스트 응답 데이터 (output1)
    /// </summary>
    [JsonPropertyName("output1")]
    public List<T> Output { get; init; } = [];

    [JsonIgnore]
    public bool IsSuccess => ReturnCode == "0";

    [JsonIgnore]
    public bool HasData => Output.Count > 0;
}