using StockTrading.Application.DTOs.Trading.Orders;
using StockTrading.Application.DTOs.Users;
using StockTrading.Domain.Exceptions.Authentication;
using StockTrading.Domain.Settings;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Helpers;

public static class KisValidationHelper
{
    public static void ValidateUserForKisApi(UserInfo user)
    {
        ArgumentNullException.ThrowIfNull(user);

        ValidateKisCredentials(user.KisAppKey, user.KisAppSecret, user.AccountNumber);
        ValidateKisToken(user.KisToken, user.Id);
    }

    public static bool IsTokenValid(KisTokenInfo? token)
    {
        return token?.AccessToken != null && token.ExpiresIn > DateTime.UtcNow;
    }

    public static void ValidateTokenRequest(int userId, string appKey, string appSecret, string accountNumber)
    {
        ValidateUserId(userId);
        ValidateKisCredentials(appKey, appSecret, accountNumber);
    }

    public static void ValidateRequest(OrderExecutionInquiryRequest request)
    {
        ValidateRequiredField(request.StartDate, nameof(request.StartDate), "조회시작일자는 필수입니다.");
        ValidateRequiredField(request.EndDate, nameof(request.EndDate), "조회종료일자는 필수입니다.");

        ValidateDateFormat(request.StartDate, nameof(request.StartDate));
        ValidateDateFormat(request.EndDate, nameof(request.EndDate));
        ValidateDateRange(request.StartDate, request.EndDate);
    }

    private static void ValidateKisCredentials(string? appKey, string? appSecret, string? accountNumber)
    {
        ValidateRequiredField(appKey, nameof(appKey), "KIS 앱 키가 설정되지 않았습니다.");
        ValidateRequiredField(appSecret, nameof(appSecret), "KIS 앱 시크릿이 설정되지 않았습니다.");
        ValidateRequiredField(accountNumber, nameof(accountNumber), "계좌번호가 설정되지 않았습니다.");
    }

    private static void ValidateKisToken(KisTokenInfo? token, int userId)
    {
        if (token?.AccessToken == null)
            throw new InvalidOperationException("KIS 액세스 토큰이 없습니다. 토큰을 먼저 발급받아주세요.");

        if (token.ExpiresIn <= DateTime.UtcNow)
            throw new KisTokenExpiredException(userId);
    }

    private static void ValidateUserId(int userId)
    {
        if (userId <= 0)
            throw new ArgumentException("유효하지 않은 사용자 ID입니다.", nameof(userId));
    }

    private static void ValidateRequiredField(string? value, string fieldName, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(errorMessage, fieldName);
    }

    private static void ValidateDateFormat(string dateString, string fieldName)
    {
        if (!DateTime.TryParseExact(dateString, ParsingSettings.ValidationConstants.Date.Format, null,
                System.Globalization.DateTimeStyles.None, out _))
            throw new ArgumentException($"{fieldName} 형식이 올바르지 않습니다.", fieldName);
    }

    private static void ValidateDateRange(string startDate, string endDate)
    {
        var start = DateTime.ParseExact(startDate, ParsingSettings.ValidationConstants.Date.Format, null);
        var end = DateTime.ParseExact(endDate, ParsingSettings.ValidationConstants.Date.Format, null);

        if ((end - start).TotalDays > ParsingSettings.ValidationConstants.Date.MaxInquiryDays)
            throw new ArgumentException("조회 기간은 최대 31일까지 가능합니다.");

        if (start > end)
            throw new ArgumentException("시작일자가 종료일자보다 늦을 수 없습니다.");
    }
}