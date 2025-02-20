using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly KisApiClient _kisApiClient;
    private readonly ApplicationDbContext _context;
    private readonly IOrderRepository _orderRepository;
    private readonly IKisTokenService _kisTokenService;
    private readonly ILogger<KisService> _logger;

    public KisService(KisApiClient kisApiClient, ApplicationDbContext context, IOrderRepository orderRepository, IKisTokenService kisTokenService, ILogger<KisService> logger)
    {
        _kisApiClient = kisApiClient;
        _context = context;
        _orderRepository = orderRepository;
        _kisTokenService = kisTokenService;
        _logger = logger;
    }

    public async Task<StockOrderResponse> PlaceOrderAsync(StockOrderRequest order, UserDto user)
    {
        var stockOrder = new StockOrder(
            stockCode: order.PDNO,
            tradeType: order.tr_id,
            orderType: order.ORD_DVSN,
            quantity: int.Parse(order.ORD_QTY),
            price: decimal.Parse(order.ORD_UNPR),
            user: user.ToEntity()
        );
        
        var apiResponse = await _kisApiClient.PlaceOrderAsync(order, user);
        await _orderRepository.SaveAsync(stockOrder, user);
        
        return apiResponse;
    }
    
    public async Task<StockBalance> GetStockBalanceAsync(UserDto user)
    {
        return await _kisApiClient.GetStockBalanceAsync(user);
    }

    public async Task<TokenResponse> UpdateUserKisInfoAndTokensAsync(int userId, string appKey, string appSecret,
        string accountNumber)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var tokenResponse = await _kisTokenService.GetKisTokenAsync(userId, appKey, appSecret, accountNumber);
            var webSocketToken = await _kisTokenService.GetWebSocketTokenAsync(userId, appKey, appSecret);

            await transaction.CommitAsync();
            return tokenResponse;
        }
        catch
        {
            await transaction.RollbackAsync();
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