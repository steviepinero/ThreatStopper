using System.Security.Cryptography;

namespace Shared.Security;

/// <summary>
/// Generates secure API keys for agents and tenants
/// </summary>
public static class ApiKeyGenerator
{
    /// <summary>
    /// Generates a secure random API key
    /// </summary>
    /// <param name="length">Length of the key in bytes (default 32)</param>
    /// <returns>Base64-encoded API key</returns>
    public static string GenerateApiKey(int length = 32)
    {
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Generates a tenant ID
    /// </summary>
    public static Guid GenerateTenantId()
    {
        return Guid.NewGuid();
    }

    /// <summary>
    /// Hashes an API key for storage
    /// </summary>
    public static string HashApiKey(string apiKey)
    {
        return HashCalculator.CalculateStringHash(apiKey);
    }

    /// <summary>
    /// Verifies an API key against a hash
    /// </summary>
    public static bool VerifyApiKey(string apiKey, string hashedApiKey)
    {
        var computedHash = HashApiKey(apiKey);
        return computedHash.Equals(hashedApiKey, StringComparison.OrdinalIgnoreCase);
    }
}
