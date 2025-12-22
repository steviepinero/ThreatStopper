namespace ManagementAPI.Data.Entities;

/// <summary>
/// Represents a user with access to the management portal
/// </summary>
public class User
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Viewer"; // Admin, PolicyManager, Viewer
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
    public bool IsActive { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
}
