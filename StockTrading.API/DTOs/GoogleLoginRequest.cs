using System.ComponentModel.DataAnnotations;

namespace stock_trading_backend.DTOs;

public class GoogleLoginRequest
{
    [Required]
    public string Credential { get; set; }
}