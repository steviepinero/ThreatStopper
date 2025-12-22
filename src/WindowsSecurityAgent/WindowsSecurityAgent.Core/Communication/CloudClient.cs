using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Shared.Models.DTOs;

namespace WindowsSecurityAgent.Core.Communication;

/// <summary>
/// HTTP client for communicating with the cloud management API
/// </summary>
public class CloudClient : IDisposable
{
    private readonly ILogger<CloudClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;
    private readonly string _apiKey;
    private readonly Guid _agentId;

    public CloudClient(ILogger<CloudClient> logger, string apiBaseUrl, string apiKey, Guid agentId)
    {
        _logger = logger;
        _apiBaseUrl = apiBaseUrl.TrimEnd('/');
        _apiKey = apiKey;
        _agentId = agentId;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_apiBaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// Registers the agent with the cloud
    /// </summary>
    public async Task<AgentRegistrationResponseDTO?> RegisterAgentAsync(AgentRegistrationDTO registration, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Registering agent with cloud...");
            
            var response = await _httpClient.PostAsJsonAsync("/api/agents/register", registration, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AgentRegistrationResponseDTO>(cancellationToken: cancellationToken);
            
            if (result?.Success == true)
            {
                _logger.LogInformation("Agent registered successfully with ID: {AgentId}", result.AgentId);
            }
            else
            {
                _logger.LogWarning("Agent registration failed: {Message}", result?.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register agent");
            return null;
        }
    }

    /// <summary>
    /// Gets policies assigned to this agent
    /// </summary>
    public async Task<List<PolicyDTO>> GetPoliciesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching policies from cloud...");
            
            var response = await _httpClient.GetAsync($"/api/agents/{_agentId}/policies", cancellationToken);
            response.EnsureSuccessStatusCode();

            var policies = await response.Content.ReadFromJsonAsync<List<PolicyDTO>>(cancellationToken: cancellationToken);
            
            _logger.LogDebug("Retrieved {Count} policies from cloud", policies?.Count ?? 0);
            return policies ?? new List<PolicyDTO>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch policies from cloud");
            return new List<PolicyDTO>();
        }
    }

    /// <summary>
    /// Sends a heartbeat to the cloud
    /// </summary>
    public async Task<bool> SendHeartbeatAsync(HeartbeatDTO heartbeat, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Sending heartbeat to cloud...");
            
            var response = await _httpClient.PostAsJsonAsync($"/api/agents/{_agentId}/heartbeat", heartbeat, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogDebug("Heartbeat sent successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send heartbeat");
            return false;
        }
    }

    /// <summary>
    /// Submits audit logs to the cloud
    /// </summary>
    public async Task<bool> SubmitAuditLogsAsync(List<AuditLogDTO> logs, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!logs.Any())
                return true;

            _logger.LogDebug("Submitting {Count} audit logs to cloud...", logs.Count);
            
            var response = await _httpClient.PostAsJsonAsync($"/api/agents/{_agentId}/audit-logs", logs, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogDebug("Audit logs submitted successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit audit logs");
            return false;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
