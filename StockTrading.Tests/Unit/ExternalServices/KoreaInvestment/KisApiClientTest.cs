using System.Net;
using System.Text;
using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using StockTrading.Application.DTOs.External.KoreaInvestment;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.DTOs.Trading.Orders;
using StockTrading.Application.DTOs.Users;
using StockTrading.Domain.Settings;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Converters;

namespace StockTrading.Tests.Unit.ExternalServices.KoreaInvestment;

[TestSubject(typeof(KisApiClient))]
public class KisApiClientTest
{
    private Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private HttpClient _httpClient;
    private Mock<IOptions<KisApiSettings>> _mockSettings;
    private Mock<ILogger<KisApiClient>> _mockLogger;
    private Mock<StockDataConverter> _mockConverter;
    private KisApiClient _kisApiClient;
    private UserInfo _testUser;

    private void SetupTest()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://openapivts.koreainvestment.com:29443")
        };
        _mockSettings = new Mock<IOptions<KisApiSettings>>();
        _mockLogger = new Mock<ILogger<KisApiClient>>();
        _mockConverter = new Mock<StockDataConverter>(Mock.Of<ILogger<StockDataConverter>>());
        _mockSettings.Setup(x => x.Value).Returns(CreateTestSettings());
        _kisApiClient = new KisApiClient(_httpClient, _mockSettings.Object, _mockLogger.Object, _mockConverter.Object);
        _testUser = CreateTestUser();
    }

    [Fact]
    public async Task PlaceOrderAsync_ValidRequest_ReturnsSuccessResponse()
    {
        SetupTest();

        var successResponse = new OrderResponse
        {
            ReturnCode = "0",
            MessageCode = "MCA0000",
            Message = "정상처리 되었습니다.",
            Output =
                new KisOrderData
                {
                    KrxForwardOrderOrgNo = "12345",
                    OrderNumber = "123456789",
                    OrderTime = "121212"
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

        var orderRequest = new OrderRequest
        {
            ACNT_PRDT_CD = "01",
            tr_id = "TTTC0802U", // 매수 주문
            PDNO = "005930", // 삼성전자
            ORD_DVSN = "00", // 지정가
            ORD_QTY = "10", // 10주
            ORD_UNPR = "70000" // 70,000원
        };

        var result = await _kisApiClient.PlaceOrderAsync(orderRequest, _testUser);

        Assert.Equal("0", result.ReturnCode);
        Assert.Equal("MCA0000", result.MessageCode);
        Assert.Equal("정상처리 되었습니다.", result.Message);
        Assert.NotNull(result.Output);
        Assert.Equal("123456789", result.Output.OrderNumber);
    }

    [Fact]
    public async Task PlaceOrderAsync_ErrorResponse_ThrowsException()
    {
        SetupTest();

        var errorResponse = new KisOrderResponse
        {
            ReturnCode = "1",
            MessageCode = "ERC00001",
            Message = "오류가 발생했습니다."
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

        var orderRequest = new OrderRequest
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

        var balanceResponse = new KisBalanceResponse
        {
            Positions =
            [
                new KisPositionResponse
                {
                    StockCode = "005930",
                    StockName = "삼성전자",
                    Quantity = "100",
                    AveragePrice = "68000",
                    CurrentPrice = "70000",
                    ProfitLoss = "200000",
                    ProfitLossRate = "2.94"
                }
            ],
            Summary =
            [
                new KisAccountSummaryResponse
                {
                    TotalDeposit = "10000000",
                    StockEvaluation = "7000000",
                    TotalEvaluation = "17000000"
                }
            ]
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

    private static KisApiSettings CreateTestSettings()
    {
        return new KisApiSettings
        {
            BaseUrl = "https://openapivts.koreainvestment.com:29443",
            WebSocketUrl = "ws://ops.koreainvestment.com:31000",
            Endpoints = new ApiEndpoints
            {
                TokenPath = "/oauth2/tokenP",
                WebSocketApprovalPath = "/oauth2/Approval",
                OrderPath = "/uapi/domestic-stock/v1/trading/order-cash",
                BalancePath = "/uapi/domestic-stock/v1/trading/inquire-balance"
            },
            Defaults = new DefaultValues
            {
                AccountProductCode = "01",
                BalanceTransactionId = "VTTC8434R",
                AfterHoursForeignPrice = "N",
                OfflineYn = "",
                InquiryDivision = "02",
                UnitPriceDivision = "01",
                FundSettlementInclude = "N",
                FinancingAmountAutoRedemption = "N",
                ProcessDivision = "00"
            }
        };
    }

    private static UserInfo CreateTestUser()
    {
        return new UserInfo
        {
            Id = 1,
            Email = "test@example.com",
            Name = "테스트 사용자",
            KisAppKey = "test_app_key",
            KisAppSecret = "test_app_secret",
            AccountNumber = "50123456789",
            KisToken = new KisTokenInfo
            {
                Id = 1,
                AccessToken = "test_access_token",
                TokenType = "Bearer",
                ExpiresIn = DateTime.UtcNow.AddMinutes(5),
            },
            WebSocketToken = "web_socket_token"
        };
    }
}