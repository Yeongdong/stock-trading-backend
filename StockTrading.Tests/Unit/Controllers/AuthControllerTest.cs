using Google.Apis.Auth;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using StockTrading.API.Controllers;
using StockTrading.API.Controllers.Auth;
using StockTrading.API.Services;
using StockTrading.API.Validator.Interfaces;
using StockTrading.Application.Common.Interfaces;
using StockTrading.Application.Features.Auth.DTOs;
using StockTrading.Application.Features.Auth.Services;
using StockTrading.Application.Features.Users.DTOs;
using StockTrading.Application.Features.Users.Services;
using StockTrading.Domain.Settings;

namespace StockTrading.Tests.Unit.Controllers;

[TestSubject(typeof(AuthController))]
public class AuthControllerTest
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IGoogleAuthValidator> _mockGoogleAuthValidator;
    private readonly Mock<IUserContextService> _mockUserContextService;
    private readonly Mock<ICookieService> _mockCookieService;
    private readonly Mock<IKisTokenRefreshService> _mockKisTokenRefreshService;
    private readonly AuthController _controller;

    public AuthControllerTest()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockJwtService = new Mock<IJwtService>();
        _mockUserService = new Mock<IUserService>();
        _mockGoogleAuthValidator = new Mock<IGoogleAuthValidator>();
        _mockUserContextService = new Mock<IUserContextService>();
        _mockCookieService = new Mock<ICookieService>();
        _mockKisTokenRefreshService = new Mock<IKisTokenRefreshService>();

        _mockConfiguration.Setup(x => x["Authentication:Google:ClientId"]).Returns("test-client-id");

        _controller = new AuthController(
            _mockConfiguration.Object,
            _mockJwtService.Object,
            _mockUserService.Object,
            _mockGoogleAuthValidator.Object,
            _mockUserContextService.Object,
            _mockCookieService.Object,
            _mockKisTokenRefreshService.Object
        )
        {
            // HttpContext 설정
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

        var payload = new GoogleJsonWebSignature.Payload
        {
            Email = "test@example.com",
            Name = "Test User",
            Subject = "user123"
        };

        var user = new UserInfo
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User"
        };

        var token = "jwt-token-value";

        _mockGoogleAuthValidator
            .Setup(x => x.ValidateAsync(googleLoginRequest.Credential, "test-client-id"))
            .ReturnsAsync(payload);

        _mockUserService
            .Setup(x => x.CreateOrGetGoogleUserAsync(payload))
            .ReturnsAsync(user);

        _mockJwtService
            .Setup(x => x.GenerateToken(user))
            .Returns(token);

        // Act
        var result = await _controller.GoogleLogin(googleLoginRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
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

        var mockPrincipal = new System.Security.Claims.ClaimsPrincipal();

        _controller.ControllerContext.HttpContext.Request.Headers.Cookie = "auth_token=valid-token";

        _mockJwtService
            .Setup(x => x.ValidateToken("valid-token"))
            .Returns(mockPrincipal);

        _mockUserContextService
            .Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(testUser);

        // Act
        var result = await _controller.CheckAuth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task CheckAuth_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var result = await _controller.CheckAuth();

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}