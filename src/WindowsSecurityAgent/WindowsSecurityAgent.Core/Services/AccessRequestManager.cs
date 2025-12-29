using Microsoft.Extensions.Logging;
using Shared.Models.DTOs;
using WindowsSecurityAgent.Core.Communication;

namespace WindowsSecurityAgent.Core.Services;

/// <summary>
/// Manages access requests and approvals
/// </summary>
public class AccessRequestManager
{
    private readonly ILogger<AccessRequestManager> _logger;
    private readonly CloudClient _cloudClient;
    private readonly Guid _agentId;
    private readonly Dictionary<string, DateTime> _pendingRequests = new();
    private readonly HashSet<Guid> _processedRequestIds = new();
    private readonly object _lock = new object();

    public event EventHandler<AccessRequestEventArgs>? AccessRequestNeeded;
    public event EventHandler<AccessApprovalEventArgs>? AccessApproved;
    public event EventHandler<AccessApprovalEventArgs>? AccessDenied;

    public AccessRequestManager(ILogger<AccessRequestManager> logger, CloudClient cloudClient, Guid agentId)
    {
        _logger = logger;
        _cloudClient = cloudClient;
        _agentId = agentId;
    }

    /// <summary>
    /// Checks if a resource has an active approval before blocking
    /// </summary>
    public async Task<bool> CheckApprovalAsync(string resourceType, string resourceIdentifier)
    {
        try
        {
            var check = new AccessApprovalCheckDTO
            {
                AgentId = _agentId,
                ResourceType = resourceType,
                ResourceIdentifier = resourceIdentifier
            };

            var result = await _cloudClient.CheckApprovalAsync(check);
            
            if (result?.IsApproved == true)
            {
                _logger.LogInformation("Access approved for {ResourceType}: {ResourceIdentifier}, expires: {ExpiresAt}", 
                    resourceType, resourceIdentifier, result.ExpiresAt?.ToString() ?? "never");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check approval for {ResourceType}: {ResourceIdentifier}", 
                resourceType, resourceIdentifier);
            return false;
        }
    }

