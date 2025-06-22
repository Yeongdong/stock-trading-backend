namespace StockTrading.Application.Common.Interfaces;

public interface ICookieService
{
    void SetAuthCookie(string token);
    void SetRefreshTokenCookie(string refreshToken);
    void DeleteAuthCookie();
    void DeleteRefreshTokenCookie();
    string? GetAuthToken();
    string? GetRefreshToken();
}