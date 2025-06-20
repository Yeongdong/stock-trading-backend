using System.ComponentModel;

namespace StockTrading.Domain.Enums;

public enum UserRole
{
    [Description("일반 사용자")] User = 0,
    [Description("관리자")] Admin = 1,
    [Description("마스터")] Master = 2
}