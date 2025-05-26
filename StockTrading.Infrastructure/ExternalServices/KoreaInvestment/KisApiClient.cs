using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using StockTrading.Application.DTOs.Common;
using StockTrading.Application.DTOs.External.KoreaInvestment;
using StockTrading.Application.DTOs.Orders;
using StockTrading.Application.DTOs.Stocks;
using StockTrading.Application.Services;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

/**
 * KIS와의 통신을 담당하는 클래스
 */
public class KisApiClient : IKisApiClient
{
    private readonly HttpClient _httpClient;

    public KisApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<StockOrderResponse> PlaceOrderAsync(StockOrderRequest request, UserDto user)
    {
        try
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

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/uapi/domestic-stock/v1/trading/order-cash");
            SetRequiredHeaders(httpRequest, request.tr_id, user);
            httpRequest.Content = content;

            var response = await _httpClient.SendAsync(httpRequest);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var orderResponse = JsonSerializer.Deserialize<StockOrderResponse>(responseContent);

            if (orderResponse.rt_cd != "0")
            {
                throw new Exception($"주문 실패: {orderResponse.msg}");
            }

            return orderResponse;
        }
        catch (HttpRequestException ex)
        {
            throw new Exception("API 요청 중 오류 발생", ex);
        }
        catch (JsonException ex)
        {
            throw new Exception("응답 데이터 처리 중 오류 발생", ex);
        }
    }

    public async Task<StockBalance> GetStockBalanceAsync(UserDto user)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["CANO"] = user.AccountNumber,
            ["ACNT_PRDT_CD"] = "01",
            ["AFHR_FLPR_YN"] = "N",
            ["OFL_YN"] = "",
            ["INQR_DVSN"] = "02",
            ["UNPR_DVSN"] = "01",
            ["FUND_STTL_ICLD_YN"] = "N",
            ["FNCG_AMT_AUTO_RDPT_YN"] = "N",
            ["PRCS_DVSN"] = "00",
            ["CTX_AREA_FK100"] = "",
            ["CTX_AREA_NK100"] = ""
        };
        var queryString = string.Join("&", queryParams
            .Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));

        var request = new HttpRequestMessage(HttpMethod.Get,
            $"uapi/domestic-stock/v1/trading/inquire-balance?{queryString}");

        SetRequiredHeaders(request, "VTTC8434R", user);

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