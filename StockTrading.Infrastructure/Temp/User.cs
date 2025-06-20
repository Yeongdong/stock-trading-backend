using System;
using System.Collections.Generic;

namespace StockTrading.Infrastructure.Temp;

public partial class User
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string GoogleId { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string Role { get; set; } = null!;

    public string? PasswordHash { get; set; }
}
