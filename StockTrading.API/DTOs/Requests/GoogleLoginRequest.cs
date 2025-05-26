using System.ComponentModel.DataAnnotations;

namespace StockTrading.API.DTOs.Requests;

public class GoogleLoginRequest
{
    [Required]
    public string Credential { get; set; }
}