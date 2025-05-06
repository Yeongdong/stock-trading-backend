using System.Security.Claims;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using stock_trading_backend.Controllers;
using stock_trading_backend.DTOs;
using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.Services.Interfaces;

namespace StockTrading.Tests.Unit.Controllers;

[TestSubject(typeof(AccountController))]
public class AccountControllerTest
{

    private readonly Mock<IKisService> _mockKisService;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IGoogleAuthProvider> _mockGoogleAuthProvider;
    private readonly AccountController _controller;

    public AccountControllerTest()
    {
        _mockKisService = new Mock<IKisService>();
        _mockUserService = new Mock<IUserService>();
        _mockGoogleAuthProvider = new Mock<IGoogleAuthProvider>();

        _controller = new AccountController(
            _mockKisService.Object,
            _mockUserService.Object,
            _mockGoogleAuthProvider.Object
        );

        // 인증된 사용자 시뮬레이션
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.Email, "test@example.com"),
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task UpdateUserInfo_Success_ReturnsOkResult()
    {
        var userInfoRequest = SetupUserInfo(out var userInfo, out var user);
        var tokenResponse = new TokenResponse
        {
            AccessToken = "access_token_value",
            TokenType = "Bearer",
            ExpiresIn = 86400
        };

        _mockGoogleAuthProvider.Setup(x => x.GetUserInfoAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(userInfo);

        _mockUserService.Setup(x => x.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        _mockKisService.Setup(x => x.UpdateUserKisInfoAndTokensAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(tokenResponse);

        var result = await _controller.UpdateUserInfo(userInfoRequest);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<TokenResponse>(okResult.Value);

        Assert.Equal(tokenResponse.AccessToken, returnValue.AccessToken);
        Assert.Equal(tokenResponse.ExpiresIn, returnValue.ExpiresIn);
        Assert.Equal(tokenResponse.TokenType, returnValue.TokenType);

        _mockKisService.Verify(x => x.UpdateUserKisInfoAndTokensAsync(
            user.Id,
            userInfoRequest.AppKey,
            userInfoRequest.AppSecret,
            userInfoRequest.AccountNumber
        ), Times.Once);
    }

    [Fact]
    public async Task UpdateUserInfo_HttpRequestException_ReturnsBadRequest()
    {
        var userInfoRequest = SetupUserInfo(out var userInfo, out var user);
        var errorMessage = "API 연결 오류";

        _mockGoogleAuthProvider.Setup(x => x.GetUserInfoAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(userInfo);

        _mockUserService.Setup(x => x.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        _mockKisService.Setup(x => x.UpdateUserKisInfoAndTokensAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException(errorMessage));

        var result = await _controller.UpdateUserInfo(userInfoRequest);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(errorMessage, badRequestResult.Value);

        _mockKisService.Verify(x => x.UpdateUserKisInfoAndTokensAsync(
            user.Id,
            userInfoRequest.AppKey,
            userInfoRequest.AppSecret,
            userInfoRequest.AccountNumber
        ), Times.Once);
    }

    [Fact]
    public async Task UpdateUserInfo_Exception_ReturnsInternalServerError()
    {
        var userInfoRequest = SetupUserInfo(out var userInfo, out var user);
        var errorMessage = "내부 서버 오류";

        _mockGoogleAuthProvider.Setup(x => x.GetUserInfoAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(userInfo);

        _mockUserService.Setup(x => x.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        _mockKisService.Setup(x => x.UpdateUserKisInfoAndTokensAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ThrowsAsync(new Exception(errorMessage));

        var result = await _controller.UpdateUserInfo(userInfoRequest);

        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal(errorMessage, statusCodeResult.Value);

        _mockKisService.Verify(x => x.UpdateUserKisInfoAndTokensAsync(
            user.Id,
            userInfoRequest.AppKey,
            userInfoRequest.AppSecret,
            userInfoRequest.AccountNumber
        ), Times.Once);
    }

    [Fact]
    public async Task UpdateUserInfo_GoogleAuthFails_ReturnsBadRequest()
    {
        var userInfoRequest = SetupUserInfo();
        var errorMessage = "구글 인증 실패";

        _mockGoogleAuthProvider.Setup(x => x.GetUserInfoAsync(It.IsAny<ClaimsPrincipal>()))
            .ThrowsAsync(new HttpRequestException(errorMessage));

        var result = await _controller.UpdateUserInfo(userInfoRequest);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(errorMessage, badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateUserInfo_UserNotFound_ReturnsInternalServerError()
    {
        var userInfoRequest = SetupUserInfo(out var userInfo);
        var errorMessage = "사용자를 찾을 수 없습니다";

        _mockGoogleAuthProvider.Setup(x => x.GetUserInfoAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(userInfo);

        _mockUserService.Setup(x => x.GetUserByEmailAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception(errorMessage));

        var result = await _controller.UpdateUserInfo(userInfoRequest);

        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal(errorMessage, statusCodeResult.Value);
    }

    private UserInfoRequest SetupUserInfo()
    {
        var userInfoRequest = new UserInfoRequest
        {
            AppKey = "testAppKey",
            AppSecret = "testAppSecret",
            AccountNumber = "testAccountNumber"
        };
        return userInfoRequest;
    }

    private UserInfoRequest SetupUserInfo(out GoogleUserInfo userInfo)
    {
        var userInfoRequest = new UserInfoRequest
        {
            AppKey = "testAppKey",
            AppSecret = "testAppSecret",
            AccountNumber = "testAccountNumber"
        };

        userInfo = new GoogleUserInfo
        {
            Email = "test@example.com",
            Name = "Test User",
        };
        return userInfoRequest;
    }

    private UserInfoRequest SetupUserInfo(out GoogleUserInfo userInfo, out UserDto user)
    {
        var userInfoRequest = new UserInfoRequest
        {
            AppKey = "testAppKey",
            AppSecret = "testAppSecret",
            AccountNumber = "testAccountNumber"
        };

        userInfo = new GoogleUserInfo
        {
            Email = "test@example.com",
            Name = "Test User",
        };

        user = new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User"
        };
        return userInfoRequest;
    }
}