namespace StockTrading.Application.Common.Interfaces;

public interface ICookieService
{
    void SetAuthCookie(string token);
    void DeleteAuthCookie();
    string? GetAuthToken();
}