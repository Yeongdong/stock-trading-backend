namespace StockTrading.Application.Services;

public interface ICookieService
{
    void SetAuthCookie(string token);
    void DeleteAuthCookie();
    string? GetAuthToken();
}