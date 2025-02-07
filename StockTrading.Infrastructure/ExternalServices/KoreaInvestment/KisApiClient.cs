using System.Net.Http.Json;
using stock_trading_backend.DTOs;
using StockTrading.DataAccess.DTOs;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

public class KisApiClient
{
    private readonly HttpClient _httpClient;
    private const string BASE_URL = "https://openapivts.koreainvestment.com:29443";

    public KisApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(BASE_URL);
    }

    public async Task<StockBalance> GetStockBalanceAsync(User user)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            "uapi/domestic-stock/v1/trading/inquire-balance");

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
        request.RequestUri = new Uri($"{request.RequestUri}?{queryString}");
        
        request.Headers.Add("content-type", "application/json");
        request.Headers.Add("authorization", $"Bearer {user.KisToken.AccessToken}");
        request.Headers.Add("appkey", user.KisAppKey);
        request.Headers.Add("appsecret", user.KisAppSecret);
        request.Headers.Add("tr_id", "VTTC8434R");

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
                TotalDeposit = apiResponse.Summary.TotalDeposit,
                StockEvaluation = apiResponse.Summary.StockEvaluation,
                TotalEvaluation = apiResponse.Summary.TotalEvaluation
            }
        };
    }
}