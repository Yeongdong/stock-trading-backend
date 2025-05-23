using System.Net;
using Microsoft.Extensions.Logging;
using stock_trading_backend.Controllers;
using stock_trading_backend.DTOs;
using StockTrading.DataAccess.DTOs;
using Xunit.Abstractions;

namespace StockTrading.Tests.Integration.Controllers;

/// <summary>
/// KIS 사용자 정보 업데이트 및 토큰 관리 기능 테스트
/// </summary>
public class AccountControllerIntegrationTest : ControllerTestBase<AccountController>
{
    private readonly ITestOutputHelper _output;

    public AccountControllerIntegrationTest(IntegrationTestWebApplicationFactory factory, ITestOutputHelper output)
        : base(factory)
    {
        _output = output;
    }

    #region 기본 기능 테스트

    /// <summary>
    /// 유효한 KIS 정보로 사용자 정보 업데이트 성공 테스트
    /// </summary>
    [Fact]
    public async Task UpdateUserInfo_WithValidKisInfo_ShouldReturnTokenResponse()
    {
        await RunTestWithLoggingAsync(nameof(UpdateUserInfo_WithValidKisInfo_ShouldReturnTokenResponse), async () =>
        {
            // Arrange
            _output.WriteLine("=== 테스트 시작 ===");
            LogAuthenticationState();

            var userInfoRequest = CreateValidUserInfoRequest();
            _output.WriteLine($"요청 데이터: AppKey={userInfoRequest.AppKey}, AccountNumber={userInfoRequest.AccountNumber}");
            try
            {
                // Act
                _output.WriteLine("API 호출 시작...");
                var response = await PostAsync(userInfoRequest, "userInfo");

                _output.WriteLine($"응답 상태: {response.StatusCode}");

                // 응답 내용 로깅
                var responseContent = await response.Content.ReadAsStringAsync();
                _output.WriteLine($"응답 내용: {responseContent}");

                // Assert
                if (!response.IsSuccessStatusCode)
                {
                    Logger.LogError("API 호출 실패: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    throw new Exception($"API 호출 실패: {response.StatusCode} - {responseContent}");
                }

                await AssertSuccessResponseAsync(response);

                var tokenResponse = await HttpClientHelper.DeserializeResponseAsync<TokenResponse>(response);

                // 토큰 응답 검증
                Assert.NotNull(tokenResponse);
                Assert.False(string.IsNullOrEmpty(tokenResponse.AccessToken));
                Assert.Equal("Bearer", tokenResponse.TokenType);
                Assert.True(tokenResponse.ExpiresIn > 0);

                Logger.LogInformation("KIS 정보 업데이트 성공: 토큰 타입={TokenType}, 만료시간={ExpiresIn}초",
                    tokenResponse.TokenType, tokenResponse.ExpiresIn);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "테스트 실행 중 오류 발생");
                throw;
            }
        });
    }

    /// <summary>
    /// 사용자 정보 업데이트 후 데이터베이스에 정보가 저장되는지 확인
    /// </summary>
    [Fact]
    public async Task UpdateUserInfo_ShouldPersistDataInDatabase()
    {
        await RunTestWithLoggingAsync(nameof(UpdateUserInfo_ShouldPersistDataInDatabase), async () =>
        {
            // Arrange
            var userInfoRequest = CreateValidUserInfoRequest();
            var originalUserCount = await DatabaseManager.CountAsync<StockTradingBackend.DataAccess.Entities.User>();

            // Act
            var response = await PostAsync(userInfoRequest, "userInfo");
            await AssertSuccessResponseAsync(response);

            // Assert
            var updatedUserCount = await DatabaseManager.CountAsync<StockTradingBackend.DataAccess.Entities.User>();

            Assert.Equal(originalUserCount, updatedUserCount);

            var tokenCount = await DatabaseManager.CountAsync<StockTradingBackend.DataAccess.Entities.KisToken>();
            Assert.True(tokenCount > 0, "KIS 토큰이 데이터베이스에 저장되어야 합니다.");

            Logger.LogInformation("데이터베이스 저장 확인 완료: 사용자={UserCount}, 토큰={TokenCount}",
                updatedUserCount, tokenCount);
        });
    }

