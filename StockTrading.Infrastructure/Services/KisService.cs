using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.Common;
using StockTrading.Application.DTOs.Orders;
using StockTrading.Application.DTOs.Stocks;
using StockTrading.Application.Repositories;
using StockTrading.Application.Services;
using StockTrading.Domain.Entities;

namespace StockTrading.Infrastructure.Services;

public class KisService : IKisService
{
    private readonly IKisApiClient _kisApiClient;
    private readonly IDbContextWrapper _dbContextWrapper;
    private readonly IOrderRepository _orderRepository;
    private readonly IKisTokenService _kisTokenService;
    private readonly ILogger<KisService> _logger;

    public KisService(IKisApiClient kisApiClient, IDbContextWrapper dbContextWrapper, IOrderRepository orderRepository, IKisTokenService kisTokenService, ILogger<KisService> logger)
    {
        _kisApiClient = kisApiClient;
        _dbContextWrapper = dbContextWrapper;
        _orderRepository = orderRepository;
        _kisTokenService = kisTokenService;
        _logger = logger;
    }

    public async Task<StockOrderResponse> PlaceOrderAsync(StockOrderRequest order, UserDto user)
    {
        _logger.LogInformation("주문 시작: 사용자 {UserId}, 종목 {StockCode}, 수량 {Quantity}", 
            user.Id, order.PDNO, order.ORD_QTY);

        var stockOrder = new StockOrder(
            stockCode: order.PDNO,
            tradeType: order.tr_id,
            orderType: order.ORD_DVSN,
            quantity: order.ORD_QTY,
            price: order.ORD_UNPR,
            user: user.ToEntity()
        );

        await using var transaction = await _dbContextWrapper.BeginTransactionAsync();
    
        var apiResponse = await _kisApiClient.PlaceOrderAsync(order, user);
        await _orderRepository.AddAsync(stockOrder);
    
        await transaction.CommitAsync();
    
        _logger.LogInformation("주문 완료: 사용자 {UserId}, 주문번호 {OrderNumber}", 
            user.Id, apiResponse?.output?.ODNO ?? "알 수 없음");

        return apiResponse;
    }

    public async Task<StockBalance> GetStockBalanceAsync(UserDto user)
    {
        var balance = await _kisApiClient.GetStockBalanceAsync(user);
        
        _logger.LogInformation("잔고 조회 완료: 사용자 {UserId}", user.Id);
        return balance;
    }

    public async Task<TokenResponse> UpdateUserKisInfoAndTokensAsync(int userId, string appKey, string appSecret, string accountNumber)
    {
        await using var transaction = await _dbContextWrapper.BeginTransactionAsync();
        
        _logger.LogDebug("토큰 발급 시작: 사용자 {UserId}", userId);
        var tokenResponse = await _kisTokenService.GetKisTokenAsync(userId, appKey, appSecret, accountNumber);
        
        _logger.LogDebug("웹소켓 토큰 발급 시작: 사용자 {UserId}", userId);
        await _kisTokenService.GetWebSocketTokenAsync(userId, appKey, appSecret);

        await transaction.CommitAsync();
        _logger.LogInformation("토큰 및 사용자 정보 업데이트 완료: 사용자 {UserId}", userId);

        return tokenResponse;
    }
}