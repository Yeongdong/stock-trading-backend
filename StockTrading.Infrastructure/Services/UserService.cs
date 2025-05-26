using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.Common;
using StockTrading.Application.Repositories;
using StockTrading.Application.Services;
using StockTrading.Domain.Entities;

namespace StockTrading.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;
    private readonly IDbContextWrapper _dbContextWrapper;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger, IDbContextWrapper dbContextWrapper)
    {
        _userRepository = userRepository;
        _logger = logger;
        _dbContextWrapper = dbContextWrapper;
    }

    public async Task<UserDto> GetOrCreateGoogleUserAsync(GoogleJsonWebSignature.Payload payload)
    {
        _logger.LogInformation("Attempting to get or create user with Google ID: {GoogleId}", payload.Subject);

        if (payload == null)
        {
            _logger.LogWarning("Google payload is null");
            throw new NullReferenceException(nameof(payload));
        }

        var user = await _userRepository.GetByGoogleIdAsync(payload.Subject);

        if (user == null)
        {
            await using var transaction = await _dbContextWrapper.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Creating new user for Google ID: {GoogleId}", payload.Subject);
                var newUser = new User
                {
                    Email = payload.Email,
                    Name = payload.Name,
                    GoogleId = payload.Subject,
                    CreatedAt = DateTime.UtcNow,
                    Role = "User",
                };
                user = await _userRepository.AddAsync(newUser);
                
                await transaction.CommitAsync();
                _logger.LogInformation("New user created with ID: {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to create new user for Google ID: {GoogleId}", payload.Subject);
                throw;
            }
        }
        else
        {
            _logger.LogInformation("Existing user found with ID: {UserId}", user.Id);
        }

        return MapToDto(user);
    }

    public async Task<UserDto> GetUserByEmailAsync(string email)
    {
        _logger.LogInformation("Attempting to get user by email: {Email}", email);

        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Invalid email provided: empty or null");
            throw new ArgumentException("Email cannot be empty or null", nameof(email));
        }

        try
        {
            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null)
            {
                _logger.LogWarning("User not found with email: {Email}", email);
                throw new KeyNotFoundException($"User with email {email} not found");
            }

            return MapToDto(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user with email: {Email}", email);
            throw;
        }
    }

    private UserDto MapToDto(User user)
    {
        if (user == null)
            return null;

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            AccountNumber = user.AccountNumber,
            KisAppKey = user.KisAppKey,
            KisAppSecret = user.KisAppSecret,
            KisToken = user.KisToken == null
                ? null
                : new KisTokenDto
                {
                    Id = user.KisToken.Id,
                    AccessToken = user.KisToken.AccessToken,
                    ExpiresIn = user.KisToken.ExpiresIn,
                    TokenType = user.KisToken.TokenType,
                }
        };
    }
}