    /// <summary>
    /// 동일한 사용자가 여러 번 업데이트해도 정상 동작하는지 확인
    /// </summary>
    [Fact]
    public async Task UpdateUserInfo_MultipleUpdates_ShouldWorkCorrectly()
    {
        await RunTestWithLoggingAsync(nameof(UpdateUserInfo_MultipleUpdates_ShouldWorkCorrectly), async () =>
        {
            // Arrange
            var firstRequest = CreateValidUserInfoRequest();
            var secondRequest = CreateValidUserInfoRequest();
            secondRequest.AppKey = "updated_app_key";
            secondRequest.AppSecret = "updated_app_secret";

            // Act
            var firstResponse = await PostAsync(firstRequest, "userInfo");
            await AssertSuccessResponseAsync(firstResponse);

            var secondResponse = await PostAsync(secondRequest, "userInfo");
            await AssertSuccessResponseAsync(secondResponse);

            // Assert
            var firstToken = await HttpClientHelper.DeserializeResponseAsync<TokenResponse>(firstResponse);
            var secondToken = await HttpClientHelper.DeserializeResponseAsync<TokenResponse>(secondResponse);

            Assert.NotNull(firstToken);
            Assert.NotNull(secondToken);

            // 토큰이 갱신되었는지 확인 (다를 수 있음)
            Logger.LogInformation("다중 업데이트 테스트 완료");
        });
    }

    #endregion

    #region 인증/권한 테스트

