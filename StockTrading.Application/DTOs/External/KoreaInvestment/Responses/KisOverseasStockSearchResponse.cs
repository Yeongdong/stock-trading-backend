namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

/// <summary>
/// KIS API 해외주식 조건검색 응답 (내부용)
/// </summary>
public class KisOverseasStockSearchResponse
{
    public string rt_cd { get; set; } = string.Empty;
    public string msg_cd { get; set; } = string.Empty;
    public string msg1 { get; set; } = string.Empty;
    public KisOverseasSearchOutput? output { get; set; }
    public List<KisOverseasStockItem> output1 { get; set; } = [];
}

public class KisOverseasSearchOutput
{
    public string zdiv { get; set; } = string.Empty;
    public string stat { get; set; } = string.Empty;
    public string crec { get; set; } = string.Empty;
    public string trec { get; set; } = string.Empty;
    public string nrec { get; set; } = string.Empty;
}

public class KisOverseasStockItem
{
    public string rsym { get; set; } = string.Empty;
    public string excd { get; set; } = string.Empty;
    public string symb { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public string ename { get; set; } = string.Empty;
    public string last { get; set; } = string.Empty;
    public string sign { get; set; } = string.Empty;
    public string diff { get; set; } = string.Empty;
    public string rate { get; set; } = string.Empty;
    public string tvol { get; set; } = string.Empty;
    public string popen { get; set; } = string.Empty;
    public string phigh { get; set; } = string.Empty;
    public string plow { get; set; } = string.Empty;
    public string valx { get; set; } = string.Empty;
    public string shar { get; set; } = string.Empty;
    public string avol { get; set; } = string.Empty;
    public string eps { get; set; } = string.Empty;
    public string per { get; set; } = string.Empty;
    public string rank { get; set; } = string.Empty;
    public string e_ordyn { get; set; } = string.Empty;
}