using System.ComponentModel.DataAnnotations;

namespace StockTrading.Application.Features.Auth.DTOs;

public class LoginRequest
{
    [Required]
    public string Credential { get; init; } = string.Empty;
}