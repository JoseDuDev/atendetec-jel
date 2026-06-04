using System.Security.Cryptography;
using System.Text;

namespace Atendefy.API.SharedKernel.Extensions;

public static class AesEncryption
{
    public static string Encrypt(string plainText, string key)
    {
        var keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        aes.IV.CopyTo(result, 0);
        cipherBytes.CopyTo(result, aes.IV.Length);
        return Convert.ToBase64String(result);
    }

    public static string Decrypt(string cipherText, string key)
    {
        var keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        var fullBytes = Convert.FromBase64String(cipherText);
        using var aes = Aes.Create();
        aes.Key = keyBytes;
        var iv = fullBytes[..16];
        var cipher = fullBytes[16..];
        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
