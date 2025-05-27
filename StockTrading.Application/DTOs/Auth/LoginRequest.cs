using System.ComponentModel.DataAnnotations;

namespace StockTrading.Application.DTOs.Auth;

public class LoginRequest
{
    [Required]
    public string Credential { get; init; }
}