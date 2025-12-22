# Windows Security Platform - Quick Setup Guide

This guide will help you get the platform up and running in under 30 minutes.

## Prerequisites Installation

### 1. Install .NET 10.0 SDK ✅
Already installed! Verify:
```bash
dotnet --version
# Should show: 10.0.101 or higher
```

### 2. Install PostgreSQL

**Download**: https://www.postgresql.org/download/windows/

**During Installation**:
- Set password for postgres user (remember this!)
- Default port: 5432
- Install pgAdmin (recommended for database management)

**After Installation**:
```bash
# Test PostgreSQL is running
psql -U postgres -c "SELECT version();"
```

### 3. Install Node.js (Already Installed)
You have Node.js v22.19.0 ✅

## Step-by-Step Setup

### Step 1: Create Database

Open pgAdmin or psql:
```sql
CREATE DATABASE WindowsSecurityPlatform;
```

### Step 2: Configure Connection String

Edit `src/ManagementAPI/ManagementAPI.WebApi/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=WindowsSecurityPlatform;Username=postgres;Password=YOUR_PASSWORD"
  }
}
```

### Step 3: Run Database Migrations

```bash
cd C:\Users\stevi\WindowsSecurityPlatform

# Add EF Core tools if not already installed
dotnet tool install --global dotnet-ef

# Create and apply migrations
cd src\ManagementAPI\ManagementAPI.Data
dotnet ef migrations add InitialCreate --startup-project ..\ManagementAPI.WebApi
dotnet ef database update --startup-project ..\ManagementAPI.WebApi
```

### Step 4: Start the Management API

Open a new terminal:
```bash
cd C:\Users\stevi\WindowsSecurityPlatform\src\ManagementAPI\ManagementAPI.WebApi
dotnet run
```

The API will start at: `https://localhost:5001`

Leave this terminal running!

### Step 5: Start the Admin Portal

Open another terminal:
```bash
cd C:\Users\stevi\WindowsSecurityPlatform\src\admin-portal
npm start
```

The portal will open at: `http://localhost:3000`

Leave this terminal running!

### Step 6: Create Your First Organization

The database is empty, so let's seed it with a test organization.

Create a file `SeedData.sql`:
```sql
-- Generate a test organization
INSERT INTO "Organizations" ("TenantId", "Name", "ApiKeyHash", "SubscriptionTier", "CreatedAt", "IsActive")
VALUES 
  ('11111111-1111-1111-1111-111111111111', 
   'Test Organization', 
   -- This is the hash of 'test-api-key-12345'
   'bb8b564e4b1fcfe034bb00f1a2fb71c9c8be65b16ef1e94c90f9b5e3ebf6f93e',
   'Enterprise',
   NOW(),
   true);
```

Run it:
```bash
psql -U postgres -d WindowsSecurityPlatform -f SeedData.sql
```

### Step 7: Update Admin Portal to Use Test Tenant

Edit `src/admin-portal/src/services/api.ts`:

Change line 9:
```typescript
this.tenantId = '11111111-1111-1111-1111-111111111111';
```

Restart the admin portal (Ctrl+C and `npm start`)

### Step 8: Verify Everything Works

1. Open `http://localhost:3000`
2. You should see the dashboard (it will be empty initially)
3. Navigate to "Agents" - it will show no agents yet

## Testing the Agent (Optional)

### Configure the Agent

Edit `src/WindowsSecurityAgent/WindowsSecurityAgent.Service/appsettings.json`:
```json
{
  "CloudApi": {
    "BaseUrl": "https://localhost:5001"
  },
  "Agent": {
    "AgentId": "",
    "ApiKey": "",
    "EncryptionKey": "",
    "CacheDirectory": "C:\\ProgramData\\WindowsSecurityAgent"
  }
}
```

### Run the Agent (Development Mode)

```bash
cd C:\Users\stevi\WindowsSecurityPlatform\src\WindowsSecurityAgent\WindowsSecurityAgent.Service
dotnet run
```

**Note**: The agent requires administrator privileges to monitor processes!

Run PowerShell as Administrator:
```powershell
cd C:\Users\stevi\WindowsSecurityPlatform\src\WindowsSecurityAgent\WindowsSecurityAgent.Service
dotnet run
```

## Creating Your First Policy

### Via Admin Portal (Future Feature)
The policy editor is planned for the next phase.

### Via API (Current Method)

Using PowerShell:
```powershell
$policy = @{
    name = "Block Unauthorized Installers"
    description = "Whitelist mode - only allow known publishers"
    mode = 0
    isActive = $true
    priority = 100
    rules = @(
        @{
            ruleType = 3
            criteria = "Microsoft Corporation"
            action = 0
            description = "Allow Microsoft"
        }
    )
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:5001/api/policies?tenantId=11111111-1111-1111-1111-111111111111" `
    -Method Post `
    -Body $policy `
    -ContentType "application/json" `
    -SkipCertificateCheck
```

## Common Issues

### Issue: Cannot connect to database
**Solution**: Make sure PostgreSQL service is running:
```bash
# Check service status
sc query postgresql-x64-14

# Start if needed
net start postgresql-x64-14
```

### Issue: Agent won't start - Access Denied
**Solution**: Run as Administrator:
```powershell
# Run PowerShell as Admin, then:
cd C:\Users\stevi\WindowsSecurityPlatform\src\WindowsSecurityAgent\WindowsSecurityAgent.Service
dotnet run
```

### Issue: API SSL certificate error
**Solution**: Trust the development certificate:
```bash
dotnet dev-certs https --trust
```

### Issue: Admin portal can't connect to API
**Solution**: 
1. Check CORS settings in API Program.cs
2. Make sure API is running on port 5001
3. Check browser console for errors

## Next Steps

Now that everything is running:

1. **Explore the Dashboard**: View agent statistics
2. **Create Policies**: Define your security rules
3. **Deploy Agents**: Install on test machines
4. **Monitor Logs**: Watch for blocked installations
5. **Refine Policies**: Adjust based on legitimate blocks

## Production Deployment

For production deployment, see the main README.md "Deployment" section.

Key differences from development:
- Use production database
- Configure proper SSL certificates
- Set secure JWT keys
- Build optimized releases
- Install agent as Windows Service
- Set up monitoring and alerts

---

**Need Help?** Check the README.md for detailed documentation or contact your IT team.

**Happy Securing! 🛡️**