    /// <summary>
    /// Requests access to a blocked resource and shows a popup
    /// </summary>
    public async Task RequestAccessAsync(
        string resourceType, 
        string resourceIdentifier, 
        string resourceName, 
        string userName,
        Guid? policyId = null,
        Guid? ruleId = null)
    {
        try
        {
            // Check if we already have a pending request for this resource (to avoid duplicate popups)
            lock (_lock)
            {
                var key = $"{resourceType}:{resourceIdentifier}";
                if (_pendingRequests.ContainsKey(key))
                {
                    // Check if the request is less than 5 minutes old
                    if (DateTime.UtcNow - _pendingRequests[key] < TimeSpan.FromMinutes(5))
                    {
                        _logger.LogDebug("Access request for {Key} already pending, skipping duplicate", key);
                        return;
                    }
                    else
                    {
                        _pendingRequests.Remove(key);
                    }
                }
            }

            // Trigger event to show popup
            var eventArgs = new AccessRequestEventArgs
            {
                ResourceType = resourceType,
                ResourceIdentifier = resourceIdentifier,
                ResourceName = resourceName,
                UserName = userName,
                PolicyId = policyId,
                RuleId = ruleId
            };

            AccessRequestNeeded?.Invoke(this, eventArgs);

            // Wait for user to provide justification
            if (eventArgs.WaitForUserInput)
            {
                var success = await eventArgs.WaitForCompletion();
                
                if (success && !string.IsNullOrWhiteSpace(eventArgs.Justification))
                {
                    // Submit the access request
                    var request = new CreateAccessRequestDTO
                    {
                        AgentId = _agentId,
                        ResourceType = resourceType,
                        ResourceIdentifier = resourceIdentifier,
                        ResourceName = resourceName,
                        UserName = userName,
                        Justification = eventArgs.Justification,
                        PolicyId = policyId,
                        RuleId = ruleId
                    };

                    var submitted = await _cloudClient.SubmitAccessRequestAsync(request);
                    
                    if (submitted)
                    {
                        lock (_lock)
                        {
                            _pendingRequests[$"{resourceType}:{resourceIdentifier}"] = DateTime.UtcNow;
                        }
                        
                        _logger.LogInformation("Access request submitted for {ResourceType}: {ResourceIdentifier}", 
                            resourceType, resourceIdentifier);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request access for {ResourceType}: {ResourceIdentifier}", 
                resourceType, resourceIdentifier);
        }
    }

    /// <summary>
    /// Notifies the user that their access request was approved
    /// </summary>
    public void NotifyApproved(string resourceType, string resourceName, DateTime? expiresAt)
    {
        try
        {
            var eventArgs = new AccessApprovalEventArgs
            {
                ResourceType = resourceType,
                ResourceName = resourceName,
                ExpiresAt = expiresAt
            };

            AccessApproved?.Invoke(this, eventArgs);

            lock (_lock)
            {
                var key = $"{resourceType}:{resourceName}";
                _pendingRequests.Remove(key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify approval");
        }
    }

    /// <summary>
    /// Notifies the user that their access request was denied
    /// </summary>
    public void NotifyDenied(string resourceType, string resourceName, string reason)
    {
        try
        {
            var eventArgs = new AccessApprovalEventArgs
            {
                ResourceType = resourceType,
                ResourceName = resourceName,
                DenialReason = reason
            };

            AccessDenied?.Invoke(this, eventArgs);

            lock (_lock)
            {
                var key = $"{resourceType}:{resourceName}";
                _pendingRequests.Remove(key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify denial");
        }
    }

    /// <summary>
    /// Checks for newly approved requests and notifies the user
    /// </summary>
    public async Task CheckForRequestUpdatesAsync()
    {
        try
        {
            List<string> keysToCheck;
            lock (_lock)
            {
                keysToCheck = _pendingRequests.Keys.ToList();
            }
            
            foreach (var key in keysToCheck)
            {
                var parts = key.Split(':', 2);
                if (parts.Length == 2)
                {
                    var resourceType = parts[0];
                    var resourceIdentifier = parts[1];
                    
                    // Check if there's an active approval using CloudClient directly to get full response
                    var check = new Shared.Models.DTOs.AccessApprovalCheckDTO
                    {
                        AgentId = _agentId,
                        ResourceType = resourceType,
                        ResourceIdentifier = resourceIdentifier
                    };
                    
                    var approvalResult = await _cloudClient.CheckApprovalAsync(check);
                    
                    if (approvalResult?.IsApproved == true)
                    {
                        // Extract resource name from identifier
                        var resourceName = System.IO.Path.GetFileName(resourceIdentifier);
                        if (string.IsNullOrEmpty(resourceName))
                            resourceName = resourceIdentifier;
                        
                        // Check if we've already notified for this approval
                        var approvalId = approvalResult.ApprovalId;
                        if (approvalId.HasValue)
                        {
                            lock (_lock)
                            {
                                if (!_processedRequestIds.Contains(approvalId.Value))
                                {
                                    _processedRequestIds.Add(approvalId.Value);
                                    _pendingRequests.Remove(key);
                                }
                                else
                                {
                                    continue; // Already processed
                                }
                            }
                            
                            // Notify outside the lock
                            NotifyApproved(resourceType, resourceName, approvalResult.ExpiresAt);
                            _logger.LogInformation("Notified user of approval for {ResourceType}: {ResourceName}", 
                                resourceType, resourceName);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for request updates");
        }
    }
}

/// <summary>
/// Event arguments for access request events
/// </summary>
public class AccessRequestEventArgs : EventArgs
{
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceIdentifier { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Justification { get; set; } = string.Empty;
    public Guid? PolicyId { get; set; }
    public Guid? RuleId { get; set; }
    public bool WaitForUserInput { get; set; } = true;

    private TaskCompletionSource<bool> _completionSource = new TaskCompletionSource<bool>();

    public void Complete(bool success)
    {
        _completionSource.TrySetResult(success);
    }

    public Task<bool> WaitForCompletion()
    {
        return _completionSource.Task;
    }
}

/// <summary>
/// Event arguments for access approval/denial notifications
/// </summary>
public class AccessApprovalEventArgs : EventArgs
{
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public string DenialReason { get; set; } = string.Empty;
}

