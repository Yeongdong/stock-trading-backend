using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

/// <summary>
/// KIS API 공통 응답 기본 클래스
/// </summary>
public abstract class KisBaseResponse<T>
{
    /// <summary>
    /// 응답코드 (0: 성공)
    /// </summary>
    [JsonPropertyName("rt_cd")]
    public string ReturnCode { get; init; } = string.Empty;

    /// <summary>
    /// 메시지코드
    /// </summary>
    [JsonPropertyName("msg_cd")]
    public string MessageCode { get; init; } = string.Empty;

    /// <summary>
    /// 응답메시지
    /// </summary>
    [JsonPropertyName("msg1")]
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 응답 데이터
    /// </summary>
    [JsonPropertyName("output")]
    public T? Output { get; init; }

    /// <summary>
    /// 응답이 성공인지 확인
    /// </summary>
    [JsonIgnore]
    public bool IsSuccess => ReturnCode == "0";

    /// <summary>
    /// 응답 데이터가 있는지 확인
    /// </summary>
    [JsonIgnore]
    public bool HasData => Output != null;
}