using StockTrading.Domain.Enums;

namespace StockTrading.Domain.Entities;

public class Stock
{
    public string Code { get; private set; } = null!; // 종목코드 (6자리)
    public string Name { get; private set; } = null!; // 회사명 (한글)
    public string? EnglishName { get; private set; } // 영문명
    public string Sector { get; private set; } = null!; // 업종
    public Market Market { get; private set; } // 시장구분 (KOSPI/KOSDAQ/KONEX)
    public DateTime LastUpdated { get; private set; } // 마지막 업데이트 시간

    private Stock() { }

    public Stock(string code, string name, string sector, Market market, string? englishName = null)
    {
        ValidateCode(code);
        ValidateName(name);
        ValidateSector(sector);

        Code = code;
        Name = name;
        EnglishName = englishName;
        Sector = sector;
        Market = market;
        LastUpdated = DateTime.UtcNow;
    }

    public void UpdateInfo(string name, string sector, Market market, string? englishName = null)
    {
        ValidateName(name);
        ValidateSector(sector);

        Name = name;
        EnglishName = englishName;
        Sector = sector;
        Market = market;
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
            throw new ArgumentException("회사명은 필수입니다.", nameof(name));
    }

    private static void ValidateSector(string sector)
    {
        if (string.IsNullOrWhiteSpace(sector))
            throw new ArgumentException("업종은 필수입니다.", nameof(sector));
    }
}