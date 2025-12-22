# Windows Security Platform - Complete Deployment Guide

## 📋 Overview

This guide covers the complete deployment of the Windows Security Platform, including:
- Management API (Backend)
- Admin Portal (Frontend)
- Windows Security Agent (Endpoint)

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Enterprise Network                        │
│                                                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │   Client PC  │  │   Client PC  │  │   Client PC  │     │
│  │   + Agent    │  │   + Agent    │  │   + Agent    │     │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘     │
│         │                  │                  │              │
│         └──────────────────┴──────────────────┘              │
│                            │                                 │
└────────────────────────────┼─────────────────────────────────┘
                             │ HTTPS
                             ▼
                  ┌──────────────────────┐
                  │   Management API     │
                  │   (ASP.NET Core)     │
                  └──────────┬───────────┘
                             │
                             ▼
                  ┌──────────────────────┐
                  │   PostgreSQL DB      │
                  └──────────────────────┘

                  ┌──────────────────────┐
                  │   Admin Portal       │
                  │   (React SPA)        │
                  └──────────────────────┘
```

## 🚀 Part 1: Management API Deployment

### Option A: Windows Server (IIS)

#### Prerequisites
- Windows Server 2016+ with IIS
- .NET 10.0 Hosting Bundle
- PostgreSQL 14+

#### Steps

1. **Install .NET Hosting Bundle**
   - Download from: https://dotnet.microsoft.com/download/dotnet/10.0
   - Install: `dotnet-hosting-10.0.x-win.exe`
   - Restart IIS: `iisreset`

2. **Install PostgreSQL**
   - Download from: https://www.postgresql.org/download/windows/
   - Create database:
   ```sql
   CREATE DATABASE WindowsSecurityPlatform;
   ```

3. **Configure Database**
   - Edit `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=WindowsSecurityPlatform;Username=postgres;Password=YourPassword"
     }
   }
   ```

4. **Run Migrations**
   ```powershell
   cd C:\inetpub\wwwroot\ManagementAPI
   dotnet ef database update --project ManagementAPI.Data.dll --startup-project ManagementAPI.WebApi.dll
   ```

5. **Create IIS Application Pool**
   - Open IIS Manager
   - Create new Application Pool: "WindowsSecurityAPI"
   - .NET CLR Version: No Managed Code
   - Managed Pipeline Mode: Integrated

6. **Create IIS Website**
   - Create new Website: "Management API"
   - Physical Path: `C:\inetpub\wwwroot\ManagementAPI`
   - Application Pool: WindowsSecurityAPI
   - Binding: HTTPS, Port 443
   - SSL Certificate: Install valid certificate

7. **Set Permissions**
   ```powershell
   icacls "C:\inetpub\wwwroot\ManagementAPI" /grant "IIS AppPool\WindowsSecurityAPI:(OI)(CI)F" /T
   ```

### Option B: Azure App Service

1. **Create Azure Resources**
   ```bash
   # Create resource group
   az group create --name WindowsSecurityPlatform --location eastus
   
   # Create PostgreSQL server
   az postgres flexible-server create \
     --name winsec-db-server \
     --resource-group WindowsSecurityPlatform \
     --location eastus \
     --admin-user dbadmin \
     --admin-password YourStrongPassword \
     --sku-name Standard_B2s
   
   # Create database
   az postgres flexible-server db create \
     --resource-group WindowsSecurityPlatform \
     --server-name winsec-db-server \
     --database-name WindowsSecurityPlatform
   
   # Create App Service Plan
   az appservice plan create \
     --name WindowsSecurityPlan \
     --resource-group WindowsSecurityPlatform \
     --sku B2 \
     --is-linux
   
   # Create Web App
   az webapp create \
     --name winsec-management-api \
     --resource-group WindowsSecurityPlatform \
     --plan WindowsSecurityPlan \
     --runtime "DOTNET|10.0"
   ```

2. **Configure Connection String**
   ```bash
   az webapp config connection-string set \
     --name winsec-management-api \
     --resource-group WindowsSecurityPlatform \
     --connection-string-type PostgreSQL \
     --settings DefaultConnection="Host=winsec-db-server.postgres.database.azure.com;Database=WindowsSecurityPlatform;Username=dbadmin;Password=YourStrongPassword;SSL Mode=Require"
   ```

3. **Deploy Application**
   ```bash
   cd src/ManagementAPI/ManagementAPI.WebApi
   dotnet publish -c Release -o ./publish
   cd publish
   zip -r ../deploy.zip .
   
   az webapp deployment source config-zip \
     --name winsec-management-api \
     --resource-group WindowsSecurityPlatform \
     --src ../deploy.zip
   ```

### Option C: Docker Container

1. **Create Dockerfile**
   ```dockerfile
   FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
   WORKDIR /app
   EXPOSE 80
   EXPOSE 443
   
   FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
   WORKDIR /src
   COPY ["ManagementAPI.WebApi/ManagementAPI.WebApi.csproj", "ManagementAPI.WebApi/"]
   COPY ["ManagementAPI.Core/ManagementAPI.Core.csproj", "ManagementAPI.Core/"]
   COPY ["ManagementAPI.Data/ManagementAPI.Data.csproj", "ManagementAPI.Data/"]
   RUN dotnet restore "ManagementAPI.WebApi/ManagementAPI.WebApi.csproj"
   COPY . .
   WORKDIR "/src/ManagementAPI.WebApi"
   RUN dotnet build "ManagementAPI.WebApi.csproj" -c Release -o /app/build
   
   FROM build AS publish
   RUN dotnet publish "ManagementAPI.WebApi.csproj" -c Release -o /app/publish
   
   FROM base AS final
   WORKDIR /app
   COPY --from=publish /app/publish .
   ENTRYPOINT ["dotnet", "ManagementAPI.WebApi.dll"]
   ```

2. **Build and Run**
   ```bash
   docker build -t windows-security-api .
   docker run -d -p 5001:443 \
     -e ConnectionStrings__DefaultConnection="Host=db;Database=WindowsSecurityPlatform;Username=postgres;Password=password" \
     --name winsec-api \
     windows-security-api
   ```

## 🌐 Part 2: Admin Portal Deployment

### Option A: IIS Static Website

1. **Build Production Bundle**
   ```bash
   cd src/admin-portal
   npm run build
   ```

2. **Create IIS Website**
   - Copy `build` folder to `C:\inetpub\wwwroot\AdminPortal`
   - Create new Website in IIS
   - Physical Path: `C:\inetpub\wwwroot\AdminPortal`
   - Binding: HTTPS, Port 443
   - Install URL Rewrite module for SPA routing

3. **Configure web.config**
   ```xml
   <?xml version="1.0" encoding="UTF-8"?>
   <configuration>
     <system.webServer>
       <rewrite>
         <rules>
           <rule name="React Routes" stopProcessing="true">
             <match url=".*" />
             <conditions logicalGrouping="MatchAll">
               <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
               <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
             </conditions>
             <action type="Rewrite" url="/" />
           </rule>
         </rules>
       </rewrite>
     </system.webServer>
   </configuration>
   ```

### Option B: Azure Static Web Apps

```bash
# Install Azure Static Web Apps CLI
npm install -g @azure/static-web-apps-cli

