using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StockTrading.Application.DTOs.Common;
using StockTrading.Application.DTOs.External.KoreaInvestment;
using StockTrading.Application.DTOs.Orders;
using StockTrading.Application.DTOs.Stocks;
using StockTrading.Application.Services;
using StockTrading.Domain.Settings;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

/**
 * KIS와의 통신을 담당하는 클래스
 */
public class KisApiClient : IKisApiClient
{
    private readonly HttpClient _httpClient;
    private readonly KisApiSettings _settings;

    public KisApiClient(HttpClient httpClient, IOptions<KisApiSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<StockOrderResponse> PlaceOrderAsync(StockOrderRequest request, UserDto user)
    {
        var kisRequest = new StockOrderRequestToKis
        {
            CANO = user.AccountNumber,
            ACNT_PRDT_CD = "01",
            PDNO = request.PDNO,
            ORD_DVSN = request.ORD_DVSN,
            ORD_QTY = request.ORD_QTY.ToString(),
            ORD_UNPR = request.ORD_UNPR.ToString(),
        };

        var content = new StringContent(
            JsonSerializer.Serialize(kisRequest),
            Encoding.UTF8,
            "application/json"
        );

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.OrderPath);
        SetRequiredHeaders(httpRequest, request.tr_id, user);
        httpRequest.Content = content;

        var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var orderResponse = JsonSerializer.Deserialize<StockOrderResponse>(responseContent);

        if (orderResponse.rt_cd != "0")
            throw new Exception($"주문 실패: {orderResponse.msg}");

        return orderResponse;
    }

    public async Task<StockBalance> GetStockBalanceAsync(UserDto user)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["CANO"] = user.AccountNumber,
            ["ACNT_PRDT_CD"] = _settings.Defaults.AccountProductCode,
            ["AFHR_FLPR_YN"] = _settings.Defaults.AfterHoursForeignPrice,
            ["OFL_YN"] = _settings.Defaults.OfflineYn,
            ["INQR_DVSN"] = _settings.Defaults.InquiryDivision,
            ["UNPR_DVSN"] = _settings.Defaults.UnitPriceDivision,
            ["FUND_STTL_ICLD_YN"] = _settings.Defaults.FundSettlementInclude,
            ["FNCG_AMT_AUTO_RDPT_YN"] = _settings.Defaults.FinancingAmountAutoRedemption,
            ["PRCS_DVSN"] = _settings.Defaults.ProcessDivision,
            ["CTX_AREA_FK100"] = "",
            ["CTX_AREA_NK100"] = ""
        };
        var queryString = string.Join("&", queryParams
            .Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));

        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{_settings.Endpoints.BalancePath}?{queryString}");

        SetRequiredHeaders(request, _settings.Defaults.BalanceTransactionId, user);

        var response = await _httpClient.SendAsync(request);
        var apiResponse = await response.Content.ReadFromJsonAsync<StockBalanceOutput>();

        return new StockBalance
        {
            Positions = apiResponse.Positions.Select(p => new Position
            {
                StockCode = p.StockCode,
                StockName = p.StockName,
                Quantity = p.Quantity,
                AveragePrice = p.AveragePrice,
                CurrentPrice = p.CurrentPrice,
                ProfitLoss = p.ProfitLoss,
                ProfitLossRate = p.ProfitLossRate
            }).ToList(),

            Summary = new Summary
            {
                TotalDeposit = apiResponse.Summary[0].TotalDeposit,
                StockEvaluation = apiResponse.Summary[0].StockEvaluation,
                TotalEvaluation = apiResponse.Summary[0].TotalEvaluation
            }
        };
    }

    private void SetRequiredHeaders(HttpRequestMessage httpRequestMessage, string trId, UserDto user)
    {
        httpRequestMessage.Headers.Add("authorization", $"Bearer {user.KisToken.AccessToken}");
        httpRequestMessage.Headers.Add("appkey", user.KisAppKey);
        httpRequestMessage.Headers.Add("appsecret", user.KisAppSecret);
        httpRequestMessage.Headers.Add("tr_id", trId);
        httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }
}