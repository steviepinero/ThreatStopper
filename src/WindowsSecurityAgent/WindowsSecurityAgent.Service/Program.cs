using WindowsSecurityAgent.Service;
using WindowsSecurityAgent.Core.Monitoring;
using WindowsSecurityAgent.Core.PolicyEngine;
using WindowsSecurityAgent.Core.Communication;
using Shared.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddEventLog(settings =>
{
    settings.SourceName = "ThreatStopper";
});

// Read configuration
var config = builder.Configuration;
var apiBaseUrl = config["CloudApi:BaseUrl"] ?? "https://localhost:5001";

// Handle AgentId - generate new if empty or invalid
var agentIdStr = config["Agent:AgentId"] ?? string.Empty;
Guid agentId;
if (string.IsNullOrWhiteSpace(agentIdStr) || !Guid.TryParse(agentIdStr, out agentId))
{
    agentId = Guid.NewGuid();
    // Log warning after logging is configured
    Console.WriteLine($"[WARNING] AgentId not configured or invalid, generated new ID: {agentId}");
}

var apiKey = config["Agent:ApiKey"] ?? ApiKeyGenerator.GenerateApiKey();
var encryptionKey = config["Agent:EncryptionKey"] ?? EncryptionHelper.GenerateKey();
var cacheDirectory = config["Agent:CacheDirectory"] ?? Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
    "WindowsSecurityAgent");

// Register services
builder.Services.AddSingleton(sp => 
    new PolicyCache(
        sp.GetRequiredService<ILogger<PolicyCache>>(),
        cacheDirectory,
        encryptionKey));

builder.Services.AddSingleton(sp => 
    new CloudClient(
        sp.GetRequiredService<ILogger<CloudClient>>(),
        apiBaseUrl,
        apiKey,
        agentId));

// Register URL blocking services
builder.Services.AddSingleton<UrlBlocker>();
builder.Services.AddSingleton(sp =>
    new UrlPolicySyncService(
        sp.GetRequiredService<ILogger<UrlPolicySyncService>>(),
        sp.GetRequiredService<PolicyCache>(),
        sp.GetRequiredService<UrlBlocker>(),
        600)); // Sync every 10 minutes

builder.Services.AddSingleton(sp => 
    new PolicySyncService(
        sp.GetRequiredService<ILogger<PolicySyncService>>(),
        sp.GetRequiredService<CloudClient>(),
        sp.GetRequiredService<PolicyCache>(),
        sp.GetRequiredService<UrlBlocker>()));
builder.Services.AddSingleton(sp => 
    new AuditReporter(
        sp.GetRequiredService<ILogger<AuditReporter>>(),
        sp.GetRequiredService<CloudClient>(),
        agentId));

builder.Services.AddSingleton<PolicyEnforcer>();
builder.Services.AddSingleton<ProcessMonitor>();
builder.Services.AddSingleton<FileSystemMonitor>();

// Register the main worker service
builder.Services.AddHostedService<AgentWorker>();

// Configure Windows Service
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "WindowsSecurityAgent";
});

var host = builder.Build();
host.Run();