# Deploy
cd src/admin-portal
npm run build

az staticwebapp create \
  --name winsec-admin-portal \
  --resource-group WindowsSecurityPlatform \
  --location eastus

# Upload build folder
az staticwebapp upload \
  --name winsec-admin-portal \
  --resource-group WindowsSecurityPlatform \
  --app-location build
```

### Option C: AWS S3 + CloudFront

```bash
# Build
cd src/admin-portal
npm run build

# Create S3 bucket
aws s3 mb s3://winsec-admin-portal

# Upload files
aws s3 sync build/ s3://winsec-admin-portal --acl public-read

# Create CloudFront distribution
aws cloudfront create-distribution \
  --origin-domain-name winsec-admin-portal.s3.amazonaws.com \
  --default-root-object index.html
```

## 🖥️ Part 3: Windows Agent Deployment

See `WindowsSecurityAgent/README.md` for detailed agent deployment instructions.

### Quick Deployment Steps

1. **Configure Agent**
   - Edit `appsettings.json`
   - Set API URL, credentials, encryption key

2. **Deploy to Single Computer**
   ```powershell
   .\Install-Agent.ps1
   ```

3. **Deploy to Multiple Computers**
   ```powershell
   .\Deploy-ToRemoteComputers.ps1 -ComputerListFile "computers.txt"
   ```

4. **Deploy via Group Policy**
   - Copy to network share
   - Create GPO with startup script
   - Link to target OUs

## 🔒 Security Configuration

### SSL/TLS Certificates

**Production:** Use certificates from trusted CA
- Let's Encrypt (free)
- DigiCert, Sectigo, etc.

**Development:** Use self-signed certificates
```powershell
# Trust development certificate
dotnet dev-certs https --trust
```

### API Security

1. **Change JWT Secret**
   - Edit `appsettings.json`
   - Set strong random secret (32+ characters)

2. **Configure CORS**
   - Update allowed origins in `Program.cs`
   - Only allow admin portal domain

3. **Enable Rate Limiting**
   - Add rate limiting middleware
   - Configure per-endpoint limits

### Database Security

1. **Strong Passwords**
   - Use complex passwords for database users
   - Rotate regularly

2. **Network Security**
   - Restrict database access to API server only
   - Use firewall rules

3. **Encrypted Connections**
   - Enable SSL for PostgreSQL connections
   - Add `SSL Mode=Require` to connection string

## 📊 Monitoring & Logging

### Application Insights (Azure)

```bash
az monitor app-insights component create \
  --app winsec-insights \
  --location eastus \
  --resource-group WindowsSecurityPlatform

