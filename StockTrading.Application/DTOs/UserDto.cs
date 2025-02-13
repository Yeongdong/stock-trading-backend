namespace StockTrading.DataAccess.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? KisAppKey { get; set; }
    public string? KisAppSecret { get; set; }
    public string? AccountNumber { get; set; }
    public KisTokenDto? KisToken { get; set; }
}