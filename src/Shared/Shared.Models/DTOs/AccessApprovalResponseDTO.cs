namespace Shared.Models.DTOs;

/// <summary>
/// DTO for access approval response
/// </summary>
public class AccessApprovalResponseDTO
{
    public bool IsApproved { get; set; }
    public Guid? ApprovalId { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

