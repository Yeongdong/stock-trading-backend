using StockTradingBackend.DataAccess.Entities;

namespace stock_trading_backend.DTOs;

public class UserDTO
{
    public string Email { get; set; }
    public string Name { get; set; }
    public string KisAppKey { get; set; }
    public string KisAppSecret { get; set; }

    public UserDTO(User user)
    {
        Email = user.Email;
        Name = user.Name;
        KisAppKey = user.KisAppKey;
        KisAppSecret = user.KisAppSecret;
    }
}