using Google.Apis.Auth;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using stock_trading_backend.controllers;
using stock_trading_backend.DTOs;
using stock_trading_backend.Interfaces;
using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.Services.Interfaces;
using StockTradingBackend.DataAccess.Interfaces;

namespace StockTrading.Tests.Controllers;

[TestSubject(typeof(AuthController))]
public class AuthControllerTest
{

    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IGoogleAuthValidator> _mockGoogleAuthValidator;
    private readonly AuthController _controller;

    public AuthControllerTest()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockJwtService = new Mock<IJwtService>();
        _mockUserService = new Mock<IUserService>();
        _mockGoogleAuthValidator = new Mock<IGoogleAuthValidator>();
            
        _mockConfiguration.Setup(x => x["Authentication:Google:ClientId"]).Returns("test-client-id");
            
        _controller = new AuthController(
            _mockConfiguration.Object,
            _mockJwtService.Object,
            _mockUserService.Object,
            _mockGoogleAuthValidator.Object
        );
    }
    [Fact]
    public async Task GoogleLogin_Success_ReturnsOkResult()
    {
        var googleLoginRequest = SetupGoogleLoginRequest(out var payload, out var user);
        var token = "jwt-token-value";
            
        _mockGoogleAuthValidator.Setup(x => x.ValidateAsync(
                It.IsAny<string>(), 
                It.IsAny<string>()))
            .ReturnsAsync(payload);
            
        _mockUserService.Setup(x => x.GetOrCreateGoogleUserAsync(It.IsAny<GoogleJsonWebSignature.Payload>()))
            .ReturnsAsync(user);
            
        _mockJwtService.Setup(x => x.GenerateToken(It.IsAny<UserDto>()))
            .Returns(token);
            
        var result = await _controller.GoogleLogin(googleLoginRequest);
            
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<GoogleLoginResponse>(okResult.Value);
            
        Assert.Equal(user.Email, response.User.Email);
        Assert.Equal(token, response.Token);
            
        _mockGoogleAuthValidator.Verify(x => x.ValidateAsync(
            googleLoginRequest.Credential,
            "test-client-id"
        ), Times.Once);
    }
    
    [Fact]
    public async Task GoogleLogin_InvalidToken_ReturnsBadRequest()
    {
        var googleLoginRequest = new GoogleLoginRequest
        {
            Credential = "invalid-google-token"
        };
            
        var errorMessage = "구글 토큰 인증 실패";
            
        _mockGoogleAuthValidator.Setup(x => x.ValidateAsync(
                It.IsAny<string>(), 
                It.IsAny<string>()))
            .ThrowsAsync(new Exception(errorMessage));
            
        var result = await _controller.GoogleLogin(googleLoginRequest);
            
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(errorMessage, badRequestResult.Value);
    }
    
    [Fact]
    public async Task GoogleLogin_UserServiceException_ReturnsBadRequest()
    {
        var googleLoginRequest = new GoogleLoginRequest
        {
            Credential = "valid-google-token"
        };
            
        var payload = new GoogleJsonWebSignature.Payload
        {
            Email = "test@example.com",
            Name = "Test User",
            Subject = "user123"
        };
            
        var errorMessage = "사용자 생성 실패";
            
        _mockGoogleAuthValidator.Setup(x => x.ValidateAsync(
                It.IsAny<string>(), 
                It.IsAny<string>()))
            .ReturnsAsync(payload);
            
        _mockUserService.Setup(x => x.GetOrCreateGoogleUserAsync(It.IsAny<GoogleJsonWebSignature.Payload>()))
            .ThrowsAsync(new Exception(errorMessage));
            
        var result = await _controller.GoogleLogin(googleLoginRequest);
            
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(errorMessage, badRequestResult.Value);
    }
    
    [Fact]
    public async Task GoogleLogin_JwtServiceException_ReturnsBadRequest()
    {
        var googleLoginRequest = SetupGoogleLoginRequest(out var payload, out var user);
        var errorMessage = "토큰 생성 실패";
            
        _mockGoogleAuthValidator.Setup(x => x.ValidateAsync(
                It.IsAny<string>(), 
                It.IsAny<string>()))
            .ReturnsAsync(payload);
            
        _mockUserService.Setup(x => x.GetOrCreateGoogleUserAsync(It.IsAny<GoogleJsonWebSignature.Payload>()))
            .ReturnsAsync(user);
            
        _mockJwtService.Setup(x => x.GenerateToken(It.IsAny<UserDto>()))
            .Throws(new Exception(errorMessage));
            
        var result = await _controller.GoogleLogin(googleLoginRequest);
            
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(errorMessage, badRequestResult.Value);
    }

    private GoogleLoginRequest SetupGoogleLoginRequest(out GoogleJsonWebSignature.Payload payload, out UserDto user)
    {
        var googleLoginRequest = new GoogleLoginRequest
        {
            Credential = "valid-google-token"
        };
            
        payload = new GoogleJsonWebSignature.Payload
        {
            Email = "test@example.com",
            Name = "Test User",
            Subject = "user123"
        };
            
        user = new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User"
        };
        
        return googleLoginRequest;
    }
}