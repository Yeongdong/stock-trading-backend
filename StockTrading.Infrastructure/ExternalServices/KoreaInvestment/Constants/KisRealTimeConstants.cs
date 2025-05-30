namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Constants;

/// <summary>
/// KIS 실시간 데이터 관련 상수들
/// </summary>
public static class KisRealTimeConstants
{
    /// <summary>
    /// 실시간 메시지 타입들
    /// </summary>
    public static class MessageTypes
    {
        /// <summary>실시간 주식 호가</summary>
        public const string StockAskBid = "H0STASP0";

        /// <summary>실시간 주식 체결</summary>
        public const string StockExecution = "H0STCNT0";

        /// <summary>실시간 체결통보</summary>
        public const string TradeNotification = "H0STCNI0";

        /// <summary>실시간 체결통보 (모의투자)</summary>
        public const string TradeNotificationDemo = "H0STCNI9";

        /// <summary>핑퐁 메시지</summary>
        public const string PingPong = "PINGPONG";
    }

    /// <summary>
    /// 구독 관련 상수들
    /// </summary>
    public static class Subscription
    {
        /// <summary>등록</summary>
        public const string Register = "1";

        /// <summary>해제</summary>
        public const string Unregister = "2";

        /// <summary>실시간 주식 호가 TR ID</summary>
        public const string StockAskBidTrId = "H0STASP0";
    }

    /// <summary>
    /// 메시지 파싱 관련 상수
    /// </summary>
    public static class Parsing
    {
        public const char PipeDelimiter = '|';
        public const char FieldDelimiter = '^';
        public const int MinimumPipeSegments = 4;
        public const int MinimumFieldsForProcessing = 15;
        public const int StockCodeLength = 6;
        public const int TradeTimeLength = 6;
        public const string JsonStartPattern = "{";
        public const string SubscribeSuccessMessage = "SUBSCRIBE SUCCESS";
    }

    /// <summary>
    /// 필드 인덱스 (H0STCNT0 기준)
    /// </summary>
    public static class FieldIndices
    {
        public const int StockCode = 0;
        public const int TradeTime = 1;
        public const int CurrentPrice = 2;
        public const int ChangeSign = 3;
        public const int PriceChange = 4;
        public const int ChangeRate = 5;
        public const int WeightedAvgPrice = 6;
        public const int OpenPrice = 7;
        public const int HighPrice = 8;
        public const int LowPrice = 9;
        public const int AskPrice1 = 10;
        public const int BidPrice1 = 11;
        public const int Volume = 12;
        public const int TotalVolume = 13;
        public const int TotalTradeAmount = 14;
    }

    /// <summary>
    /// 변동 구분
    /// </summary>
    public static class ChangeTypes
    {
        public const string Rise = "상승";
        public const string Fall = "하락";
        public const string Unchanged = "보합";

        /// <summary>상승 코드</summary>
        public static readonly string[] RiseCodes = ["1", "2"];

        /// <summary>하락 코드</summary>
        public static readonly string[] FallCodes = ["4", "5"];
    }

    /// <summary>
    /// JSON 속성명
    /// </summary>
    public static class JsonProperties
    {
        public const string Header = "header";
        public const string Body = "body";
        public const string TrId = "tr_id";
        public const string TrKey = "tr_key";
        public const string ReturnCode = "rt_cd";
        public const string Message = "msg1";
        public const string SuccessCode = "0";
    }
}

/// <summary>
/// 메시지 파싱 결과
/// </summary>
public record MessageParseResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public string? TrId { get; private init; }
    public string? Data { get; private init; }
    public int DataCount { get; private init; }

    public static MessageParseResult Success(string? trId, string? data, int dataCount = 1) =>
        new() { IsSuccess = true, TrId = trId, Data = data, DataCount = dataCount };

    public static MessageParseResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}