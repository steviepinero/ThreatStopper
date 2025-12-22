namespace ManagementAPI.Data.Entities;

/// <summary>
/// Represents a tenant organization
/// </summary>
public class Organization
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApiKeyHash { get; set; } = string.Empty;
    public string SubscriptionTier { get; set; } = "Free";
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }

    // Navigation properties
    public ICollection<Agent> Agents { get; set; } = new List<Agent>();
    public ICollection<Policy> Policies { get; set; } = new List<Policy>();
    public ICollection<User> Users { get; set; } = new List<User>();
}