# Get instrumentation key
az monitor app-insights component show \
  --app winsec-insights \
  --resource-group WindowsSecurityPlatform \
  --query instrumentationKey
```

Add to `appsettings.json`:
```json
{
  "ApplicationInsights": {
    "InstrumentationKey": "your-key-here"
  }
}
```

### ELK Stack (Self-hosted)

1. Install Elasticsearch, Logstash, Kibana
2. Configure Serilog to send logs to Elasticsearch
3. Create Kibana dashboards

### Windows Event Logs

Agents automatically log to Windows Event Log:
```powershell
Get-EventLog -LogName Application -Source WindowsSecurityAgent
```

## 🧪 Testing Deployment

### API Health Check
```powershell
Invoke-RestMethod -Uri "https://your-api-url/health" -Method Get
```

### Database Connection Test
```powershell
psql -h your-db-server -U postgres -d WindowsSecurityPlatform -c "SELECT version();"
```

### Agent Connectivity Test
```powershell
# From agent machine
Test-NetConnection -ComputerName your-api-url.com -Port 443
```

## 📝 Post-Deployment Checklist

- [ ] Management API is accessible via HTTPS
- [ ] Database migrations completed successfully
- [ ] Admin Portal loads and connects to API
- [ ] SSL certificates are valid and trusted
- [ ] CORS configured correctly
- [ ] JWT authentication working
- [ ] Test agent can register and connect
- [ ] Heartbeats appearing in dashboard
- [ ] Policy sync working
- [ ] Audit logs being recorded
- [ ] Monitoring/logging configured
- [ ] Backup strategy implemented
- [ ] Documentation updated with URLs

## 🔄 Backup & Recovery

### Database Backups

**Automated (PostgreSQL)**
```bash
# Create backup script
pg_dump -h localhost -U postgres WindowsSecurityPlatform > backup_$(date +%Y%m%d).sql

# Schedule with cron/Task Scheduler
```

**Azure PostgreSQL**
- Automatic backups enabled by default
- Configure retention period (7-35 days)

### Application Backups

- Version control (Git) for code
- Document configuration changes
- Export policies and settings regularly

## 📞 Support & Troubleshooting

### Common Issues

1. **API not accessible**
   - Check firewall rules
   - Verify SSL certificate
   - Check IIS/service status

2. **Database connection failed**
   - Verify connection string
   - Check PostgreSQL service
   - Test network connectivity

3. **Agents not appearing**
   - Check agent configuration
   - Verify API URL is correct
   - Check agent event logs

### Logs Locations

- **API Logs:** Event Viewer or Application Insights
- **Agent Logs:** Event Viewer → Application → WindowsSecurityAgent
- **IIS Logs:** `C:\inetpub\logs\LogFiles`

---

**For agent-specific deployment, see `WindowsSecurityAgent/README.md`**
**For configuration details, see `WindowsSecurityAgent/DEPLOYMENT_CONFIG.txt`**

