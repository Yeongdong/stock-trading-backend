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
    /// 변동 구분
    /// </summary>
    public static class ChangeTypes
    {
        public const string Rise = "상승";
        public const string Fall = "하락";
    }
}