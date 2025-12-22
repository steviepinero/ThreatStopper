using System.Security.Cryptography;
using System.Text;

namespace Shared.Security;

/// <summary>
/// Provides AES encryption and decryption utilities
/// </summary>
public static class EncryptionHelper
{
    private const int KeySize = 256;
    private const int BlockSize = 128;

    /// <summary>
    /// Encrypts a string using AES
    /// </summary>
    /// <param name="plainText">Text to encrypt</param>
    /// <param name="key">Encryption key (base64 encoded)</param>
    /// <returns>Encrypted text (base64 encoded)</returns>
    public static string Encrypt(string plainText, string key)
    {
        var keyBytes = Convert.FromBase64String(key);
        
        using var aes = Aes.Create();
        aes.KeySize = KeySize;
        aes.BlockSize = BlockSize;
        aes.Key = keyBytes;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Prepend IV to cipher text
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// Decrypts an AES-encrypted string
    /// </summary>
    /// <param name="cipherText">Encrypted text (base64 encoded)</param>
    /// <param name="key">Decryption key (base64 encoded)</param>
    /// <returns>Decrypted plain text</returns>
    public static string Decrypt(string cipherText, string key)
    {
        var keyBytes = Convert.FromBase64String(key);
        var cipherBytes = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.KeySize = KeySize;
        aes.BlockSize = BlockSize;
        aes.Key = keyBytes;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        // Extract IV from cipher text
        var iv = new byte[aes.BlockSize / 8];
        var cipher = new byte[cipherBytes.Length - iv.Length];
        Buffer.BlockCopy(cipherBytes, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(cipherBytes, iv.Length, cipher, 0, cipher.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }

    /// <summary>
    /// Generates a new AES encryption key
    /// </summary>
    /// <returns>Base64-encoded encryption key</returns>
    public static string GenerateKey()
    {
        using var aes = Aes.Create();
        aes.KeySize = KeySize;
        aes.GenerateKey();
        return Convert.ToBase64String(aes.Key);
    }
}
