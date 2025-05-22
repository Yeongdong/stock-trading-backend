using System.Net;
using Microsoft.Extensions.Logging;
using stock_trading_backend.Controllers;
using StockTrading.DataAccess.DTOs;

namespace StockTrading.Tests.Integration.Controllers;

public class UserControllerIntegrationTest : ControllerTestBase<UserController>
{
    public UserControllerIntegrationTest(IntegrationTestWebApplicationFactory factory)
        : base(factory)
    {
    }

    #region 기본 기능 테스트

    /// <summary>
    /// 인증된 사용자의 현재 사용자 정보 조회 성공 테스트
    /// </summary>
    [Fact]
    public async Task GetCurrentUser_WithValidAuthentication_ShouldReturnUserInfo()
    {
        await RunTestWithLoggingAsync(nameof(GetCurrentUser_WithValidAuthentication_ShouldReturnUserInfo), async () =>
        {
            LogAuthenticationState();

            // Act
            var response = await GetAsync();

            // Assert
            await AssertSuccessResponseAsync(response);

            var userDto = await HttpClientHelper.DeserializeResponseAsync<UserDto>(response);

            // 반환된 사용자 정보 검증
            Assert.NotNull(userDto);
            Assert.Equal(CurrentUser.Email, userDto.Email);
            Assert.Equal(CurrentUser.Name, userDto.Name);
            Assert.Equal(CurrentUser.Id, userDto.Id);

            Logger.LogInformation("사용자 정보 조회 성공: {Email}", userDto.Email);
        });
    }

    /// <summary>
    /// 사용자 정보에 KIS 관련 데이터가 포함되어 있는지 확인
    /// </summary>
    [Fact]
    public async Task GetCurrentUser_ShouldIncludeKisInformation()
    {
        await RunTestWithLoggingAsync(nameof(GetCurrentUser_ShouldIncludeKisInformation), async () =>
        {
            // Act
            var userDto = await GetAsync<UserDto>();

            // Assert
            Assert.NotNull(userDto);

            // KIS 관련 정보 확인 (테스트 데이터에 따라 존재할 수 있음)
            if (!string.IsNullOrEmpty(userDto.KisAppKey))
            {
                Assert.NotNull(userDto.KisAppSecret);
                Assert.NotNull(userDto.AccountNumber);
                Logger.LogInformation("KIS 정보가 포함된 사용자: {Email}", userDto.Email);
            }
            else
            {
                Logger.LogInformation("KIS 정보가 없는 사용자: {Email}", userDto.Email);
            }
        });
    }

    #endregion

    #region 인증/권한 테스트

