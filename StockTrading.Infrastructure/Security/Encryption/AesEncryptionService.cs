using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using StockTrading.Infrastructure.Security.Options;

namespace StockTrading.Infrastructure.Security.Encryption;

public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesEncryptionService(IOptions<EncryptionOptions> options)
    {
        var encryptionOptions = options.Value;
        _key = Convert.FromBase64String(encryptionOptions.Key);
        _iv = Convert.FromBase64String(encryptionOptions.IV);
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

        using var memoryStream = new MemoryStream();
        using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
        using (var streamWriter = new StreamWriter(cryptoStream))
        {
            streamWriter.Write(plainText);
        }

        return Convert.ToBase64String(memoryStream.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        try
        {
            var buffer = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using var memoryStream = new MemoryStream(buffer);
            using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            using var streamReader = new StreamReader(cryptoStream);

            return streamReader.ReadToEnd();
        }
        catch
        {
            // 복호화 실패시 원본 반환
            return cipherText;
        }
    }
}