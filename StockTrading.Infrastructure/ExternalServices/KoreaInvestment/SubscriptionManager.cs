using System.Text.Json;
using Microsoft.Extensions.Logging;
using StockTrading.Application.Services;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

public class SubscriptionManager : ISubscriptionManager
{
    private readonly IWebSocketClient _webSocketClient;
    private readonly ILogger<SubscriptionManager> _logger;
    private readonly Dictionary<string, bool> _subscribedSymbols = new();
    private string _webSocketToken = string.Empty;

    public SubscriptionManager(IWebSocketClient webSocketClient, ILogger<SubscriptionManager> logger)
    {
        _webSocketClient = webSocketClient;
        _logger = logger;
    }

    public async Task SubscribeSymbolAsync(string symbol)
    {
        if (_subscribedSymbols.ContainsKey(symbol)) 
        {
            _logger.LogInformation("📋 [Subscription] 이미 구독 중인 종목: {Symbol}", symbol);
            return;
        }

        if (string.IsNullOrEmpty(_webSocketToken))
        {
            _logger.LogError("❌ [Subscription] WebSocket 토큰이 없습니다.");
            throw new InvalidOperationException("WebSocket 토큰이 설정되지 않았습니다.");
        }

        _logger.LogInformation("📡 [Subscription] 종목 구독 시작: {Symbol}", symbol);

        // KIS WebSocket API 공식 문서 기준 메시지 형태 (체결가로 변경)
        var subscriptionMessage = new
        {
            header = new 
            {
                approval_key = _webSocketToken,
                custtype = "P",        // P: 개인, B: 법인
                tr_type = "1",         // 1: 등록, 2: 해제  
                content_type = "utf-8"
            },
            body = new 
            {
                input = new 
                {
                    tr_id = "H0STCNT0",    // 실시간 주식 체결가 (호가가 아닌 체결가로 변경)
                    tr_key = symbol
                }
            }
        };

        var jsonMessage = JsonSerializer.Serialize(subscriptionMessage, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false  // 압축된 JSON
        });

        _logger.LogInformation("📤 [Subscription] 구독 메시지 (H0STCNT0): {Message}", jsonMessage);

