using System.Text.Json;
using Microsoft.Extensions.Configuration;
using static System.Net.HttpStatusCode;
using static System.Text.Encoding;
using static StockTrading.Tests.Integration.IntegrationTestConstants;

namespace StockTrading.Tests.Integration;

/// <summary>
/// 외부 HTTP 호출을 모킹하는 메시지 핸들러
/// KIS API 호출을 가로채서 테스트용 응답을 반환
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly IConfiguration _configuration;

    public MockHttpMessageHandler(IConfiguration configuration = null)
    {
        _configuration = configuration;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (IsKisApiRequest(request))
            return Task.FromResult(CreateMockKisResponse(request));
        return Task.FromResult(CreateDefaultSuccessResponse());
    }

    /// <summary>
    /// KIS API 요청인지 확인
    /// </summary>
    private static bool IsKisApiRequest(HttpRequestMessage request)
    {
        return request.RequestUri?.Host.Contains(KisApiHostIdentifier) == true;
    }

    /// <summary>
    /// 기본 성공 응답 생성
    /// </summary>
    private static HttpResponseMessage CreateDefaultSuccessResponse()
    {
        return new HttpResponseMessage(OK)
        {
            Content = new StringContent(DefaultSuccessMessage)
        };
    }

    /// <summary>
    /// KIS API Mock 응답 생성
    /// </summary>
    private HttpResponseMessage CreateMockKisResponse(HttpRequestMessage request)
    {
        var pathAndQuery = request.RequestUri?.PathAndQuery ?? string.Empty;

        return pathAndQuery switch
        {
            var path when path.Contains("/oauth2/tokenP") => CreateTokenResponse(),
            var path when path.Contains("/oauth2/Approval") => CreateApprovalResponse(),
            var path when path.Contains("/inquire-balance") => CreateBalanceResponse(),
            var path when path.Contains("/order-cash") => CreateOrderResponse(),
            _ => CreateDefaultSuccessResponse()
        };
    }

    /// <summary>
    /// 토큰 발급 응답 생성
    /// </summary>
    private HttpResponseMessage CreateTokenResponse()
    {
        var mockConfig = _configuration?.GetSection("TestData:MockApi");
        var userConfig = _configuration?.GetSection("TestData:User");

        var tokenResponse = new
        {
            access_token = mockConfig?["AccessToken"] ?? "mock_access_token",
            token_type = userConfig?["TokenType"] ?? "Bearer",
            expires_in = mockConfig?.GetValue<int>("TokenExpiresIn") ?? 86400
        };

        return CreateJsonResponse(tokenResponse);
    }

    /// <summary>
    /// 웹소켓 승인 응답 생성
    /// </summary>
    private HttpResponseMessage CreateApprovalResponse()
    {
        var mockConfig = _configuration?.GetSection("TestData:MockApi");

        var approvalResponse = new
        {
            approval_key = mockConfig?["ApprovalKey"] ?? "mock_approval_key"
        };

        return CreateJsonResponse(approvalResponse);
    }

    /// <summary>
    /// 잔고 조회 응답 생성
    /// </summary>
    private HttpResponseMessage CreateBalanceResponse()
    {
        var mockConfig = _configuration?.GetSection("TestData:MockApi");
        var stockConfig = mockConfig?.GetSection("Stock");
        var accountConfig = mockConfig?.GetSection("Account");
        var responseConfig = mockConfig?.GetSection("Response");

        var balanceResponse = new
        {
            rt_cd = responseConfig?["SuccessReturnCode"] ?? "0",
            msg_cd = responseConfig?["SuccessMessageCode"] ?? "MCA0000",
            output1 = new[]
            {
                new
                {
                    pdno = stockConfig?["Code"] ?? "005930",
                    prdt_name = stockConfig?["Name"] ?? "삼성전자",
                    hldg_qty = stockConfig?["Quantity"] ?? "10",
                    pchs_avg_pric = stockConfig?["AveragePrice"] ?? "70000",
                    prpr = stockConfig?["CurrentPrice"] ?? "75000",
                    evlu_pfls_amt = stockConfig?["ProfitLoss"] ?? "50000",
                    evlu_pfls_rt = stockConfig?["ProfitLossRate"] ?? "7.14"
                }
            },
            output2 = new[]
            {
                new
                {
                    dnca_tot_amt = accountConfig?["TotalDeposit"] ?? "1000000",
                    scts_evlu_amt = accountConfig?["StockEvaluation"] ?? "750000",
                    tot_evlu_amt = accountConfig?["TotalEvaluation"] ?? "1750000"
                }
            }
        };

        return CreateJsonResponse(balanceResponse);
    }

    /// <summary>
    /// 주문 응답 생성
    /// </summary>
    private HttpResponseMessage CreateOrderResponse()
    {
        var mockConfig = _configuration?.GetSection("TestData:MockApi");
        var orderConfig = mockConfig?.GetSection("Order");
        var responseConfig = mockConfig?.GetSection("Response");

        var orderResponse = new
        {
            rt_cd = responseConfig?["SuccessReturnCode"] ?? "0",
            msg_cd = responseConfig?["SuccessMessageCode"] ?? "MCA0000",
            msg = responseConfig?["SuccessMessage"] ?? "정상처리 되었습니다.",
            output = new
            {
                KRX_FWDG_ORD_ORGNO = orderConfig?["OrgNo"] ?? "12345",
                ODNO = orderConfig?["OrderNo"] ?? "0000123456",
                ORD_TMD = orderConfig?["OrderTime"] ?? "153045"
            }
        };

        return CreateJsonResponse(orderResponse);
    }

    /// <summary>
    /// JSON 응답 생성 헬퍼 메서드
    /// </summary>
    private static HttpResponseMessage CreateJsonResponse(object responseObject)
    {
        var json = JsonSerializer.Serialize(responseObject);
        return new HttpResponseMessage(OK)
        {
            Content = new StringContent(json, UTF8, "application/json")
        };
    }
}