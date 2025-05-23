using Microsoft.Extensions.DependencyInjection;
using Moq;
using StockTrading.Infrastructure.Security.Encryption;

namespace StockTrading.Tests.Integration.Configuration;

/// <summary>
/// 암호화 서비스 Mock 설정 담당
/// </summary>
public static class EncryptionServiceMocker
{
    public static void ConfigureMockEncryption(IServiceCollection services)
    {
        // 기존 암호화 서비스 제거
        var encryptionDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEncryptionService));
        if (encryptionDescriptor != null)
            services.Remove(encryptionDescriptor);

        // Mock 암호화 서비스 등록
        services.AddSingleton<IEncryptionService>(provider =>
        {
            var mockEncryption = new Mock<IEncryptionService>();
            mockEncryption.Setup(x => x.Encrypt(It.IsAny<string>())).Returns<string>(input => input);
            mockEncryption.Setup(x => x.Decrypt(It.IsAny<string>())).Returns<string>(input => input);
            return mockEncryption.Object;
        });
    }
}