using ManagementAPI.Core.Services;
using ManagementAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure JSON serialization to use camelCase (matches frontend)
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Configure Database (supports SQLite for testing, PostgreSQL for production)
var databaseProvider = builder.Configuration["Database:Provider"] ?? "Sqlite";

if (databaseProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Host=localhost;Database=WindowsSecurityPlatform;Username=postgres;Password=postgres";
    
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    // Use SQLite for testing/development
    var sqliteConnection = builder.Configuration.GetConnectionString("SqliteConnection") 
        ?? "Data Source=windowssecurity.db";
    
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(sqliteConnection));
}

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "your-secret-key-here-change-in-production-minimum-32-characters";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "WindowsSecurityPlatform";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "WindowsSecurityPlatform";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

// Register application services
builder.Services.AddScoped<AgentService>();
builder.Services.AddScoped<PolicyService>();
builder.Services.AddScoped<AuditLogService>();

var app = builder.Build();

// Initialize database and seed test data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Create database and apply migrations
    context.Database.EnsureCreated();
    
    // Seed test data if database is empty
    if (!context.Organizations.Any())
    {
        var testOrg = new ManagementAPI.Data.Entities.Organization
        {
            TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Test Organization",
            ApiKeyHash = "bb8b564e4b1fcfe034bb00f1a2fb71c9c8be65b16ef1e94c90f9b5e3ebf6f93e", // Hash of 'test-api-key-12345'
            SubscriptionTier = "Enterprise",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        context.Organizations.Add(testOrg);
        
        // Add a test agent
        var testAgent = new ManagementAPI.Data.Entities.Agent
        {
            AgentId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            TenantId = testOrg.TenantId,
            MachineName = "TEST-PC-001",
            OperatingSystem = "Windows 11 Pro",
            AgentVersion = "1.0.0",
            ApiKeyHash = "test-hash",
            Status = Shared.Models.Enums.AgentStatus.Online,
            LastHeartbeat = DateTime.UtcNow,
            RegisteredAt = DateTime.UtcNow.AddDays(-7)
        };
        context.Agents.Add(testAgent);
        
        // Add a test policy
        var testPolicy = new ManagementAPI.Data.Entities.Policy
        {
            PolicyId = Guid.NewGuid(),
            TenantId = testOrg.TenantId,
            Name = "Block Unauthorized Installers",
            Description = "Whitelist mode - only allow known publishers",
            Mode = Shared.Models.Enums.PolicyMode.Whitelist,
            IsActive = true,
            Priority = 100,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Policies.Add(testPolicy);
        
        // Add some test audit logs
        for (int i = 0; i < 10; i++)
        {
            var auditLog = new ManagementAPI.Data.Entities.AuditLog
            {
                LogId = Guid.NewGuid(),
                AgentId = testAgent.AgentId,
                EventType = i % 3 == 0 ? Shared.Models.Enums.EventType.InstallationBlocked : Shared.Models.Enums.EventType.InstallationAllowed,
                ProcessName = $"installer_{i}.exe",
                ProcessPath = $"C:\\Temp\\installer_{i}.exe",
                Blocked = i % 3 == 0,
                Timestamp = DateTime.UtcNow.AddHours(-i),
                UserName = "TESTUSER",
                Details = $"Test installation attempt {i}"
            };
            context.AuditLogs.Add(auditLog);
        }
        
        context.SaveChanges();
        
        Console.WriteLine("✓ Database created and seeded with test data");
        Console.WriteLine($"  Test Org ID: {testOrg.TenantId}");
        Console.WriteLine($"  Test Agent ID: {testAgent.AgentId}");
    }
    
    // Add FOOTBALLHEAD agent if it doesn't exist
    var footballheadId = Guid.Parse("e432ffa3-d4ae-434e-b128-98290d052cfa");
    if (!context.Agents.Any(a => a.AgentId == footballheadId))
    {
        var footballheadAgent = new ManagementAPI.Data.Entities.Agent
        {
            AgentId = footballheadId,
            TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            MachineName = "FOOTBALLHEAD",
            OperatingSystem = "Windows 11",
            AgentVersion = "1.0.0",
            ApiKeyHash = "test-hash",
            Status = Shared.Models.Enums.AgentStatus.Online,
            LastHeartbeat = DateTime.UtcNow,
            RegisteredAt = DateTime.UtcNow
        };
        context.Agents.Add(footballheadAgent);
        context.SaveChanges();
        
        Console.WriteLine("✓ Added FOOTBALLHEAD agent");
        Console.WriteLine($"  Agent ID: {footballheadAgent.AgentId}");
    }
    
    // Add agent 675869b0-1804-486b-b1e7-b3182ec7e5b1 if it doesn't exist
    var agentId = Guid.Parse("675869b0-1804-486b-b1e7-b3182ec7e5b1");
    if (!context.Agents.Any(a => a.AgentId == agentId))
    {
        var agent = new ManagementAPI.Data.Entities.Agent
        {
            AgentId = agentId,
            TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            MachineName = Environment.MachineName,
            OperatingSystem = "Windows 11",
            AgentVersion = "1.0.0",
            ApiKeyHash = "test-hash",
            Status = Shared.Models.Enums.AgentStatus.Online,
            LastHeartbeat = DateTime.UtcNow,
            RegisteredAt = DateTime.UtcNow
        };
        context.Agents.Add(agent);
        context.SaveChanges();
        
        Console.WriteLine($"✓ Added agent {agent.MachineName}");
        Console.WriteLine($"  Agent ID: {agent.AgentId}");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
