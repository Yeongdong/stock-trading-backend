using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StockTrading.Application.Features.Users.DTOs;

public class UserSettingsRequest 
{
    [Required(ErrorMessage = "앱 키는 필수입니다.")]
    [JsonPropertyName("appKey")]
    public string AppKey { get; init; } = string.Empty;
    
    [Required(ErrorMessage = "앱 시크릿은 필수입니다.")]
    [JsonPropertyName("appSecret")]
    public string AppSecret { get; init; } = string.Empty;
    
    [Required(ErrorMessage = "계좌번호는 필수입니다.")]
    [JsonPropertyName("accountNumber")]
    public string AccountNumber { get; init; } = string.Empty;
}