        try
        {
            await _webSocketClient.SendMessageAsync(jsonMessage);
            
            // 구독 후 잠깐 기다려서 응답 확인
            await Task.Delay(1000);
            
            _subscribedSymbols[symbol] = true;
            _logger.LogInformation("✅ [Subscription] 종목 구독 완료: {Symbol} (체결가 실시간 데이터)", symbol);
            
            // 추가로 호가 데이터도 구독
            await SubscribeAskBidAsync(symbol);

            _logger.LogInformation("✅ [Subscription] 종목 구독 완료: {Symbol}, 총 구독 종목 수: {Count}", 
                symbol, _subscribedSymbols.Count);
            _logger.LogDebug("📊 [Subscription] 현재 구독 중인 종목들: {Symbols}", 
                string.Join(", ", _subscribedSymbols.Keys));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Subscription] 종목 구독 실패: {Symbol} - {Error}", symbol, ex.Message);
            throw;
        }
    }

    public async Task UnsubscribeSymbolAsync(string symbol)
    {
        if (!_subscribedSymbols.ContainsKey(symbol))
        {
            _logger.LogDebug("📋 [Subscription] 구독하지 않은 종목 해제 요청: {Symbol}", symbol);
            return;
        }

        _logger.LogInformation("📡 [Subscription] 종목 구독 해제 시작: {Symbol}", symbol);

        var unsubscriptionMessage = new
        {
            header = new 
            {
                approval_key = _webSocketToken,
                custtype = "P",
                tr_type = "2",         // 2: 해제
                content_type = "utf-8"
            },
            body = new 
            {
                input = new 
                {
                    tr_id = "H0STASP0",
                    tr_key = symbol
                }
            }
        };

        var jsonMessage = JsonSerializer.Serialize(unsubscriptionMessage, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        _logger.LogDebug("📤 [Subscription] 구독 해제 메시지: {Message}", jsonMessage);

        try
        {
            await _webSocketClient.SendMessageAsync(jsonMessage);
            _subscribedSymbols.Remove(symbol);

            _logger.LogInformation("✅ [Subscription] 종목 구독 해제 완료: {Symbol}, 남은 구독 종목 수: {Count}", 
                symbol, _subscribedSymbols.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Subscription] 종목 구독 해제 실패: {Symbol} - {Error}", symbol, ex.Message);
            throw;
        }
    }

    public async Task UnsubscribeAllAsync()
    {
        if (_subscribedSymbols.Count == 0)
        {
            _logger.LogDebug("📋 [Subscription] 구독 중인 종목이 없습니다.");
            return;
        }

        _logger.LogInformation("📡 [Subscription] 전체 구독 해제 시작: {Count}개 종목", _subscribedSymbols.Count);

        var symbolsToUnsubscribe = _subscribedSymbols.Keys.ToList();
        
        foreach (var symbol in symbolsToUnsubscribe)
        {
            try
            {
                await UnsubscribeSymbolAsync(symbol);
                await Task.Delay(100); // 메시지 간격 조절
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ [Subscription] 종목 구독 해제 중 오류 (계속 진행): {Symbol}", symbol);
            }
        }

        _logger.LogInformation("✅ [Subscription] 전체 구독 해제 완료");
    }

    public IReadOnlyCollection<string> GetSubscribedSymbols()
    {
        return _subscribedSymbols.Keys.ToList().AsReadOnly();
    }

    public void SetWebSocketToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogError("❌ [Subscription] 빈 WebSocket 토큰이 설정되었습니다.");
            throw new ArgumentException("WebSocket 토큰은 필수입니다.", nameof(token));
        }

        _webSocketToken = token;
        _logger.LogInformation("🔑 [Subscription] WebSocket 토큰 설정 완료: {TokenLength}자", token.Length);
    }

    // 테스트용 메서드 - 실제 KIS 서버 응답 확인
    public async Task SendTestPingAsync()
    {
        if (string.IsNullOrEmpty(_webSocketToken))
        {
            _logger.LogError("❌ [Subscription] WebSocket 토큰이 없어 Ping 테스트를 할 수 없습니다.");
            return;
        }

        var pingMessage = new
        {
            header = new 
            {
                approval_key = _webSocketToken,
                custtype = "P",
                tr_type = "1",
                content_type = "utf-8"
            },
            body = new 
            {
                input = new 
                {
                    tr_id = "PINGPONG",
                    tr_key = "PING"
                }
            }
        };

        var jsonMessage = JsonSerializer.Serialize(pingMessage, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _logger.LogInformation("🏓 [Subscription] Ping 메시지 전송: {Message}", jsonMessage);

        try
        {
            await _webSocketClient.SendMessageAsync(jsonMessage);
            _logger.LogInformation("✅ [Subscription] Ping 메시지 전송 완료");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Subscription] Ping 메시지 전송 실패");
        }
    }

    // 호가 데이터 별도 구독
    private async Task SubscribeAskBidAsync(string symbol)
    {
        var askBidMessage = new
        {
            header = new 
            {
                approval_key = _webSocketToken,
                custtype = "P",
                tr_type = "1",
                content_type = "utf-8"
            },
            body = new 
            {
                input = new 
                {
                    tr_id = "H0STASP0",    // 실시간 주식 호가
                    tr_key = symbol
                }
            }
        };

        var jsonMessage = JsonSerializer.Serialize(askBidMessage, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        _logger.LogInformation("📤 [Subscription] 호가 구독 메시지 (H0STASP0): {Message}", jsonMessage);

        try
        {
            await _webSocketClient.SendMessageAsync(jsonMessage);
            await Task.Delay(500);
            _logger.LogInformation("✅ [Subscription] 호가 구독 완료: {Symbol}", symbol);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ [Subscription] 호가 구독 실패 (체결가는 정상): {Symbol}", symbol);
        }
    }

    // 디버깅용: 다양한 TR ID로 테스트
    public async Task TestVariousSubscriptionsAsync(string symbol)
    {
        var trIds = new[] { "H0STCNT0", "H0STASP0", "H0STCNI0" };
        
        foreach (var trId in trIds)
        {
            _logger.LogInformation("🧪 [Test] {TrId} 구독 테스트: {Symbol}", trId, symbol);
            
            var testMessage = new
            {
                header = new 
                {
                    approval_key = _webSocketToken,
                    custtype = "P",
                    tr_type = "1",
                    content_type = "utf-8"
                },
                body = new 
                {
                    input = new 
                    {
                        tr_id = trId,
                        tr_key = symbol
                    }
                }
            };

            var jsonMessage = JsonSerializer.Serialize(testMessage, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            try
            {
                await _webSocketClient.SendMessageAsync(jsonMessage);
                await Task.Delay(1000); // 응답 대기
                _logger.LogInformation("✅ [Test] {TrId} 메시지 전송 완료", trId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [Test] {TrId} 메시지 전송 실패", trId);
            }
        }
    }
}