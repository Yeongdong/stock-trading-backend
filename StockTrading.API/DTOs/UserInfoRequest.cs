using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace stock_trading_backend.DTOs;

public class UserInfoRequest 
{
    [JsonPropertyName("appKey")]
    public string AppKey { get; set; }
    
    [JsonPropertyName("appSecret")]
    public string AppSecret { get; set; }
    
    [JsonPropertyName("accountNumber")]
    public string AccountNumber { get; set; }
}