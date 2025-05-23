using System.Text.Json;
using Microsoft.Extensions.Logging;
using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.DTOs.OrderDTOs;
using StockTrading.DataAccess.Repositories;
using StockTrading.DataAccess.Services.Interfaces;
using StockTrading.Infrastructure.ExternalServices.Interfaces;
using StockTrading.Infrastructure.Interfaces;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Infrastructure.Implementations;

public class KisService : IKisService
{
    private readonly IKisApiClient _kisApiClient;
    private readonly IDbContextWrapper _dbContextWrapper;
    private readonly IOrderRepository _orderRepository;
    private readonly IKisTokenService _kisTokenService;
    private readonly ILogger<KisService> _logger;

    public KisService(IKisApiClient kisApiClient, IDbContextWrapper dbContextWrapper, IOrderRepository orderRepository,
        IKisTokenService kisTokenService, ILogger<KisService> logger)
    {
        _kisApiClient = kisApiClient;
        _dbContextWrapper = dbContextWrapper;
        _orderRepository = orderRepository;
        _kisTokenService = kisTokenService;

        _logger = logger;
    }

    public async Task<StockOrderResponse> PlaceOrderAsync(StockOrderRequest order, UserDto user)
    {
        try
        {
            _logger.LogInformation($"주문 시작: 사용자 {user.Id}, 종목코드 {order.PDNO}, 수량 {order.ORD_QTY}");
            if (!int.TryParse(order.ORD_QTY, out int quantity))
                throw new ArgumentException("유효하지 않은 수량입니다.", nameof(order.ORD_QTY));

            if (!decimal.TryParse(order.ORD_UNPR, out decimal price))
                throw new ArgumentException("유효하지 않은 가격입니다.", nameof(order.ORD_UNPR));
            var stockOrder = new StockOrder(
                stockCode: order.PDNO,
                tradeType: order.tr_id,
                orderType: order.ORD_DVSN,
                quantity: quantity,
                price: price,
                user: user.ToEntity()
            );

            var apiResponse = await _kisApiClient.PlaceOrderAsync(order, user);
            await _orderRepository.SaveAsync(stockOrder, user);

            _logger.LogInformation($"주문 완료: 사용자 {user.Id}, 주문번호 {apiResponse?.output?.ODNO ?? "알 수 없음"}");

            return apiResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"주문 처리 중 오류 발생: 사용자 {user.Id}, 종목코드 {order.PDNO}");
            throw;
        }
    }

    public async Task<StockBalance> GetStockBalanceAsync(UserDto user)
    {
        try
        {
            _logger.LogInformation($"잔고 조회 시작: 사용자 {user.Id}");
            var balance = await _kisApiClient.GetStockBalanceAsync(user);
            _logger.LogInformation($"잔고 조회 완료: 사용자 {user.Id}");
            return balance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"잔고 조회 중 오류 발생: 사용자 {user.Id}");
            throw;
        }
    }

    public async Task<TokenResponse> UpdateUserKisInfoAndTokensAsync(int userId, string appKey, string appSecret,
        string accountNumber)
    {
        _logger.LogInformation($"토큰 및 사용자 정보 업데이트 시작: 사용자 {userId}");

        await using var transaction = await _dbContextWrapper.BeginTransactionAsync();
        try
        {
            _logger.LogDebug($"토큰 발급 시작: 사용자 {userId}");
            var tokenResponse = await _kisTokenService.GetKisTokenAsync(userId, appKey, appSecret, accountNumber);
            _logger.LogDebug($"토큰 발급 완료: 사용자 {userId}, 액세스 토큰: {tokenResponse.AccessToken.Substring(0, 10)}...");

            _logger.LogDebug($"웹소켓 토큰 발급 시작: 사용자 {userId}");
            var webSocketToken = await _kisTokenService.GetWebSocketTokenAsync(userId, appKey, appSecret);
            _logger.LogDebug($"웹소켓 토큰 발급 완료: 사용자 {userId}");

            await transaction.CommitAsync();
            _logger.LogInformation($"토큰 및 사용자 정보 업데이트 완료: 사용자 {userId}");

            return tokenResponse;
        }
        catch (NullReferenceException)
        {
            await transaction.RollbackAsync();
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, $"API 호출 중 오류 발생: 사용자 {userId}, 상태 코드: {ex.StatusCode}");
            await transaction.RollbackAsync();
            throw new Exception("한국투자증권 API 호출 중 오류가 발생했습니다.", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, $"응답 데이터 처리 중 오류 발생: 사용자 {userId}");
            await transaction.RollbackAsync();
            throw new Exception("응답 데이터 처리 중 오류가 발생했습니다.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"토큰 업데이트 중 예상치 못한 오류 발생: 사용자 {userId}");
            await transaction.RollbackAsync();
            throw new Exception("토큰 업데이트 중 오류가 발생했습니다.", ex);
        }
    }
}