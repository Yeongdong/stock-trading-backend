using System.Net;
using System.Text;
using System.Text.Json;
using JetBrains.Annotations;
using Moq;
using Moq.Protected;
using StockTrading.Application.DTOs.Common;
using StockTrading.Application.DTOs.External.KoreaInvestment;
using StockTrading.Application.DTOs.Orders;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

namespace StockTrading.Tests.Unit.ExternalServices.KoreaInvestment;

[TestSubject(typeof(KisApiClient))]
public class KisApiClientTest
{
    private Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private HttpClient _httpClient;
    private KisApiClient _kisApiClient;
    private UserDto _testUser;

    private void SetupTest()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://openapivts.koreainvestment.com:29443")
        };
        _kisApiClient = new KisApiClient(_httpClient);
        _testUser = new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            Name = "테스트 사용자",
            KisAppKey = "test_app_key",
            KisAppSecret = "test_app_secret",
            AccountNumber = "50123456789",
            KisToken = new KisTokenDto
            {
                Id = 1,
                AccessToken = "test_access_token",
                TokenType = "Bearer",
                ExpiresIn = DateTime.UtcNow.AddMinutes(5),
            },
            WebSocketToken = "web_socket_token"
        };
    }

    [Fact]
    public async Task PlaceOrderAsync_ValidRequest_ReturnsSuccessResponse()
    {
        SetupTest();

        var successResponse = new StockOrderResponse
        {
            rt_cd = "0",
            msg_cd = "MCA0000",
            msg = "정상처리 되었습니다.",
            output = new OrderOutput
            {
                KRX_FWDG_ORD_ORGNO = "12345",
                ODNO = "123456789",
                ORD_TMD = "121212"
            }
        };

        var responseContent = new StringContent(
            JsonSerializer.Serialize(successResponse),
            Encoding.UTF8,
            "application/json"
        );

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = responseContent
            });

        var orderRequest = new StockOrderRequest
        {
            ACNT_PRDT_CD = "01",
            tr_id = "TTTC0802U", // 매수 주문
            PDNO = "005930", // 삼성전자
            ORD_DVSN = "00", // 지정가
            ORD_QTY = "10", // 10주
            ORD_UNPR = "70000" // 70,000원
        };

        var result = await _kisApiClient.PlaceOrderAsync(orderRequest, _testUser);

        Assert.Equal("0", result.rt_cd);
        Assert.Equal("MCA0000", result.msg_cd);
        Assert.Equal("정상처리 되었습니다.", result.msg);
        Assert.NotNull(result.output);
        Assert.Equal("123456789", result.output.ODNO);
    }

    [Fact]
    public async Task PlaceOrderAsync_ErrorResponse_ThrowsException()
    {
        SetupTest();
        
        var errorResponse = new StockOrderResponse
        {
            rt_cd = "1",
            msg_cd = "ERC00001",
            msg = "오류가 발생했습니다.",
            output = null
        };
        
        var responseContent = new StringContent(
            JsonSerializer.Serialize(errorResponse),
            Encoding.UTF8,
            "application/json"
        );
        
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = responseContent
            });
        
        var orderRequest = new StockOrderRequest
        {
            ACNT_PRDT_CD = "01",
            tr_id = "TTTC0802U",
            PDNO = "005930",
            ORD_DVSN = "00",
            ORD_QTY = "10",
            ORD_UNPR = "70000"
        };
        
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _kisApiClient.PlaceOrderAsync(orderRequest, _testUser));
        
        Assert.Contains("주문 실패", exception.Message);
    }
    
    [Fact]
    public async Task GetStockBalanceAsync_ValidRequest_ReturnsBalance()
    {
        SetupTest();
        
        var balanceResponse = new StockBalanceOutput
        {
            Positions = new List<StockPosition>
            {
                new StockPosition
                {
                    StockCode = "005930",
                    StockName = "삼성전자",
                    Quantity = "100",
                    AveragePrice = "68000",
                    CurrentPrice = "70000",
                    ProfitLoss = "200000",
                    ProfitLossRate = "2.94"
                }
            },
            Summary = new List<AccountSummary>
            {
                new AccountSummary
                {
                    TotalDeposit = "10000000",
                    StockEvaluation = "7000000",
                    TotalEvaluation = "17000000"
                }
            }
        };
        
        var responseContent = new StringContent(
            JsonSerializer.Serialize(balanceResponse),
            Encoding.UTF8,
            "application/json"
        );
        
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = responseContent
            });
        
        var result = await _kisApiClient.GetStockBalanceAsync(_testUser);
        
        Assert.NotNull(result);
        Assert.NotNull(result.Positions);
        Assert.NotNull(result.Summary);
        Assert.Single(result.Positions);
        Assert.Equal("005930", result.Positions[0].StockCode);
        Assert.Equal("삼성전자", result.Positions[0].StockName);
        Assert.Equal("100", result.Positions[0].Quantity);
        Assert.Equal("10000000", result.Summary.TotalDeposit);
        Assert.Equal("7000000", result.Summary.StockEvaluation);
        Assert.Equal("17000000", result.Summary.TotalEvaluation);
    }
    
    [Fact]
    public async Task GetStockBalanceAsync_HttpError_ThrowsException()
    {
        SetupTest();
    
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("서버 오류", Encoding.UTF8, "application/json") // 유효하지 않은 JSON 형식
            });
    
        await Assert.ThrowsAsync<JsonException>(() => 
            _kisApiClient.GetStockBalanceAsync(_testUser));
    }
    
    [Fact]
    public async Task GetStockBalanceAsync_NetworkError_ThrowsHttpRequestException()
    {
        SetupTest();
    
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("네트워크 연결 오류"));
    
        await Assert.ThrowsAsync<HttpRequestException>(() => 
            _kisApiClient.GetStockBalanceAsync(_testUser));
    }
}