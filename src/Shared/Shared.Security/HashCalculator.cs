using System.Security.Cryptography;
using System.Text;

namespace Shared.Security;

/// <summary>
/// Provides file and string hashing utilities
/// </summary>
public static class HashCalculator
{
    /// <summary>
    /// Calculates SHA-256 hash of a file
    /// </summary>
    public static string CalculateFileHash(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found for hashing", filePath);

        using var sha256 = SHA256.Create();
        using var fileStream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(fileStream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Calculates SHA-256 hash of a string
    /// </summary>
    public static string CalculateStringHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Calculates SHA-256 hash of a byte array
    /// </summary>
    public static string CalculateHash(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
