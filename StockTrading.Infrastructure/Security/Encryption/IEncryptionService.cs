namespace StockTrading.Infrastructure.Security.Encryption;

public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}