using Microsoft.Extensions.Logging;
using System.Text.Json;
using Shared.Security;

namespace WindowsSecurityAgent.Core.PolicyEngine;

/// <summary>
/// Caches policies locally for offline operation
/// </summary>
public class PolicyCache
{
    private readonly ILogger<PolicyCache> _logger;
    private readonly string _cacheFilePath;
    private readonly string _encryptionKey;
    private readonly object _lock = new();
    private List<Policy> _cachedPolicies = new();

    public PolicyCache(ILogger<PolicyCache> logger, string cacheDirectory, string encryptionKey)
    {
        _logger = logger;
        _cacheFilePath = Path.Combine(cacheDirectory, "policies.cache");
        _encryptionKey = encryptionKey;
        
        // Ensure cache directory exists
        Directory.CreateDirectory(cacheDirectory);
        
        // Load cached policies on startup
        LoadFromDisk();
    }

    /// <summary>
    /// Gets all active policies from cache
    /// </summary>
    public List<Policy> GetActivePolicies()
    {
        lock (_lock)
        {
            return _cachedPolicies.Where(p => p.IsActive).OrderByDescending(p => p.Priority).ToList();
        }
    }

    /// <summary>
    /// Gets all policies from cache
    /// </summary>
    public List<Policy> GetAllPolicies()
    {
        lock (_lock)
        {
            return new List<Policy>(_cachedPolicies);
        }
    }

    /// <summary>
    /// Updates the policy cache with new policies
    /// </summary>
    public void UpdatePolicies(List<Policy> policies)
    {
        lock (_lock)
        {
            _cachedPolicies = policies;
            SaveToDisk();
            _logger.LogInformation("Policy cache updated with {Count} policies", policies.Count);
        }
    }

    /// <summary>
    /// Adds or updates a single policy in cache
    /// </summary>
    public void UpsertPolicy(Policy policy)
    {
        lock (_lock)
        {
            var existingIndex = _cachedPolicies.FindIndex(p => p.PolicyId == policy.PolicyId);
            if (existingIndex >= 0)
            {
                _cachedPolicies[existingIndex] = policy;
                _logger.LogInformation("Updated policy {PolicyName} in cache", policy.Name);
            }
            else
            {
                _cachedPolicies.Add(policy);
                _logger.LogInformation("Added policy {PolicyName} to cache", policy.Name);
            }
            
            SaveToDisk();
        }
    }

    /// <summary>
    /// Removes a policy from cache
    /// </summary>
    public void RemovePolicy(Guid policyId)
    {
        lock (_lock)
        {
            var removed = _cachedPolicies.RemoveAll(p => p.PolicyId == policyId);
            if (removed > 0)
            {
                SaveToDisk();
                _logger.LogInformation("Removed policy {PolicyId} from cache", policyId);
            }
        }
    }

    /// <summary>
    /// Clears all policies from cache
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _cachedPolicies.Clear();
            SaveToDisk();
            _logger.LogInformation("Policy cache cleared");
        }
    }

    private void SaveToDisk()
    {
        try
        {
            var json = JsonSerializer.Serialize(_cachedPolicies, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            // Encrypt the cache
            var encrypted = EncryptionHelper.Encrypt(json, _encryptionKey);
            File.WriteAllText(_cacheFilePath, encrypted);
            
            _logger.LogDebug("Policy cache saved to disk");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save policy cache to disk");
        }
    }

    private void LoadFromDisk()
    {
        try
        {
            if (!File.Exists(_cacheFilePath))
            {
                _logger.LogInformation("No policy cache file found, starting with empty cache");
                return;
            }

            var encrypted = File.ReadAllText(_cacheFilePath);
            var json = EncryptionHelper.Decrypt(encrypted, _encryptionKey);
            _cachedPolicies = JsonSerializer.Deserialize<List<Policy>>(json) ?? new List<Policy>();
            
            _logger.LogInformation("Loaded {Count} policies from cache", _cachedPolicies.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load policy cache from disk, starting with empty cache");
            _cachedPolicies = new List<Policy>();
        }
    }
}