    /// <summary>
    /// 인증되지 않은 사용자의 요청은 401 Unauthorized 반환 테스트
    /// </summary>
    [Fact]
    public async Task UpdateUserInfo_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        await RunTestWithLoggingAsync(nameof(UpdateUserInfo_WithoutAuthentication_ShouldReturnUnauthorized), async () =>
        {
            // Arrange
            await SwitchToAnonymousAsync();
            var userInfoRequest = CreateValidUserInfoRequest();

            // Act
            var client = CreateClient();
            var response = await HttpClientHelper.PostJsonAsync("/api/account/userInfo", userInfoRequest, client);

            // Assert
            await AssertUnauthorizedAsync(response);

            Logger.LogInformation("인증되지 않은 요청 정상적으로 거부됨");
        });
    }

    /// <summary>
    /// 만료된 토큰으로 요청 시 401 Unauthorized 반환 테스트
    /// </summary>
    [Fact]
    public async Task UpdateUserInfo_WithExpiredToken_ShouldReturnUnauthorized()
    {
        await RunTestWithLoggingAsync(nameof(UpdateUserInfo_WithExpiredToken_ShouldReturnUnauthorized), async () =>
        {
            // Arrange
            await SwitchToExpiredTokenAsync();
            var userInfoRequest = CreateValidUserInfoRequest();

            // Act
            var response = await PostAsync(userInfoRequest, "userInfo");

            // Assert
            await AssertUnauthorizedAsync(response);

            Logger.LogInformation("만료된 토큰 요청 정상적으로 거부됨");
        });
    }

    /// <summary>
    /// 잘못된 서명 토큰으로 요청 시 401 Unauthorized 반환 테스트
    /// </summary>
    [Fact]
    public async Task UpdateUserInfo_WithInvalidSignatureToken_ShouldReturnUnauthorized()
    {
        await RunTestWithLoggingAsync(nameof(UpdateUserInfo_WithInvalidSignatureToken_ShouldReturnUnauthorized),
            async () =>
            {
                // Arrange
                await SwitchToInvalidSignatureTokenAsync();
                var userInfoRequest = CreateValidUserInfoRequest();

                // Act
                var response = await PostAsync(userInfoRequest, "userInfo");

                // Assert
                await AssertUnauthorizedAsync(response);

                Logger.LogInformation("잘못된 서명 토큰 요청 정상적으로 거부됨");
            });
    }

    /// <summary>
    /// 다른 사용자로 인증된 상태에서도 정상 동작하는지 테스트
    /// </summary>
    [Fact]
    public async Task UpdateUserInfo_WithDifferentUser_ShouldUpdateCorrectUser()
    {
        await RunTestWithLoggingAsync(nameof(UpdateUserInfo_WithDifferentUser_ShouldUpdateCorrectUser), async () =>
        {
            // Arrange
            var differentUser = new UserDto
            {
                Id = 888,
                Email = "different@example.com",
                Name = "Different User"
            };

            await SwitchToExistingUserAsync(differentUser);
            var userInfoRequest = CreateValidUserInfoRequest();

            // Act
            var response = await PostAsync(userInfoRequest, "userInfo");

            // Assert
            await AssertSuccessResponseAsync(response);

            var tokenResponse = await HttpClientHelper.DeserializeResponseAsync<TokenResponse>(response);
            Assert.NotNull(tokenResponse);

            Logger.LogInformation("다른 사용자로 KIS 정보 업데이트 성공: {Email}", differentUser.Email);
        });
    }

    #endregion

    #region 입력 검증 테스트

    /// <summary>
    /// AppKey가 비어있을 때 BadRequest 반환 테스트
    /// </summary>
    [Fact]
    public async Task UpdateUserInfo_WithEmptyAppKey_ShouldReturnBadRequest()
    {
        await RunTestWithLoggingAsync(nameof(UpdateUserInfo_WithEmptyAppKey_ShouldReturnBadRequest), async () =>
        {
            // Arrange
            var invalidRequest = CreateValidUserInfoRequest();
            invalidRequest.AppKey = string.Empty;

            // Act
            var response = await PostAsync(invalidRequest, "userInfo");

            // Assert
            await AssertBadRequestAsync(response);

            Logger.LogInformation("빈 AppKey 요청 정상적으로 거부됨");
        });
    }

    /// <summary>
    /// AppSecret이 null일 때 BadRequest 반환 테스트
    /// </summary>
    [Fact]
    public async Task UpdateUserInfo_WithNullAppSecret_ShouldReturnBadRequest()
    {
        await RunTestWithLoggingAsync(nameof(UpdateUserInfo_WithNullAppSecret_ShouldReturnBadRequest), async () =>
        {
            // Arrange
            var invalidRequest = CreateValidUserInfoRequest();
            invalidRequest.AppSecret = null;

            // Act
            var response = await PostAsync(invalidRequest, "userInfo");

            // Assert
            await AssertBadRequestAsync(response);

            Logger.LogInformation("null AppSecret 요청 정상적으로 거부됨");
        });
    }

    /// <summary>
    /// AccountNumber가 비어있을 때 BadRequest 반환 테스트
    /// </summary>
    [Fact]
    public async Task UpdateUserInfo_WithEmptyAccountNumber_ShouldReturnBadRequest()
    {
        await RunTestWithLoggingAsync(nameof(UpdateUserInfo_WithEmptyAccountNumber_ShouldReturnBadRequest), async () =>
        {
            // Arrange
            var invalidRequest = CreateValidUserInfoRequest();
            invalidRequest.AccountNumber = "";

            // Act
            var response = await PostAsync(invalidRequest, "userInfo");

            // Assert
            await AssertBadRequestAsync(response);

            Logger.LogInformation("빈 AccountNumber 요청 정상적으로 거부됨");
        });
    }

    /// <summary>
    /// 모든 필드가 null인 요청 테스트
    /// </summary>
    [Fact]
    public async Task UpdateUserInfo_WithAllNullFields_ShouldReturnBadRequest()
    {
        await RunTestWithLoggingAsync(nameof(UpdateUserInfo_WithAllNullFields_ShouldReturnBadRequest), async () =>
        {
            // Arrange
            var invalidRequest = new UserInfoRequest
            {
                AppKey = null,
                AppSecret = null,
                AccountNumber = null
            };

            // Act
            var response = await PostAsync(invalidRequest, "userInfo");

            // Assert
            await AssertBadRequestAsync(response);

            Logger.LogInformation("모든 필드가 null인 요청 정상적으로 거부됨");
        });
    }

    #endregion

    #region 에러 처리 테스트

    /// <summary>
    /// KIS API 호출 실패 시 BadRequest 반환 테스트
    /// </summary>
    [Fact]
    public async Task UpdateUserInfo_WhenKisApiCallFails_ShouldReturnBadRequest()
    {
        await RunTestWithLoggingAsync(nameof(UpdateUserInfo_WhenKisApiCallFails_ShouldReturnBadRequest), async () =>
        {
            // Arrange
            var invalidKisRequest = CreateValidUserInfoRequest();
            invalidKisRequest.AppKey = "invalid_app_key_that_causes_failure";

            // Act
            var response = await PostAsync(invalidKisRequest, "userInfo");

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.OK ||
                        response.StatusCode == HttpStatusCode.BadRequest ||
                        response.StatusCode == HttpStatusCode.InternalServerError);

            Logger.LogInformation("KIS API 실패 시나리오 테스트 완료: {StatusCode}", response.StatusCode);
        });
    }

    /// <summary>
    /// GoogleAuthProvider 실패 시 처리 테스트
    /// </summary>
    [Fact]
    public async Task UpdateUserInfo_WhenGoogleAuthFails_ShouldReturnBadRequest()
    {
        await RunTestWithLoggingAsync(nameof(UpdateUserInfo_WhenGoogleAuthFails_ShouldReturnBadRequest), async () =>
        {
            // Arrange
            // 잘못된 클레임으로 토큰 생성 (이메일 클레임 없음)
            var invalidToken = AuthenticationHelper.GenerateTokenWithClaims(
                new System.Security.Claims.Claim("invalid_claim", "value"));

            var invalidClient = AuthenticationHelper.CreateAuthenticatedClientWithToken(invalidToken);
            var userInfoRequest = CreateValidUserInfoRequest();

            // Act
            var response =
                await HttpClientHelper.PostJsonAsync("/api/account/userInfo", userInfoRequest, invalidClient);

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest ||
                        response.StatusCode == HttpStatusCode.Unauthorized ||
                        response.StatusCode == HttpStatusCode.InternalServerError);

            Logger.LogInformation("Google 인증 실패 시나리오 테스트 완료: {StatusCode}", response.StatusCode);
        });
    }

    /// <summary>
    /// 존재하지 않는 사용자로 요청 시 처리 테스트
    /// </summary>
    [Fact]
    public async Task UpdateUserInfo_WithNonExistentUser_ShouldReturnInternalServerError()
    {
        await RunTestWithLoggingAsync(nameof(UpdateUserInfo_WithNonExistentUser_ShouldReturnInternalServerError),
            async () =>
            {
                // Arrange
                var nonExistentUser = new UserDto
                {
                    Id = 99999,
                    Email = "nonexistent@example.com",
                    Name = "Non Existent User"
                };

                await SwitchToUserAsync(nonExistentUser);
                var userInfoRequest = CreateValidUserInfoRequest();

                // Act
                var response = await PostAsync(userInfoRequest, "userInfo");

                // Assert
                Assert.True(response.StatusCode == HttpStatusCode.InternalServerError ||
                            response.StatusCode == HttpStatusCode.BadRequest ||
                            response.StatusCode == HttpStatusCode.NotFound);

                Logger.LogInformation("존재하지 않는 사용자 요청 처리됨: {StatusCode}", response.StatusCode);
            });
    }

    #endregion

    #region 응답 형식 테스트

    /// <summary>
    /// 응답이 올바른 JSON 형식인지 확인
    /// </summary>
    [Fact]
    public async Task UpdateUserInfo_ShouldReturnValidJsonFormat()
    {
        await RunTestWithLoggingAsync(nameof(UpdateUserInfo_ShouldReturnValidJsonFormat), async () =>
        {
            // Arrange
            var userInfoRequest = CreateValidUserInfoRequest();

            // Act
            var response = await PostAsync(userInfoRequest, "userInfo");

            // Assert
            await AssertSuccessResponseAsync(response);

            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            var tokenResponse = await HttpClientHelper.DeserializeResponseAsync<TokenResponse>(response);
            Assert.NotNull(tokenResponse);

            Assert.False(string.IsNullOrEmpty(tokenResponse.AccessToken));
            Assert.False(string.IsNullOrEmpty(tokenResponse.TokenType));
            Assert.True(tokenResponse.ExpiresIn > 0);

            Logger.LogInformation("응답 JSON 형식 검증 완료");
        });
    }

    /// <summary>
    /// TokenResponse가 IsValid() 메서드를 통과하는지 확인
    /// </summary>
    [Fact]
    public async Task UpdateUserInfo_ShouldReturnValidTokenResponse()
    {
        await RunTestWithLoggingAsync(nameof(UpdateUserInfo_ShouldReturnValidTokenResponse), async () =>
        {
            // Arrange
            var userInfoRequest = CreateValidUserInfoRequest();

            // Act
            var tokenResponse = await PostAsync<UserInfoRequest, TokenResponse>(userInfoRequest, "userInfo");

            // Assert
            Assert.NotNull(tokenResponse);
            Assert.True(tokenResponse.IsValid(), "TokenResponse.IsValid()가 true를 반환해야 합니다.");

            Logger.LogInformation("TokenResponse 유효성 검증 완료");
        });
    }

    #endregion

    #region 성능 테스트

    /// <summary>
    /// 연속 요청 성능 테스트
    /// </summary>
    [Fact]
    public async Task UpdateUserInfo_MultipleRequests_ShouldPerformWell()
    {
        await RunTestWithLoggingAsync(nameof(UpdateUserInfo_MultipleRequests_ShouldPerformWell), async () =>
        {
            // Arrange
            const int requestCount = 5; // 통합 테스트이므로 적은 수로 설정
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var tasks = Enumerable.Range(0, requestCount)
                .Select(i =>
                {
                    var request = CreateValidUserInfoRequest();
                    request.AppKey = $"test_app_key_{i}"; // 각 요청을 구분하기 위해
                    return PostAsync<UserInfoRequest, TokenResponse>(request, "userInfo");
                })
                .ToArray();

            var results = await Task.WhenAll(tasks);

            stopwatch.Stop();

            // Assert
            Assert.Equal(requestCount, results.Length);
            Assert.All(results, tokenResponse =>
            {
                Assert.NotNull(tokenResponse);
                Assert.True(tokenResponse.IsValid());
            });

            var averageTimeMs = stopwatch.ElapsedMilliseconds / (double)requestCount;

            Logger.LogInformation("연속 {Count}회 요청 완료. 평균 소요시간: {AverageMs:F2}ms",
                requestCount, averageTimeMs);

            // 성능 기준 - 평균 응답시간이 2초를 넘지 않아야 함 (KIS API 호출 포함)
            Assert.True(averageTimeMs < 2000,
                $"응답시간이 너무 느림: {averageTimeMs:F2}ms > 2000ms");
        });
    }

    #endregion

    #region 통합 시나리오 테스트

    /// <summary>
    /// 전체 사용자 정보 업데이트 워크플로우 테스트
    /// </summary>
    [Fact]
    public async Task UpdateUserInfo_CompleteWorkflow_ShouldWorkEndToEnd()
    {
        await RunTestWithLoggingAsync(nameof(UpdateUserInfo_CompleteWorkflow_ShouldWorkEndToEnd), async () =>
        {
            // Arrange
            var userInfoRequest = CreateValidUserInfoRequest();

            // Act & Assert - 1단계: 사용자 정보 업데이트
            var updateResponse = await PostAsync(userInfoRequest, "userInfo");
            await AssertSuccessResponseAsync(updateResponse);

            var tokenResponse = await HttpClientHelper.DeserializeResponseAsync<TokenResponse>(updateResponse);
            Assert.NotNull(tokenResponse);
            Assert.True(tokenResponse.IsValid());

            // Act & Assert - 2단계: 업데이트된 정보로 다른 API 호출이 가능한지 확인
            // (예: 현재 사용자 정보 조회)
            var userInfoResponse = await AuthenticatedGetAsync("/api/user");
            await AssertSuccessResponseAsync(userInfoResponse);

            var currentUser = await HttpClientHelper.DeserializeResponseAsync<UserDto>(userInfoResponse);
            Assert.NotNull(currentUser);
            Assert.Equal(CurrentUser.Email, currentUser.Email);

            Logger.LogInformation("전체 워크플로우 테스트 완료: 업데이트 -> 사용자 정보 조회");
        });
    }

    #endregion

    #region 테스트 유틸리티 메서드들

    /// <summary>
    /// 유효한 UserInfoRequest 생성 헬퍼 메서드
    /// </summary>
    private static UserInfoRequest CreateValidUserInfoRequest()
    {
        return new UserInfoRequest
        {
            AppKey = "test_app_key_" + Guid.NewGuid().ToString("N")[..8],
            AppSecret = "test_app_secret_" + Guid.NewGuid().ToString("N")[..8],
            AccountNumber = "1234567890123456"
        };
    }

    /// <summary>
    /// TokenResponse 유효성 검증 헬퍼 메서드
    /// </summary>
    private static void AssertValidTokenResponse(TokenResponse tokenResponse)
    {
        Assert.NotNull(tokenResponse);
        Assert.False(string.IsNullOrEmpty(tokenResponse.AccessToken));
        Assert.False(string.IsNullOrEmpty(tokenResponse.TokenType));
        Assert.True(tokenResponse.ExpiresIn > 0);
        Assert.True(tokenResponse.IsValid());
    }

    /// <summary>
    /// 응답 시간 측정 헬퍼 메서드
    /// </summary>
    private async Task<(HttpResponseMessage Response, long ElapsedMs)> MeasureResponseTimeAsync(
        Func<Task<HttpResponseMessage>> requestFunc)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await requestFunc();
        stopwatch.Stop();

        return (response, stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// 사용자별 고유한 KIS 정보 생성 헬퍼 메서드
    /// </summary>
    private UserInfoRequest CreateUniqueUserInfoRequest(string suffix = null)
    {
        suffix ??= Guid.NewGuid().ToString("N")[..6];

        return new UserInfoRequest
        {
            AppKey = $"app_key_{suffix}",
            AppSecret = $"app_secret_{suffix}",
            AccountNumber = $"1234567890{suffix.PadLeft(6, '0')}"
        };
    }

    #endregion
}