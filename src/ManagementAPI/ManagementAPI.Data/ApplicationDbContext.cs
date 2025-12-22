using ManagementAPI.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ManagementAPI.Data;

/// <summary>
/// Entity Framework Core database context
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Agent> Agents { get; set; }
    public DbSet<Policy> Policies { get; set; }
    public DbSet<PolicyRule> PolicyRules { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<AgentPolicyAssignment> AgentPolicyAssignments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Organization configuration
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.TenantId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ApiKeyHash).IsRequired().HasMaxLength(500);
            entity.HasIndex(e => e.Name);
        });

        // Agent configuration
        modelBuilder.Entity<Agent>(entity =>
        {
            entity.HasKey(e => e.AgentId);
            entity.Property(e => e.MachineName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.OperatingSystem).HasMaxLength(100);
            entity.Property(e => e.AgentVersion).HasMaxLength(50);
            entity.Property(e => e.ApiKeyHash).IsRequired().HasMaxLength(500);
            
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Agents)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.LastHeartbeat);
        });

        // Policy configuration
        modelBuilder.Entity<Policy>(entity =>
        {
            entity.HasKey(e => e.PolicyId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Policies)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.TenantId, e.Priority });
        });

        // PolicyRule configuration
        modelBuilder.Entity<PolicyRule>(entity =>
        {
            entity.HasKey(e => e.RuleId);
            entity.Property(e => e.Criteria).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Description).HasMaxLength(500);
            
            entity.HasOne(e => e.Policy)
                .WithMany(p => p.Rules)
                .HasForeignKey(e => e.PolicyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.PolicyId);
            entity.HasIndex(e => new { e.PolicyId, e.Order });
        });

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.LogId);
            entity.Property(e => e.ProcessName).HasMaxLength(500);
            entity.Property(e => e.ProcessPath).HasMaxLength(1000);
            entity.Property(e => e.FileHash).HasMaxLength(64);
            entity.Property(e => e.Publisher).HasMaxLength(500);
            entity.Property(e => e.UserName).HasMaxLength(200);
            entity.Property(e => e.Details).HasMaxLength(2000);
            
            entity.HasOne(e => e.Agent)
                .WithMany(a => a.AuditLogs)
                .HasForeignKey(e => e.AgentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.AgentId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.Blocked);
        });

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Users)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.TenantId);
        });

        // AgentPolicyAssignment configuration
        modelBuilder.Entity<AgentPolicyAssignment>(entity =>
        {
            entity.HasKey(e => e.AssignmentId);
            
            entity.HasOne(e => e.Agent)
                .WithMany(a => a.PolicyAssignments)
                .HasForeignKey(e => e.AgentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Policy)
                .WithMany(p => p.AgentAssignments)
                .HasForeignKey(e => e.PolicyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.AgentId);
            entity.HasIndex(e => e.PolicyId);
            entity.HasIndex(e => new { e.AgentId, e.PolicyId }).IsUnique();
        });
    }
}
