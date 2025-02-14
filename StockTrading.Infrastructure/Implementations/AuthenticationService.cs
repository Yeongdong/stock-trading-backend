using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using StockTrading.DataAccess.Repositories;
using StockTrading.DataAccess.Services.Interfaces;
using StockTradingBackend.DataAccess.Interfaces;
using IAuthenticationService = StockTradingBackend.DataAccess.Interfaces.IAuthenticationService;

namespace StockTrading.Infrastructure.Implementations;

public class AuthenticationService : IAuthenticationService
{
    private readonly IConfiguration _configuration;
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IGoogleAuthProvider _googleAuthProvider;

    public AuthenticationService(IConfiguration configuration, IUserRepository userRepository, IJwtService jwtService, IGoogleAuthProvider googleAuthProvider)
    {
        _configuration = configuration;
        _userRepository = userRepository;
        _jwtService = jwtService;
        _googleAuthProvider = googleAuthProvider;
    }

    public AuthenticationProperties ConfigureGoogleAuth()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = "/api/auth/oauth2/callback/google",
            IsPersistent = true,
            AllowRefresh = true,
        };
        
        var state = Guid.NewGuid().ToString();
        properties.Items["state"] = state;
        
        Console.WriteLine($"Generated state: {state}");
        
        return properties;
    }

    // public async Task<AuthResponse> HandleGoogleCallbackAsync(ClaimsPrincipal principal)
    // {
    //     var googleUser = await _googleAuthProvider.GetUserInfoAsync(principal);
    //     var user = await _userRepository.GetByEmailAsync(googleUser.Email) ??
    //                await CreateUserFromGoogleInfo(googleUser);
    //
    //     return await GenerateAuthResponse(user);
    // }
    //
    // public async Task<AuthResponse> LoginAsync(LoginRequest request)
    // {
    //     var user = await _userRepository.GetByEmailAsync(request.Email)
    //                ?? throw new UnauthorizedAccessException("Invalid credentials");
    //
    //     if (!VerifyPassword(request.Password, user.PasswordHash))
    //         throw new UnauthorizedAccessException("Invalid credentials");
    //
    //     var token = _jwtService.GenerateToken(user);
    //     var refreshToken = _jwtService.GenerateRefreshToken().token;
    //
    //     user.RefreshToken = refreshToken;
    //     user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7);
    //     await _userRepository.UpdateAsync(user);
    //
    //     return new AuthResponse(token, refreshToken);
    // }

    // public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    // {
    //     var user = await _userRepository.GetByRefreshTokenAsync(refreshToken)
    //                ?? throw new UnauthorizedAccessException("Invalid refresh token");
    //
    //     if (user.RefreshTokenExpiryTime <= DateTime.Now)
    //     {
    //         throw new UnauthorizedAccessException("Refresh token expired");
    //     }
    //
    //     var token = _jwtService.GenerateToken(user);
    //     var newRefreshToken = _jwtService.GenerateRefreshToken().token;
    //
    //     user.RefreshToken = newRefreshToken;
    //     user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7);
    //     await _userRepository.UpdateAsync(user);
    //
    //     return new AuthResponse(token, newRefreshToken);
    // }
    //
    // private bool VerifyPassword(string password, string hash)
    // {
    //     return BCrypt.Net.BCrypt.Verify(password, hash);
    // }
    //
    // private async Task<AuthResponse> GenerateAuthResponse(User user)
    // {
    //     var token = _jwtService.GenerateToken(user);
    //     var refreshToken = _jwtService.GenerateRefreshToken().token;
    //
    //     user.RefreshToken = refreshToken;
    //     user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7);
    //     await _userRepository.UpdateAsync(user);
    //
    //     return new AuthResponse(token, refreshToken);
    // }
    //
    // private async Task<User> CreateUserFromGoogleInfo(GoogleUserInfo googleUser)
    // {
    //     var user = new User
    //     {
    //         Email = googleUser.Email,
    //     };
    //     await _userRepository.CreateAsync(user);
    //     return user;
    // }
}