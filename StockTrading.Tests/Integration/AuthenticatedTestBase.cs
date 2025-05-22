using Microsoft.Extensions.Logging;
using StockTrading.DataAccess.DTOs;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Tests.Integration;

/// <summary>
/// 인증이 필요한 통합테스트의 기본 베이스 클래스
/// IntegrationTestBase를 확장하여 인증 관련 기능을 추가로 제공
/// </summary>
public abstract class AuthenticatedTestBase : IntegrationTestBase
{
    /// <summary>
    /// 현재 테스트에서 사용 중인 인증된 사용자
    /// </summary>
    protected UserDto CurrentUser { get; private set; }

    /// <summary>
    /// 현재 테스트에서 사용 중인 JWT 토큰
    /// </summary>
    protected string CurrentToken { get; private set; }

    /// <summary>
    /// 인증된 HTTP 클라이언트 (기본 사용자)
    /// </summary>
    protected HttpClient AuthenticatedClient { get; private set; }

    protected AuthenticatedTestBase(IntegrationTestWebApplicationFactory factory)
        : base(factory)
    {
    }

    /// <summary>
    /// 테스트 환경 초기화 완료 후 인증 설정 수행
    /// </summary>
    protected override async Task OnTestEnvironmentInitializedAsync()
    {
        await base.OnTestEnvironmentInitializedAsync();
        await SetupDefaultAuthenticationAsync();
    }

    #region 기본 인증 설정

