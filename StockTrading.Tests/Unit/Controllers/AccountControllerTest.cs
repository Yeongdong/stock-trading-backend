using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StockTrading.API.Controllers;
using StockTrading.API.DTOs.Requests;
using StockTrading.API.Services;
using StockTrading.Application.DTOs.Common;
using StockTrading.Application.Services;

namespace StockTrading.Tests.Unit.Controllers;

[TestSubject(typeof(AccountController))]
public class AccountControllerTest
{
    private readonly Mock<IKisTokenService> _mockKisTokenService;
    private readonly Mock<IUserContextService> _mockUserContextService;
    private readonly AccountController _controller;
    private readonly UserDto _testUser;

    public AccountControllerTest()
    {
        _mockKisTokenService = new Mock<IKisTokenService>();
        _mockUserContextService = new Mock<IUserContextService>();

        _testUser = new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User"
        };

        _controller = new AccountController(
            _mockKisTokenService.Object,
            _mockUserContextService.Object);

        _mockUserContextService
            .Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(_testUser);
    }

    [Fact]
    public async Task UpdateUserInfo_Success_ReturnsOkResult()
    {
        // Arrange
        var userInfoRequest = new UserInfoRequest
        {
            AppKey = "testAppKey",
            AppSecret = "testAppSecret",
            AccountNumber = "testAccountNumber"
        };

        var tokenResponse = new TokenResponse
        {
            AccessToken = "access_token_value",
            TokenType = "Bearer",
            ExpiresIn = 86400
        };

        _mockKisTokenService
            .Setup(x => x.UpdateUserKisInfoAndTokensAsync(
                _testUser.Id,
                userInfoRequest.AppKey,
                userInfoRequest.AppSecret,
                userInfoRequest.AccountNumber))
            .ReturnsAsync(tokenResponse);

        // Act
        var result = await _controller.UpdateUserInfo(userInfoRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<TokenResponse>(okResult.Value);
        Assert.Equal(tokenResponse.AccessToken, returnValue.AccessToken);
    }

    [Fact]
    public async Task UpdateUserInfo_InvalidModel_ReturnsBadRequest()
    {
        // Arrange
        var userInfoRequest = new UserInfoRequest(); // 빈 요청
        _controller.ModelState.AddModelError("AppKey", "Required");

        // Act
        var result = await _controller.UpdateUserInfo(userInfoRequest);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
}