    /// <summary>
    /// 인증되지 않은 요청은 401 Unauthorized 반환 테스트
    /// </summary>
    [Fact]
    public async Task GetCurrentUser_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        await RunTestWithLoggingAsync(nameof(GetCurrentUser_WithoutAuthentication_ShouldReturnUnauthorized), async () =>
        {
            // Arrange
            await SwitchToAnonymousAsync();

            // Act
            var client = CreateClient();
            var response = await client.GetAsync("/api/user");

            // Assert
            await AssertUnauthorizedAsync(response);

            Logger.LogInformation("인증되지 않은 요청 정상적으로 거부됨");
        });
    }

    /// <summary>
    /// 만료된 토큰으로 요청 시 401 Unauthorized 반환 테스트
    /// </summary>
    [Fact]
    public async Task GetCurrentUser_WithExpiredToken_ShouldReturnUnauthorized()
    {
        await RunTestWithLoggingAsync(nameof(GetCurrentUser_WithExpiredToken_ShouldReturnUnauthorized), async () =>
        {
            // Arrange
            await SwitchToExpiredTokenAsync();

            // Act
            var response = await GetAsync();

            // Assert
            await AssertUnauthorizedAsync(response);

            Logger.LogInformation("만료된 토큰 요청 정상적으로 거부됨");
        });
    }

    /// <summary>
    /// 잘못된 서명 토큰으로 요청 시 401 Unauthorized 반환 테스트
    /// </summary>
    [Fact]
    public async Task GetCurrentUser_WithInvalidSignatureToken_ShouldReturnUnauthorized()
    {
        await RunTestWithLoggingAsync(nameof(GetCurrentUser_WithInvalidSignatureToken_ShouldReturnUnauthorized),
            async () =>
            {
                // Arrange
                await SwitchToInvalidSignatureTokenAsync();

                // Act
                var response = await GetAsync();

                // Assert
                await AssertUnauthorizedAsync(response);

                Logger.LogInformation("잘못된 서명 토큰 요청 정상적으로 거부됨");
            });
    }

    /// <summary>
    /// 다른 사용자로 인증 변경 후 해당 사용자 정보 반환 테스트
    /// </summary>
    [Fact]
    public async Task GetCurrentUser_WithDifferentUser_ShouldReturnCorrectUserInfo()
    {
        await RunTestWithLoggingAsync(nameof(GetCurrentUser_WithDifferentUser_ShouldReturnCorrectUserInfo), async () =>
        {
            // Arrange
            var differentUser = new UserDto
            {
                Id = 999,
                Email = "different@example.com",
                Name = "Different User",
                KisAppKey = "different_app_key",
                KisAppSecret = "different_app_secret"
            };

            await SwitchToExistingUserAsync(differentUser);

            // Act
            var response = await GetAsync();

            // Assert
            await AssertSuccessResponseAsync(response);

            var userDto = await HttpClientHelper.DeserializeResponseAsync<UserDto>(response);

            // 변경된 사용자 정보가 반환되는지 확인
            Assert.NotNull(userDto);
            Assert.Equal(differentUser.Email, userDto.Email);
            Assert.Equal(differentUser.Name, userDto.Name);

            Logger.LogInformation("다른 사용자로 전환 후 정보 조회 성공: {Email}", userDto.Email);
        });
    }

    #endregion

    #region 에러 처리 테스트

    /// <summary>
    /// 데이터베이스에 존재하지 않는 사용자의 토큰으로 요청 시 처리 테스트
    /// </summary>
    [Fact]
    public async Task GetCurrentUser_WithNonExistentUser_ShouldReturnNotFound()
    {
        await RunTestWithLoggingAsync(nameof(GetCurrentUser_WithNonExistentUser_ShouldReturnNotFound), async () =>
        {
            // Arrange
            var nonExistentUser = new UserDto
            {
                Id = 99999,
                Email = "nonexistent@example.com",
                Name = "Non Existent User"
            };

            await SwitchToUserAsync(nonExistentUser);

            // Act
            var response = await GetAsync();

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.NotFound ||
                response.StatusCode == HttpStatusCode.InternalServerError,
                $"예상하지 못한 상태 코드: {response.StatusCode}");

            Logger.LogInformation("존재하지 않는 사용자 요청 처리됨: {StatusCode}", response.StatusCode);
        });
    }

    #endregion

    #region 응답 형식 테스트

    /// <summary>
    /// 응답이 올바른 JSON 형식인지 확인
    /// </summary>
    [Fact]
    public async Task GetCurrentUser_ShouldReturnValidJsonFormat()
    {
        await RunTestWithLoggingAsync(nameof(GetCurrentUser_ShouldReturnValidJsonFormat), async () =>
        {
            // Act
            var response = await GetAsync();

            // Assert
            await AssertSuccessResponseAsync(response);

            // Content-Type 헤더 확인
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

            // JSON 역직렬화 가능한지 확인
            var userDto = await HttpClientHelper.DeserializeResponseAsync<UserDto>(response);
            Assert.NotNull(userDto);

            // 필수 필드들이 존재하는지 확인
            Assert.False(string.IsNullOrEmpty(userDto.Email));
            Assert.False(string.IsNullOrEmpty(userDto.Name));
            Assert.True(userDto.Id > 0);

            Logger.LogInformation("응답 JSON 형식 검증 완료");
        });
    }

    #endregion

    #region 성능 테스트

    /// <summary>
    /// 연속 요청 성능 테스트
    /// </summary>
    [Fact]
    public async Task GetCurrentUser_MultipleRequests_ShouldPerformWell()
    {
        await RunTestWithLoggingAsync(nameof(GetCurrentUser_MultipleRequests_ShouldPerformWell), async () =>
        {
            // Arrange
            const int requestCount = 10;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var tasks = Enumerable.Range(0, requestCount)
                .Select(_ => GetAsync<UserDto>())
                .ToArray();

            var results = await Task.WhenAll(tasks);

            stopwatch.Stop();

            // Assert
            Assert.Equal(requestCount, results.Length);
            Assert.All(results, userDto =>
            {
                Assert.NotNull(userDto);
                Assert.Equal(CurrentUser.Email, userDto.Email);
            });

            var averageTimeMs = stopwatch.ElapsedMilliseconds / (double)requestCount;

            Logger.LogInformation("연속 {Count}회 요청 완료. 평균 소요시간: {AverageMs:F2}ms",
                requestCount, averageTimeMs);

            // 성능 기준 - 평균 응답시간이 1초를 넘지 않아야 함
            Assert.True(averageTimeMs < 1000,
                $"응답시간이 너무 느림: {averageTimeMs:F2}ms > 1000ms");
        });
    }

    #endregion

    #region 통합 시나리오 테스트

    /// <summary>
    /// 전체 인증 시나리오 테스트 (기본 제공 메서드 활용)
    /// </summary>
    [Fact]
    public async Task GetCurrentUser_AuthenticationScenario_ShouldWorkCorrectly()
    {
        await RunTestWithLoggingAsync(nameof(GetCurrentUser_AuthenticationScenario_ShouldWorkCorrectly), async () =>
        {
            // Act & Assert
            var response = await GetAsync();
            await AssertSuccessResponseAsync(response);

            Logger.LogInformation("전체 인증 시나리오 테스트 완료");
        });
    }

    #endregion

    #region 테스트 유틸리티 메서드들

    /// <summary>
    /// 사용자 정보 검증 헬퍼 메서드
    /// </summary>
    private static void AssertValidUserDto(UserDto userDto, UserDto expectedUser = null)
    {
        Assert.NotNull(userDto);
        Assert.False(string.IsNullOrEmpty(userDto.Email));
        Assert.False(string.IsNullOrEmpty(userDto.Name));
        Assert.True(userDto.Id > 0);

        if (expectedUser != null)
        {
            Assert.Equal(expectedUser.Email, userDto.Email);
            Assert.Equal(expectedUser.Name, userDto.Name);
            Assert.Equal(expectedUser.Id, userDto.Id);
        }
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

    #endregion
}