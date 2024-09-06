using System.Security.Cryptography;
namespace Infrastructure.Services;

public class EncryptedResult {
    public byte[] Key { get; set; }
    public byte[] IV { get; set; }
    public byte[] Encrypted { get; set; }
}

public static class EncryptionService {
    public static async Task<EncryptedResult> Encrypt(string password) {
        EncryptedResult result = new EncryptedResult();
        using Aes aes = Aes.Create();
        aes.Padding = PaddingMode.PKCS7;
        ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var memoryEncrypt = new MemoryStream();
        await using var cryptoStream = new CryptoStream(memoryEncrypt, encryptor, CryptoStreamMode.Write);
        await using (var writerEncrypt = new StreamWriter(cryptoStream)) {
            await writerEncrypt.WriteAsync(password);
        }
        result.Encrypted = memoryEncrypt.ToArray();
        result.IV = aes.IV;
        result.Key = aes.Key;

        return result;
    }

    public static async Task<string> Decrypt(EncryptedResult encryptedResult) {
        string decrypted;
        using (Aes aes = Aes.Create()) {
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = encryptedResult.Key;
            aes.IV = encryptedResult.IV;
            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var memoryDecrypt = new MemoryStream(encryptedResult.Encrypted);
            await using (var cryptoStream = new CryptoStream(memoryDecrypt, decryptor, CryptoStreamMode.Read)) {
                using (var readerDecrypt = new StreamReader(cryptoStream)) {
                    decrypted = await readerDecrypt.ReadToEndAsync();
                }
            }
        }
        return decrypted;
    }
}