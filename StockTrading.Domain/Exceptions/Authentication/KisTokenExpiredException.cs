namespace StockTrading.Domain.Exceptions.Authentication;

public class KisTokenExpiredException : Exception
{
    public int UserId { get; }

    public KisTokenExpiredException(int userId) : base("KIS 액세스 토큰이 만료되었습니다.")
    {
        UserId = userId;
    }

    public KisTokenExpiredException(string? message, int userId) : base(message)
    {
        UserId = userId;
    }

    public KisTokenExpiredException(int userId, string message, Exception innerException) : base(message,
        innerException)
    {
        UserId = userId;
    }
}