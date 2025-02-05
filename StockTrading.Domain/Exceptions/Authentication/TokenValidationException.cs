namespace StockTradingBackend.DataAccess.Exceptions.Authentication;

public class TokenValidationException : Exception
{
    public TokenValidationException(string message) : base(message)
    {
    }

    public TokenValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}