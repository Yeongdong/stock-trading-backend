using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace stock_trading_backend.DTOs;

public class UserInfoRequest
{
    [Required(ErrorMessage = "AppKey는 필수 항목입니다.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "AppKey는 1-100자 사이여야 합니다.")]
    [JsonPropertyName("appKey")]
    public string AppKey { get; set; }

    [Required(ErrorMessage = "AppSecret은 필수 항목입니다.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "AppSecret은 1-100자 사이여야 합니다.")]
    [JsonPropertyName("appSecret")]
    public string AppSecret { get; set; }

    [Required(ErrorMessage = "계좌번호는 필수 항목입니다.")]
    [StringLength(20, MinimumLength = 10, ErrorMessage = "계좌번호는 10-20자 사이여야 합니다.")]
    [JsonPropertyName("accountNumber")]
    public string AccountNumber { get; set; }
}