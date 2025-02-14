using System.ComponentModel;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using stock_trading_backend.DTOs;
using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.DTOs.OrderDTOs;
using StockTrading.DataAccess.Repositories;
using StockTrading.DataAccess.Services.Interfaces;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment;
using StockTrading.Infrastructure.Repositories;
using StockTradingBackend.DataAccess.Entities;
using StockTradingBackend.DataAccess.Enums;

namespace StockTrading.Infrastructure.Implementations;

/**
 * 서비스 사용을 위한 로직 구현 계층
 */
public class KisService : IKisService
{
    private readonly HttpClient _httpClient;
    private readonly KisApiClient _kisApiClient;
    private readonly ApplicationDbContext _context;
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<KisService> _logger;
    private const string BASE_URL = "https://openapivts.koreainvestment.com:29443";

    public KisService(HttpClient httpClient, KisApiClient kisApiClient, ApplicationDbContext context, IOrderRepository orderRepository, ILogger<KisService> logger)
    {
        _httpClient = httpClient;
        _kisApiClient = kisApiClient;
        _context = context;
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<StockOrderResponse> PlaceOrderAsync(StockOrderRequest order, UserDto user)
    {
        // stockOrder를 만들어서
        // 1. 요청 전달 후
        // 2. DB에 저장
        var stockOrder = new StockOrder(
            stockCode: order.PDNO,
            orderType: order.ORD_DVSN,
            quantity: int.Parse(order.ORD_QTY),
            price: decimal.Parse(order.ORD_UNPR),
            user: user.ToEntity()
        );
        
        var kisRequest = new StockOrderRequest
        {
            ACNT_PRDT_CD = "01",
            PDNO = order.PDNO,
            ORD_DVSN = order.ORD_DVSN,
            ORD_QTY = order.ORD_QTY,
            ORD_UNPR = order.ORD_UNPR,
        };

        var apiResponse = await _kisApiClient.PlaceOrderAsync(kisRequest, user);
        await _orderRepository.SaveAsync(stockOrder);
        
        return apiResponse;

        // return await _kisApiClient.PlaceOrderAsync();
    }
    
    public async Task<StockBalance> GetStockBalanceAsync(UserDto user)
    {
        return await _kisApiClient.GetStockBalanceAsync(user);
    }

    public async Task<TokenResponse> UpdateUserKisInfoAndTokenAsync(int userId, string appKey, string appSecret,
        string accountNumber)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var tokenResponse = await GetKisTokenAsync(appKey, appSecret);
            var expiresIn = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

            await SaveTokenAsync(userId, tokenResponse.AccessToken, expiresIn, tokenResponse.TokenType);
            await UpdateUserKisInfo(userId, appKey, appSecret, accountNumber);

            await transaction.CommitAsync();
            return tokenResponse;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<TokenResponse> GetKisTokenAsync(string appKey, string appSecret)
    {
        try
        {
            var bodyData = new
            {
                grant_type = "client_credentials",
                appkey = appKey,
                appsecret = appSecret
            };

            var response = await _httpClient.PostAsJsonAsync($"{BASE_URL}/oauth2/tokenP", bodyData);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"Response Status: {response.StatusCode}");
            _logger.LogInformation($"Response Content: {responseContent}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"토큰 발급 실패: {responseContent}");
                throw new HttpRequestException($"Failed to get token: {responseContent}");
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                throw new InvalidOperationException("Invalid token response");
            }

            _logger.LogInformation("토큰 발급 성공");
            return tokenResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError($"토큰 발급 중 에러 발생: {ex.Message}");
            throw;
        }
    }

    /**
     * User의 Kis 관련 정보 관리
     */
    private async Task UpdateUserKisInfo(int userId, string appKey, string appSecret, string accountNumber)
    {
        try
        {
            _logger.LogInformation($"KIS 정보 업데이트 시도 - UserId: {userId}");

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
            {
                _logger.LogError($"사용자를 찾을 수 없습니다 - UserId: {userId}");
                throw new KeyNotFoundException($"UserId {userId}에 해당하는 사용자를 찾을 수 없습니다.");
            }

            user.KisAppKey = appKey;
            user.KisAppSecret = appSecret;
            user.AccountNumber = accountNumber;

            _context.Users.Update(user);
            var result = await _context.SaveChangesAsync();

            _logger.LogInformation($"KIS 정보 업데이트 완료 - 변경사항: {result}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"KIS 정보 업데이트 중 에러 발생: {ex.Message}");
            _logger.LogError($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    /**
     * KisToken 정보 관리
     **/
    private async Task SaveTokenAsync(int userId, string accessToken, DateTime expiresIn, string tokenType)
    {
        try
        {
            _logger.LogInformation($"토큰 저장 시도 - UserId: {userId}");
            _logger.LogInformation($"토큰 저장 시도 - AccessToken: {accessToken}");

            var existingToken = await _context.KisTokens
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (existingToken != null)
            {
                _logger.LogInformation("기존 토큰 업데이트");
                existingToken.AccessToken = accessToken;
                existingToken.ExpiresIn = expiresIn;
                existingToken.TokenType = tokenType;
                _context.KisTokens.Update(existingToken);
            }

            _logger.LogInformation("새로운 토큰 생성");
            var newToken = new KisToken
            {
                UserId = userId,
                AccessToken = accessToken,
                ExpiresIn = expiresIn,
                TokenType = tokenType
            };

            await _context.KisTokens.AddAsync(newToken);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"토큰 저장 중 에러 발생: {ex.Message}");
            throw;
        }
    }

    private string GetOrderTypeDescription(OrderType orderType)
    {
        var field = orderType.GetType().GetField(orderType.ToString());
        var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
        return attribute?.Description ?? orderType.ToString();
    }
}