    /// <summary>
    /// 기본 인증 설정 (테스트 사용자로 자동 로그인)
    /// </summary>
    protected virtual async Task SetupDefaultAuthenticationAsync()
    {
        try
        {
            Logger.LogDebug("기본 인증 설정 시작");

            // 데이터베이스에서 테스트 사용자 조회
            var testUser = await DatabaseManager.GetTestUserAsync();
            await SetupAuthenticationForUserAsync(testUser);

            Logger.LogInformation("기본 인증 설정 완료: {Email}", CurrentUser.Email);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "기본 인증 설정 중 오류 발생");
            throw;
        }
    }

    /// <summary>
    /// 특정 사용자로 인증 설정
    /// </summary>
    protected virtual async Task SetupAuthenticationForUserAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        try
        {
            Logger.LogDebug("사용자 {Email}로 인증 설정 시작", user.Email);

            // User 엔티티를 UserDto로 변환
            CurrentUser = MapUserToDto(user);

            // JWT 토큰 생성
            CurrentToken = AuthenticationHelper.GenerateTokenForUser(CurrentUser);

            // 인증된 HTTP 클라이언트 생성
            AuthenticatedClient = AuthenticationHelper.CreateAuthenticatedClientWithToken(CurrentToken);

            Logger.LogDebug("사용자 {Email}로 인증 설정 완료", user.Email);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "사용자 {Email}로 인증 설정 중 오류 발생", user.Email);
            throw;
        }
    }

    #endregion

    #region 동적 인증 변경

    /// <summary>
    /// 다른 사용자로 인증 변경
    /// </summary>
    protected virtual async Task SwitchToUserAsync(UserDto user)
    {
        ArgumentNullException.ThrowIfNull(user);

        try
        {
            Logger.LogDebug("사용자 변경: {OldEmail} -> {NewEmail}", CurrentUser?.Email, user.Email);

            CurrentUser = user;
            CurrentToken = AuthenticationHelper.GenerateTokenForUser(user);

            // 기존 클라이언트 정리
            AuthenticatedClient?.Dispose();

            // 새 인증된 클라이언트 생성
            AuthenticatedClient = AuthenticationHelper.CreateAuthenticatedClientWithToken(CurrentToken);

            Logger.LogInformation("사용자 변경 완료: {Email}", user.Email);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "사용자 변경 중 오류 발생: {Email}", user.Email);
            throw;
        }
    }
    
    protected virtual async Task SwitchToExistingUserAsync(UserDto user)
    {
        ArgumentNullException.ThrowIfNull(user);

        try
        {
            Logger.LogDebug("기존 사용자로 변경: {Email}", user.Email);

            var dbUser = await DatabaseManager.EnsureUserExistsAsync(user);
        
            await SwitchToUserAsync(MapUserToDto(dbUser));

            Logger.LogInformation("기존 사용자로 변경 완료: {Email}", user.Email);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "기존 사용자로 변경 중 오류 발생: {Email}", user.Email);
            throw;
        }
    }

    /// <summary>
    /// 특정 역할(Role)로 인증 변경
    /// </summary>
    protected virtual async Task SwitchToRoleAsync(string role, string email = null, string name = null)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("역할이 제공되지 않았습니다.", nameof(role));

        try
        {
            Logger.LogDebug("역할 변경: {Role}", role);

            // 임시 사용자 생성
            var tempUser = new UserDto
            {
                Id = CurrentUser?.Id ?? 1,
                Email = email ?? $"test_{role.ToLower()}@example.com",
                Name = name ?? $"Test {role} User",
                KisAppKey = CurrentUser?.KisAppKey,
                KisAppSecret = CurrentUser?.KisAppSecret,
                AccountNumber = CurrentUser?.AccountNumber,
                WebSocketToken = CurrentUser?.WebSocketToken,
                KisToken = CurrentUser?.KisToken
            };

            CurrentToken = AuthenticationHelper.GenerateTokenWithRole(role, tempUser.Email, tempUser.Name);

            // 기존 클라이언트 정리
            AuthenticatedClient?.Dispose();

            // 새 인증된 클라이언트 생성
            AuthenticatedClient = AuthenticationHelper.CreateAuthenticatedClientWithToken(CurrentToken);

            CurrentUser = tempUser;

            Logger.LogInformation("역할 변경 완료: {Role}", role);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "역할 변경 중 오류 발생: {Role}", role);
            throw;
        }
    }

    /// <summary>
    /// 관리자 권한으로 인증 변경
    /// </summary>
    protected virtual async Task SwitchToAdminAsync()
    {
        await SwitchToRoleAsync("Admin", "admin@example.com", "Admin User");
    }

    /// <summary>
    /// 인증 해제 (익명 사용자로 변경)
    /// </summary>
    protected virtual async Task SwitchToAnonymousAsync()
    {
        try
        {
            Logger.LogDebug("인증 해제 시작");

            CurrentUser = null;
            CurrentToken = null;

            // 기존 클라이언트 정리
            AuthenticatedClient?.Dispose();

            // 인증되지 않은 클라이언트 생성
            AuthenticatedClient = CreateClient();

            Logger.LogInformation("인증 해제 완료");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "인증 해제 중 오류 발생");
            throw;
        }
    }

    #endregion

    #region 토큰 관리

    /// <summary>
    /// 현재 토큰 갱신
    /// </summary>
    protected virtual async Task RefreshCurrentTokenAsync()
    {
        if (CurrentUser == null)
            throw new InvalidOperationException("현재 인증된 사용자가 없습니다.");

        try
        {
            Logger.LogDebug("토큰 갱신 시작: {Email}", CurrentUser.Email);

            CurrentToken = AuthenticationHelper.GenerateTokenForUser(CurrentUser);

            // 기존 클라이언트 정리
            AuthenticatedClient?.Dispose();

            // 새 인증된 클라이언트 생성
            AuthenticatedClient = AuthenticationHelper.CreateAuthenticatedClientWithToken(CurrentToken);

            Logger.LogDebug("토큰 갱신 완료: {Email}", CurrentUser.Email);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "토큰 갱신 중 오류 발생: {Email}", CurrentUser.Email);
            throw;
        }
    }

    /// <summary>
    /// 만료된 토큰으로 변경 (인증 실패 테스트용)
    /// </summary>
    protected virtual async Task SwitchToExpiredTokenAsync()
    {
        try
        {
            Logger.LogDebug("만료된 토큰으로 변경 시작");

            CurrentToken = AuthenticationHelper.GenerateExpiredToken();

            // 기존 클라이언트 정리
            AuthenticatedClient?.Dispose();

            // 만료된 토큰으로 클라이언트 생성
            AuthenticatedClient = AuthenticationHelper.CreateAuthenticatedClientWithToken(CurrentToken);

            Logger.LogDebug("만료된 토큰으로 변경 완료");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "만료된 토큰으로 변경 중 오류 발생");
            throw;
        }
    }

    /// <summary>
    /// 잘못된 서명 토큰으로 변경 (보안 테스트용)
    /// </summary>
    protected virtual async Task SwitchToInvalidSignatureTokenAsync()
    {
        try
        {
            Logger.LogDebug("잘못된 서명 토큰으로 변경 시작");

            CurrentToken = AuthenticationHelper.GenerateInvalidSignatureToken();

            // 기존 클라이언트 정리
            AuthenticatedClient?.Dispose();

            // 잘못된 토큰으로 클라이언트 생성
            AuthenticatedClient = AuthenticationHelper.CreateAuthenticatedClientWithToken(CurrentToken);

            Logger.LogDebug("잘못된 서명 토큰으로 변경 완료");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "잘못된 서명 토큰으로 변경 중 오류 발생");
            throw;
        }
    }

    #endregion

    #region 편의 메서드들

    /// <summary>
    /// User 엔티티를 UserDto로 변환
    /// </summary>
    protected virtual UserDto MapUserToDto(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            KisAppKey = user.KisAppKey,
            KisAppSecret = user.KisAppSecret,
            AccountNumber = user.AccountNumber,
            WebSocketToken = user.WebSocketToken,
            KisToken = user.KisToken != null
                ? new KisTokenDto
                {
                    Id = user.KisToken.Id,
                    AccessToken = user.KisToken.AccessToken,
                    ExpiresIn = user.KisToken.ExpiresIn,
                    TokenType = user.KisToken.TokenType
                }
                : null
        };
    }

    /// <summary>
    /// 현재 사용자의 인증 상태 확인
    /// </summary>
    protected virtual bool IsAuthenticated()
    {
        return CurrentUser != null && !string.IsNullOrEmpty(CurrentToken);
    }

    /// <summary>
    /// 현재 사용자가 특정 역할을 가지고 있는지 확인
    /// </summary>
    protected virtual bool HasRole(string role)
    {
        if (!IsAuthenticated() || string.IsNullOrEmpty(CurrentToken))
            return false;

        try
        {
            var principal = AuthenticationHelper.ValidateToken(CurrentToken);
            return principal.IsInRole(role);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 현재 사용자가 관리자인지 확인
    /// </summary>
    protected virtual bool IsAdmin()
    {
        return HasRole("Admin");
    }

    #endregion

    #region 인증된 HTTP 요청 헬퍼

    /// <summary>
    /// 인증된 GET 요청
    /// </summary>
    protected virtual async Task<HttpResponseMessage> AuthenticatedGetAsync(string requestUri)
    {
        EnsureAuthenticated();
        return await HttpClientHelper.GetAsync(requestUri, AuthenticatedClient);
    }

    /// <summary>
    /// 인증된 POST 요청 (JSON)
    /// </summary>
    protected virtual async Task<HttpResponseMessage> AuthenticatedPostJsonAsync<T>(string requestUri, T content)
    {
        EnsureAuthenticated();
        return await HttpClientHelper.PostJsonAsync(requestUri, content, AuthenticatedClient);
    }

    /// <summary>
    /// 인증된 PUT 요청 (JSON)
    /// </summary>
    protected virtual async Task<HttpResponseMessage> AuthenticatedPutJsonAsync<T>(string requestUri, T content)
    {
        EnsureAuthenticated();
        return await HttpClientHelper.PutJsonAsync(requestUri, content, AuthenticatedClient);
    }

    /// <summary>
    /// 인증된 DELETE 요청
    /// </summary>
    protected virtual async Task<HttpResponseMessage> AuthenticatedDeleteAsync(string requestUri)
    {
        EnsureAuthenticated();
        return await HttpClientHelper.DeleteAsync(requestUri, AuthenticatedClient);
    }

    /// <summary>
    /// 인증 상태 확인 및 예외 처리
    /// </summary>
    private void EnsureAuthenticated()
    {
        if (!IsAuthenticated())
            throw new InvalidOperationException("인증된 사용자가 필요합니다. SetupAuthenticationAsync()를 먼저 호출하세요.");
    }

    #endregion

    #region 테스트 생명주기 오버라이드

    /// <summary>
    /// 각 테스트 메서드 실행 전 인증 상태 확인
    /// </summary>
    protected override async Task SetupAsync()
    {
        await base.SetupAsync();

        // 인증 상태가 유효한지 확인
        if (IsAuthenticated())
        {
            try
            {
                AuthenticationHelper.ValidateToken(CurrentToken);
            }
            catch
            {
                Logger.LogWarning("토큰이 유효하지 않음, 토큰 갱신 시도");
                await RefreshCurrentTokenAsync();
            }
        }
    }

    /// <summary>
    /// 각 테스트 메서드 실행 후 인증 정리
    /// </summary>
    protected override async Task OnTestTeardownAsync()
    {
        // 필요시 인증 상태 초기화 (기본적으로는 유지)
        await base.OnTestTeardownAsync();
    }

    #endregion

    #region 디버깅 헬퍼

    /// <summary>
    /// 현재 인증 상태 출력 (디버깅용)
    /// </summary>
    protected virtual void LogAuthenticationState()
    {
        Logger.LogDebug("=== 현재 인증 상태 ===");
        Logger.LogDebug("인증 여부: {IsAuthenticated}", IsAuthenticated());
        Logger.LogDebug("현재 사용자: {Email}", CurrentUser?.Email ?? "없음");
        Logger.LogDebug("토큰 존재: {HasToken}", !string.IsNullOrEmpty(CurrentToken));

        if (IsAuthenticated())
        {
            Logger.LogDebug("사용자 ID: {UserId}", CurrentUser.Id);
            Logger.LogDebug("사용자 이름: {Name}", CurrentUser.Name);
            Logger.LogDebug("KIS 토큰 존재: {HasKisToken}", CurrentUser.KisToken != null);
        }

        Logger.LogDebug("======================");
    }

    #endregion

    #region IDisposable 오버라이드

    /// <summary>
    /// 인증 관련 리소스 정리
    /// </summary>
    protected override async Task CleanupResourcesAsync()
    {
        try
        {
            AuthenticatedClient?.Dispose();

            Logger.LogDebug("인증 관련 리소스 정리 완료");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "인증 관련 리소스 정리 중 오류 발생");
        }
        finally
        {
            await base.CleanupResourcesAsync();
        }
    }

    #endregion
}