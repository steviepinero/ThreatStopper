using Microsoft.Extensions.Logging;
using WindowsSecurityAgent.Core.Monitoring;
using WindowsSecurityAgent.Core.PolicyEngine;
using Shared.Models.Enums;

namespace WindowsSecurityAgent.Core.Communication;

/// <summary>
/// Syncs URL blocking policies from the cloud and applies them
/// </summary>
public class UrlPolicySyncService
{
    private readonly ILogger<UrlPolicySyncService> _logger;
    private readonly PolicyCache _policyCache;
    private readonly UrlBlocker _urlBlocker;
    private readonly Timer _syncTimer;
    private readonly int _syncIntervalSeconds;

    public UrlPolicySyncService(
        ILogger<UrlPolicySyncService> logger,
        PolicyCache policyCache,
        UrlBlocker urlBlocker,
        int syncIntervalSeconds = 600)
    {
        _logger = logger;
        _policyCache = policyCache;
        _urlBlocker = urlBlocker;
        _syncIntervalSeconds = syncIntervalSeconds;

        // Start periodic sync
        _syncTimer = new Timer(SyncCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(_syncIntervalSeconds));
    }

    private async void SyncCallback(object? state)
    {
        try
        {
            await SyncUrlPoliciesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during URL policy sync");
        }
    }

    /// <summary>
    /// Syncs URL blocking policies and applies them to the hosts file
    /// </summary>
    public async Task SyncUrlPoliciesAsync()
    {
        try
        {
            _logger.LogInformation("Syncing URL blocking policies");

            // Get all policies from cache
            var policies = _policyCache.GetAllPolicies();

            if (policies == null || !policies.Any())
            {
                _logger.LogWarning("No policies available for URL blocking");
                return;
            }

            // Extract URLs to block from all active policies
            var urlsToBlock = new List<string>();

            foreach (var policy in policies.Where(p => p.IsActive))
            {
                foreach (var rule in policy.Rules)
                {
                    // Check for URL or Domain rule types
                    if (rule.RuleType == RuleType.Url || rule.RuleType == RuleType.Domain)
                    {
                        // Only block if action is Block
                        if (rule.Action == BlockAction.Block)
                        {
                            urlsToBlock.Add(rule.Criteria);
                            _logger.LogInformation("Found URL to block: {Url} from policy {PolicyName}", 
                                rule.Criteria, policy.Name);
                        }
                    }
                }
            }

            // Apply URL blocks
            if (urlsToBlock.Any())
            {
                await _urlBlocker.BlockUrlsAsync(urlsToBlock);
                _logger.LogInformation("Applied {Count} URL blocks", urlsToBlock.Count);
            }
            else
            {
                // Clear all blocks if no URLs to block
                await _urlBlocker.ClearBlockedUrlsAsync();
                _logger.LogInformation("No URLs to block, cleared all blocks");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync URL policies");
        }
    }

    public void Dispose()
    {
        _syncTimer?.Dispose();
    }
}

