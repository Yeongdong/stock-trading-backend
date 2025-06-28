using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using StockTrading.API.Controllers.Auth;
using StockTrading.API.Services;
using StockTrading.Application.Common.Interfaces;
using StockTrading.Application.Features.Auth.DTOs;
using StockTrading.Application.Features.Auth.Services;
using StockTrading.Application.Features.Users.DTOs;
using StockTrading.Application.Features.Users.Services;
using StockTrading.Domain.Exceptions.Authentication;
using System.Security.Claims;

namespace StockTrading.Tests.Unit.Controllers;

[TestSubject(typeof(AuthController))]
public class AuthControllerTest
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<IUserContextService> _mockUserContextService;
    private readonly Mock<ICookieService> _mockCookieService;
    private readonly Mock<IKisTokenRefreshService> _mockKisTokenRefreshService;
    private readonly AuthController _controller;

    public AuthControllerTest()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockJwtService = new Mock<IJwtService>();
        _mockUserService = new Mock<IUserService>();
        _mockAuthService = new Mock<IAuthService>();
        _mockUserContextService = new Mock<IUserContextService>();
        _mockCookieService = new Mock<ICookieService>();
        _mockKisTokenRefreshService = new Mock<IKisTokenRefreshService>();

        _mockConfiguration.Setup(x => x["Authentication:Google:ClientId"]).Returns("test-client-id");
        _mockConfiguration.Setup(x => x["Authentication:Google:masterId"]).Returns("master@example.com");

        _controller = new AuthController(
            _mockConfiguration.Object,
            _mockJwtService.Object,
            _mockUserService.Object,
            _mockAuthService.Object,
            _mockUserContextService.Object,
            _mockCookieService.Object,
            _mockKisTokenRefreshService.Object
        )
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task GoogleLogin_Success_ReturnsOkResult()
    {
        // Arrange
        var googleLoginRequest = new LoginRequest
        {
            Credential = "valid-google-token"
        };

        var loginResponse = new LoginResponse
        {
            User = new UserInfo
            {
                Id = 1,
                Email = "test@example.com",
                Name = "Test User"
            },
            IsAuthenticated = true,
            Message = "로그인 성공"
        };

        _mockAuthService
            .Setup(x => x.GoogleLoginAsync(googleLoginRequest.Credential))
            .ReturnsAsync(loginResponse);

        // Act
        var result = await _controller.GoogleLogin(googleLoginRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<LoginResponse>(okResult.Value);
        Assert.Equal(loginResponse.User.Email, returnValue.User.Email);
        Assert.True(returnValue.IsAuthenticated);
    }

    [Fact]
    public async Task CheckAuth_WithValidToken_ReturnsOkResult()
    {
        // Arrange
        var testUser = new UserInfo
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User"
        };

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, testUser.Email),
            new Claim(ClaimTypes.Name, testUser.Name),
            new Claim(ClaimTypes.NameIdentifier, testUser.Id.ToString())
        };
        var mockPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        // Authorization 헤더 설정
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = "Bearer valid-token";

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        _mockJwtService
            .Setup(x => x.ValidateToken("valid-token"))
            .Returns(mockPrincipal);

        _mockUserContextService
            .Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(testUser);

        _mockKisTokenRefreshService
            .Setup(x => x.EnsureValidTokenAsync(testUser))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CheckAuth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var response = okResult.Value as LoginResponse;
        Assert.NotNull(response);
        Assert.Equal(testUser.Email, response.User.Email);
        Assert.True(response.IsAuthenticated);
    }

    [Fact]
    public async Task CheckAuth_WithoutAuthorizationHeader_ReturnsUnauthorized()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        // Authorization 헤더를 설정하지 않음

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await _controller.CheckAuth();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorizedResult.Value);

        // dynamic 대신 구체적인 타입 사용
        var response = unauthorizedResult.Value.ToString();
        Assert.Contains("인증되지 않음", response);
    }

    [Fact]
    public async Task CheckAuth_WithInvalidBearerFormat_ReturnsUnauthorized()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = "InvalidFormat token";

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await _controller.CheckAuth();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorizedResult.Value);

        // dynamic 대신 구체적인 타입 사용
        var response = unauthorizedResult.Value.ToString();
        Assert.Contains("인증되지 않음", response);
    }

    [Fact]
    public async Task CheckAuth_WithInvalidToken_ThrowsTokenValidationException()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = "Bearer invalid-token";

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        _mockJwtService
            .Setup(x => x.ValidateToken("invalid-token"))
            .Throws(new TokenValidationException("토큰 검증 실패"));

        // Act & Assert
        await Assert.ThrowsAsync<TokenValidationException>(async () => await _controller.CheckAuth());
    }

    [Fact]
    public async Task RefreshToken_Success_ReturnsOkResult()
    {
        // Arrange
        var refreshResponse = new RefreshTokenResponse
        {
            AccessToken = "new-access-token",
            Message = "토큰 갱신 성공"
        };

        _mockAuthService
            .Setup(x => x.RefreshTokenAsync())
            .ReturnsAsync(refreshResponse);

        // Act
        var result = await _controller.RefreshToken();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<RefreshTokenResponse>(okResult.Value);
        Assert.Equal(refreshResponse.AccessToken, returnValue.AccessToken);
        Assert.Equal(refreshResponse.Message, returnValue.Message);
    }

    [Fact]
    public async Task RefreshToken_WithAuthenticationException_ReturnsUnauthorized()
    {
        // Arrange
        _mockAuthService
            .Setup(x => x.RefreshTokenAsync())
            .ThrowsAsync(new System.Security.Authentication.AuthenticationException("토큰 갱신 실패"));

        // Act
        var result = await _controller.RefreshToken();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorizedResult.Value);

        // dynamic 대신 구체적인 타입 사용
        var response = unauthorizedResult.Value.ToString();
        Assert.Contains("토큰 갱신 실패", response);
    }

    [Fact]
    public async Task Logout_Success_ReturnsOkResult()
    {
        // Arrange
        var testUser = new UserInfo
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User"
        };

        _mockUserContextService
            .Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(testUser);

        _mockAuthService
            .Setup(x => x.LogoutAsync(testUser.Id))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Logout();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        // dynamic 대신 구체적인 타입 사용
        var response = okResult.Value.ToString();
        Assert.Contains("로그아웃 성공", response);
    }

    [Fact]
    public async Task MasterLogin_Success_ReturnsOkResult()
    {
        // Arrange
        var masterUser = new UserInfo
        {
            Id = 1,
            Email = "master@example.com",
            Name = "Master User"
        };

        var token = "master-jwt-token";

        _mockUserService
            .Setup(x => x.GetUserByEmailAsync("master@example.com"))
            .ReturnsAsync(masterUser);

        _mockJwtService
            .Setup(x => x.GenerateAccessToken(masterUser))
            .Returns(token);

        _mockCookieService
            .Setup(x => x.SetAuthCookie(token))
            .Verifiable();

        // Act
        var result = await _controller.MasterLogin();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<LoginResponse>(okResult.Value);
        Assert.Equal(masterUser.Email, returnValue.User.Email);
        Assert.Equal("마스터 로그인 성공", returnValue.Message);

        _mockCookieService.Verify(x => x.SetAuthCookie(token), Times.Once);
    }

    [Fact]
    public async Task MasterLogin_WithNullUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockUserService
            .Setup(x => x.GetUserByEmailAsync("master@example.com"))
            .ThrowsAsync(new KeyNotFoundException("사용자를 찾을 수 없습니다."));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await _controller.MasterLogin());
    }
}