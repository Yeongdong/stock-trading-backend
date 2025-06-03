using StockTrading.Domain.Enums;

namespace StockTrading.Domain.Entities;

public class Stock
{
    public string Code { get; private set; } = null!; // 종목코드 (6자리)
    public string Name { get; private set; } = null!; // 종목명 (약어)
    public string FullName { get; private set; } = null!; // 종목명 (정식)
    public string? EnglishName { get; private set; } // 영문명
    public string Sector { get; private set; } = null!; // 업종
    public string? StockType { get; private set; } // 주식 종류 (예: 보통주)
    public string? ParValue { get; private set; } // 액면가
    public string? ListedShares { get; private set; } // 상장주식수
    public DateTime? ListedDate { get; private set; } // 상장일
    public Market Market { get; private set; } // 시장구분 (KOSPI/KOSDAQ/KONEX)
    public DateTime LastUpdated { get; private set; } // 마지막 업데이트 시간

    private Stock() { }

    public Stock(
        string code,
        string name,
        string fullName,
        string sector,
        Market market,
        string? englishName = null,
        string? stockType = null,
        string? parValue = null,
        string? listedShares = null,
        DateTime? listedDate = null)
    {
        ValidateCode(code);
        ValidateName(name);
        ValidateName(fullName);
        ValidateSector(sector);

        Code = code;
        Name = name;
        FullName = fullName;
        EnglishName = englishName;
        Sector = sector;
        Market = market;
        StockType = stockType;
        ParValue = parValue;
        ListedShares = listedShares;
        ListedDate = listedDate;
        LastUpdated = DateTime.UtcNow;
    }

    public void UpdateInfo(
        string name,
        string fullName,
        string sector,
        Market market,
        string? englishName = null,
        string? stockType = null,
        string? parValue = null,
        string? listedShares = null,
        DateTime? listedDate = null)
    {
        ValidateName(name);
        ValidateName(fullName);
        ValidateSector(sector);

        Name = name;
        FullName = fullName;
        EnglishName = englishName;
        Sector = sector;
        Market = market;
        StockType = stockType;
        ParValue = parValue;
        ListedShares = listedShares;
        ListedDate = listedDate;
        LastUpdated = DateTime.UtcNow;
    }

    private static void ValidateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("종목코드는 필수입니다.", nameof(code));

        if (code.Length != 6 || !code.All(char.IsDigit))
            throw new ArgumentException("종목코드는 6자리 숫자여야 합니다.", nameof(code));
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("이름은 필수입니다.", nameof(name));
    }

    private static void ValidateSector(string sector)
    {
        if (string.IsNullOrWhiteSpace(sector))
            throw new ArgumentException("업종은 필수입니다.", nameof(sector));
    }
}
