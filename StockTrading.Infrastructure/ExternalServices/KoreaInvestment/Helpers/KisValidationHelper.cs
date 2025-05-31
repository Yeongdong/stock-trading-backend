using StockTrading.Application.DTOs.Trading.Orders;
using StockTrading.Application.DTOs.Users;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Helpers;

public static class KisValidationHelper
{
    public static void ValidateUserForKisApi(UserInfo user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrWhiteSpace(user.KisAppKey))
            throw new InvalidOperationException("KIS 앱 키가 설정되지 않았습니다.");

        if (string.IsNullOrWhiteSpace(user.KisAppSecret))
            throw new InvalidOperationException("KIS 앱 시크릿이 설정되지 않았습니다.");

        if (string.IsNullOrWhiteSpace(user.AccountNumber))
            throw new InvalidOperationException("계좌번호가 설정되지 않았습니다.");

        if (user.KisToken?.AccessToken == null)
            throw new InvalidOperationException("KIS 액세스 토큰이 없습니다. 토큰을 먼저 발급받아주세요.");

        if (user.KisToken.ExpiresIn <= DateTime.UtcNow)
            throw new InvalidOperationException("KIS 액세스 토큰이 만료되었습니다. 토큰을 재발급받아주세요.");
    }

    public static void ValidateTokenRequest(int userId, string appKey, string appSecret, string accountNumber)
    {
        if (userId <= 0)
            throw new ArgumentException("유효하지 않은 사용자 ID입니다.", nameof(userId));

        if (string.IsNullOrWhiteSpace(appKey))
            throw new ArgumentException("앱 키는 필수입니다.", nameof(appKey));

        if (string.IsNullOrWhiteSpace(appSecret))
            throw new ArgumentException("앱 시크릿은 필수입니다.", nameof(appSecret));

        if (string.IsNullOrWhiteSpace(accountNumber))
            throw new ArgumentException("계좌번호는 필수입니다.", nameof(accountNumber));
    }

    public static void ValidateRequest(OrderExecutionInquiryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.StartDate))
            throw new ArgumentException("조회시작일자는 필수입니다.", nameof(request.StartDate));

        if (string.IsNullOrWhiteSpace(request.EndDate))
            throw new ArgumentException("조회종료일자는 필수입니다.", nameof(request.EndDate));

        // 날짜 형식 검증
        if (!DateTime.TryParseExact(request.StartDate, "yyyyMMdd", null,
                System.Globalization.DateTimeStyles.None, out var startDate))
            throw new ArgumentException("조회시작일자 형식이 올바르지 않습니다.", nameof(request.StartDate));

        if (!DateTime.TryParseExact(request.EndDate, "yyyyMMdd", null,
                System.Globalization.DateTimeStyles.None, out var endDate))
            throw new ArgumentException("조회종료일자 형식이 올바르지 않습니다.", nameof(request.EndDate));

        // 조회 기간 검증 (최대 1개월)
        if ((endDate - startDate).TotalDays > 31)
            throw new ArgumentException("조회 기간은 최대 31일까지 가능합니다.");

        if (startDate > endDate)
            throw new ArgumentException("시작일자가 종료일자보다 늦을 수 없습니다.");
    }
}