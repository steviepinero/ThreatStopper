using ManagementAPI.Data;
using ManagementAPI.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Models.DTOs;
using Shared.Models.Enums;

namespace ManagementAPI.Core.Services;

/// <summary>
/// Service for managing access requests and approvals
/// </summary>
public class AccessRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccessRequestService> _logger;

    public AccessRequestService(ApplicationDbContext context, ILogger<AccessRequestService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new access request from an agent
    /// </summary>
    public async Task<AccessRequestDTO> CreateAccessRequestAsync(CreateAccessRequestDTO request)
    {
        try
        {
            // Check if there's already a pending request for this resource
            var existingRequest = await _context.AccessRequests
                .FirstOrDefaultAsync(r => 
                    r.AgentId == request.AgentId &&
                    r.ResourceIdentifier == request.ResourceIdentifier &&
                    r.Status == AccessRequestStatus.Pending);

            if (existingRequest != null)
            {
                _logger.LogInformation("Returning existing pending request {RequestId}", existingRequest.RequestId);
                return await MapToDTO(existingRequest);
            }

            var agent = await _context.Agents.FindAsync(request.AgentId);
            if (agent == null)
            {
                throw new ArgumentException($"Agent {request.AgentId} not found");
            }

            var accessRequest = new AccessRequest
            {
                RequestId = Guid.NewGuid(),
                AgentId = request.AgentId,
                TenantId = agent.TenantId,
                ResourceType = request.ResourceType,
                ResourceIdentifier = request.ResourceIdentifier,
                ResourceName = request.ResourceName,
                UserName = request.UserName,
                Justification = request.Justification,
                Status = AccessRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                PolicyId = request.PolicyId,
                RuleId = request.RuleId
            };

            _context.AccessRequests.Add(accessRequest);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created access request {RequestId} for {ResourceType}: {ResourceIdentifier}", 
                accessRequest.RequestId, accessRequest.ResourceType, accessRequest.ResourceIdentifier);

            return await MapToDTO(accessRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create access request");
            throw;
        }
    }

    /// <summary>
    /// Gets all access requests for a tenant
    /// </summary>
    public async Task<List<AccessRequestDTO>> GetAccessRequestsByTenantAsync(Guid tenantId, AccessRequestStatus? status = null)
    {
        try
        {
            var query = _context.AccessRequests
                .Include(r => r.Agent)
                .Where(r => r.TenantId == tenantId);

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            var requests = await query
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            return requests.Select(r => MapToDTOSync(r)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get access requests for tenant {TenantId}", tenantId);
            return new List<AccessRequestDTO>();
        }
    }

    /// <summary>
    /// Gets pending access requests for a specific agent
    /// </summary>
    public async Task<List<AccessRequestDTO>> GetPendingRequestsByAgentAsync(Guid agentId)
    {
        try
        {
            var requests = await _context.AccessRequests
                .Include(r => r.Agent)
                .Where(r => r.AgentId == agentId && r.Status == AccessRequestStatus.Pending)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            return requests.Select(r => MapToDTOSync(r)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending requests for agent {AgentId}", agentId);
            return new List<AccessRequestDTO>();
        }
    }

    /// <summary>
    /// Gets a specific access request by ID
    /// </summary>
    public async Task<AccessRequestDTO?> GetAccessRequestByIdAsync(Guid requestId)
    {
        try
        {
            var request = await _context.AccessRequests
                .Include(r => r.Agent)
                .FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (request == null)
            {
                _logger.LogWarning("Access request {RequestId} not found", requestId);
                return null;
            }

            return MapToDTOSync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get access request {RequestId}", requestId);
            return null;
        }
    }

    /// <summary>
    /// Reviews an access request (approve or deny)
    /// </summary>
    public async Task<AccessRequestDTO?> ReviewAccessRequestAsync(ReviewAccessRequestDTO review)
    {
        try
        {
            var request = await _context.AccessRequests
                .Include(r => r.Agent)
                .FirstOrDefaultAsync(r => r.RequestId == review.RequestId);

            if (request == null)
            {
                _logger.LogWarning("Access request {RequestId} not found", review.RequestId);
                return null;
            }

            if (request.Status != AccessRequestStatus.Pending)
            {
                _logger.LogWarning("Access request {RequestId} is not pending (status: {Status})", 
                    review.RequestId, request.Status);
                return null;
            }

            request.Status = review.Approved ? AccessRequestStatus.Approved : AccessRequestStatus.Denied;
            request.ReviewedBy = review.ReviewedBy;
            request.ReviewedAt = DateTime.UtcNow;
            request.ReviewComments = review.ReviewComments;

            // If approved, create an approval record
            if (review.Approved)
            {
                var approval = new AccessApproval
                {
                    ApprovalId = Guid.NewGuid(),
                    RequestId = request.RequestId,
                    AgentId = request.AgentId,
                    TenantId = request.TenantId,
                    ResourceType = request.ResourceType,
                    ResourceIdentifier = request.ResourceIdentifier,
                    ApprovedAt = DateTime.UtcNow,
                    ExpiresAt = request.ResourceType.Equals("Executable", StringComparison.OrdinalIgnoreCase) 
                        ? DateTime.UtcNow.AddHours(1) 
                        : null,
                    IsActive = true
                };

                _context.AccessApprovals.Add(approval);

                _logger.LogInformation("Created approval {ApprovalId} for request {RequestId}, expires at {ExpiresAt}", 
                    approval.ApprovalId, request.RequestId, approval.ExpiresAt?.ToString() ?? "never");
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Reviewed access request {RequestId}: {Status}", 
                review.RequestId, request.Status);

            return MapToDTOSync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to review access request {RequestId}", review.RequestId);
            throw;
        }
    }

    /// <summary>
    /// Checks if a resource has an active approval
    /// </summary>
    public async Task<AccessApprovalResponseDTO> CheckApprovalAsync(AccessApprovalCheckDTO check)
    {
        try
        {
            // Clean up expired approvals first
            await CleanupExpiredApprovalsAsync();

            var approval = await _context.AccessApprovals
                .FirstOrDefaultAsync(a => 
                    a.AgentId == check.AgentId &&
                    a.ResourceIdentifier == check.ResourceIdentifier &&
                    a.IsActive &&
                    (a.ExpiresAt == null || a.ExpiresAt > DateTime.UtcNow));

            if (approval != null)
            {
                return new AccessApprovalResponseDTO
                {
                    IsApproved = true,
                    ApprovalId = approval.ApprovalId,
                    ExpiresAt = approval.ExpiresAt
                };
            }

            return new AccessApprovalResponseDTO
            {
                IsApproved = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check approval for {ResourceType}: {ResourceIdentifier}", 
                check.ResourceType, check.ResourceIdentifier);
            return new AccessApprovalResponseDTO { IsApproved = false };
        }
    }

    /// <summary>
    /// Cleans up expired approvals
    /// </summary>
    private async Task CleanupExpiredApprovalsAsync()
    {
        try
        {
            var expiredApprovals = await _context.AccessApprovals
                .Where(a => a.IsActive && a.ExpiresAt != null && a.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync();

            foreach (var approval in expiredApprovals)
            {
                approval.IsActive = false;
                _logger.LogInformation("Deactivated expired approval {ApprovalId}", approval.ApprovalId);
            }

            if (expiredApprovals.Any())
            {
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired approvals");
        }
    }

    private async Task<AccessRequestDTO> MapToDTO(AccessRequest request)
    {
        var agent = request.Agent ?? await _context.Agents.FindAsync(request.AgentId);
        
        return new AccessRequestDTO
        {
            RequestId = request.RequestId,
            AgentId = request.AgentId,
            TenantId = request.TenantId,
            MachineName = agent?.MachineName ?? "Unknown",
            ResourceType = request.ResourceType,
            ResourceIdentifier = request.ResourceIdentifier,
            ResourceName = request.ResourceName,
            UserName = request.UserName,
            Justification = request.Justification,
            Status = request.Status,
            RequestedAt = request.RequestedAt,
            ReviewedBy = request.ReviewedBy,
            ReviewedAt = request.ReviewedAt,
            ReviewComments = request.ReviewComments,
            PolicyId = request.PolicyId,
            RuleId = request.RuleId
        };
    }

    private AccessRequestDTO MapToDTOSync(AccessRequest request)
    {
        return new AccessRequestDTO
        {
            RequestId = request.RequestId,
            AgentId = request.AgentId,
            TenantId = request.TenantId,
            MachineName = request.Agent?.MachineName ?? "Unknown",
            ResourceType = request.ResourceType,
            ResourceIdentifier = request.ResourceIdentifier,
            ResourceName = request.ResourceName,
            UserName = request.UserName,
            Justification = request.Justification,
            Status = request.Status,
            RequestedAt = request.RequestedAt,
            ReviewedBy = request.ReviewedBy,
            ReviewedAt = request.ReviewedAt,
            ReviewComments = request.ReviewComments,
            PolicyId = request.PolicyId,
            RuleId = request.RuleId
        };
    }
}

