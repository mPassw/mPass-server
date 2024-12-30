using System.Security.Cryptography;
using System.Text;

namespace mPass_server.Services;

// currently this service is not used, so SERVER_ENCRYPTION_KEY is not required
public class EncryptionService(IConfiguration configuration)
{
    private readonly string?
        _encryptionKey = configuration["Server:EncryptionKey"]?.Trim(); // must be 32 characters long

    public async Task<string> EncryptStringAsync(string input)
    {
        if (_encryptionKey?.Length != 32)
            throw new InvalidOperationException("Encryption key must be exactly 32 characters long");

        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(_encryptionKey);
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor(aes.Key, null);
        await using var ms = new MemoryStream();
        await using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            await using var writer = new StreamWriter(cs);
            await writer.WriteAsync(input);
        }

        var encryptedContent = ms.ToArray();
        return Convert.ToBase64String(encryptedContent);
    }

    public async Task<string> DecryptStringAsync(string input)
    {
        if (_encryptionKey?.Length != 32)
            throw new InvalidOperationException("Encryption key must be exactly 32 characters long");

        var cipher = Convert.FromBase64String(input);

        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(_encryptionKey);
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor(aes.Key, null);
        using var ms = new MemoryStream(cipher);
        await using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var reader = new StreamReader(cs);

        return await reader.ReadToEndAsync();
    }
}