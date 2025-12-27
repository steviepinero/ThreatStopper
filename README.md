# 🛡️ ThreatStopper

An enterprise-grade endpoint protection platform designed to replace complex Group Policy configurations with an intuitive, cloud-based management system. ThreatStopper provides comprehensive application control, URL blocking, security monitoring, and centralized policy management for Windows environments.

## 🚀 Features

### Core Security Features
- **Application Control**: Block unauthorized software installations using multi-layered detection
- **URL/Domain Blocking**: Block malicious websites and domains at the network level
- **Process Monitoring**: Real-time monitoring of process creation via WMI
- **File System Monitoring**: Track changes to protected directories (Program Files, System32)
- **Policy Engine**: Support for both whitelist and blacklist modes
- **Certificate Validation**: Verify digital signatures and publishers
- **Hash-based Rules**: Control applications by SHA-256 file hash
- **Path-based Rules**: Block/allow applications by file path or patterns

### Management Features
- **Cloud-based Management**: Centralized control via REST API
- **Real-time Dashboard**: Monitor agents, blocked events, and system health
- **Audit Logging**: Comprehensive logging of all security events
- **Multi-tenancy**: Support for multiple organizations
- **Agent Health Monitoring**: Track online/offline status with heartbeats
- **Policy Assignment**: Flexible policy-to-agent assignment system

  <img width="1654" height="872" alt="image" src="https://github.com/user-attachments/assets/156a52e8-34fe-48b7-96a4-8cd76126627e" />

<img width="1614" height="774" alt="image" src="https://github.com/user-attachments/assets/572d0190-446f-4ce8-90e2-520d4bb32cf9" />

<img width="975" height="568" alt="image" src="https://github.com/user-attachments/assets/53aaf71a-010b-4432-9fc6-ee6c0c6a6a09" />

<img width="1546" height="1023" alt="image" src="https://github.com/user-attachments/assets/3b70d6ff-b2fe-41d9-9494-5be670e1d09e" />



## 📁 Project Structure

```
WindowsSecurityPlatform/
├── src/
│   ├── Shared/                          # Shared libraries
│   │   ├── Shared.Models/              # DTOs and enums
│   │   └── Shared.Security/            # Encryption, hashing, API keys
│   │
│   ├── WindowsSecurityAgent/           # Windows Agent (runs on client machines)
│   │   ├── WindowsSecurityAgent.Service/   # Windows Service host
│   │   └── WindowsSecurityAgent.Core/      # Core functionality
│   │       ├── Monitoring/             # Process and file system monitors
│   │       ├── PolicyEngine/           # Policy enforcement
│   │       ├── Communication/          # Cloud API client
│   │       └── Utilities/              # Helpers (installer detection, etc.)
│   │
│   ├── ManagementAPI/                  # Cloud Management API
│   │   ├── ManagementAPI.WebApi/      # ASP.NET Core Web API
│   │   ├── ManagementAPI.Core/        # Business logic and services
│   │   └── ManagementAPI.Data/        # Entity Framework Core
│   │
│   └── admin-portal/                   # React Admin Portal
│       ├── src/
│       │   ├── components/            # React components
│       │   └── services/              # API client
│       └── public/
│
└── README.md
```

## 🛠️ Technology Stack

### Backend (.NET)
- **.NET 10.0**: Latest .NET framework
- **ASP.NET Core**: Web API framework
- **Entity Framework Core**: ORM for database access
- **PostgreSQL**: Primary database
- **System.Management**: WMI for process monitoring
- **JWT Authentication**: Secure API authentication

### Frontend (React)
- **React 18**: UI framework
- **TypeScript**: Type-safe JavaScript
- **React Router**: Client-side routing
- **Axios**: HTTP client
- **Recharts**: Data visualization

### Security
- **AES-256 Encryption**: Policy cache encryption
- **SHA-256 Hashing**: File integrity verification
- **JWT Tokens**: API authentication
- **Certificate Validation**: Digital signature verification

## 🚦 Getting Started

### Prerequisites

1. **.NET 10.0 SDK**
   - Download from: https://dotnet.microsoft.com/download

2. **Node.js 18+** (for admin portal)
   - Download from: https://nodejs.org/

3. **PostgreSQL 14+**
   - Download from: https://www.postgresql.org/download/

### Database Setup

1. Install PostgreSQL and create a database:
```sql
CREATE DATABASE WindowsSecurityPlatform;
```

2. Update the connection string in `src/ManagementAPI/ManagementAPI.WebApi/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=WindowsSecurityPlatform;Username=postgres;Password=your_password"
  }
}
```

3. Run Entity Framework migrations:
```bash
cd src/ManagementAPI/ManagementAPI.Data
dotnet ef migrations add InitialCreate --startup-project ../ManagementAPI.WebApi
dotnet ef database update --startup-project ../ManagementAPI.WebApi
```

### Running the Management API

```bash
cd src/ManagementAPI/ManagementAPI.WebApi
dotnet run
```

The API will be available at `https://localhost:5001`

### Running the Admin Portal

```bash
cd src/admin-portal
npm install
npm start
```

The portal will be available at `http://localhost:3000`

### Installing the Windows Agent

1. Build the agent:
```bash
cd src/WindowsSecurityAgent/WindowsSecurityAgent.Service
dotnet publish -c Release -r win-x64 --self-contained
```

2. Configure the agent by editing `appsettings.json`:
```json
{
  "CloudApi": {
    "BaseUrl": "https://your-api-url.com"
  },
  "Agent": {
    "AgentId": "generate-new-guid",
    "ApiKey": "your-agent-api-key",
    "EncryptionKey": "generate-with-EncryptionHelper.GenerateKey()"
  }
}
```

3. Install as a Windows Service (requires Administrator):
```bash
sc create "WindowsSecurityAgent" binPath= "C:\Path\To\WindowsSecurityAgent.Service.exe"
sc start WindowsSecurityAgent
```

## 📊 Architecture Overview

### Agent Architecture

```
┌─────────────────────────────────────┐
│     Windows Security Agent          │
├─────────────────────────────────────┤
│  ┌─────────────┐  ┌──────────────┐ │
│  │   Process   │  │ File System  │ │
│  │  Monitor    │  │   Monitor    │ │
│  │   (WMI)     │  │ (FSWatcher)  │ │
│  └─────┬───────┘  └──────┬───────┘ │
│        │                 │          │
│        └────────┬────────┘          │
│                 │                   │
│        ┌────────▼────────┐          │
│        │ Policy Enforcer │          │
│        └────────┬────────┘          │
│                 │                   │
│   ┌─────────────▼──────────────┐   │
│   │   Cloud Communication      │   │
│   │  (Policy Sync, Heartbeat,  │   │
│   │   Audit Log Reporting)     │   │
│   └────────────────────────────┘   │
└─────────────────────────────────────┘
                 │
                 │ HTTPS
                 ▼
        ┌──────────────────┐
        │  Management API  │
        └──────────────────┘
```

### Policy Evaluation Flow

```
Process Created
     │
     ▼
Is Installer? ──No──> [Ignore]
     │
     Yes
     ▼
Load Policies
     │
     ▼
Match Rules (Hash, Cert, Path, Publisher)
     │
     ├─Matched─> Apply Action (Block/Allow/Alert)
     │
     └─No Match─> Apply Default (Whitelist: Block, Blacklist: Allow)
          │
          ▼
     Record Audit Log
          │
          ▼
     Report to Cloud
```

## 🔒 Security Considerations

### Agent Security
- Runs with SYSTEM privileges for maximum protection
- Encrypted policy cache using AES-256
- Signed communication with cloud API
- Self-protection mechanisms (monitor own processes)
- Secure credential storage

### API Security
- JWT-based authentication
- API key validation for agents
- Multi-tenancy isolation (row-level security)
- Input validation and sanitization
- HTTPS only communication
- Rate limiting (recommended for production)

### Database Security
- Encrypted connections
- Hashed API keys (never store plaintext)
- Encrypted sensitive fields
- Regular backups
- Audit trail for all changes

## 🧪 Testing

### Unit Tests
```bash
cd src/WindowsSecurityAgent/WindowsSecurityAgent.Tests
dotnet test
```

### Integration Tests
```bash
cd src/ManagementAPI/ManagementAPI.Tests
dotnet test
```

## 📝 Creating Your First Policy

### Via API

```bash
curl -X POST https://localhost:5001/api/policies \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Block Unauthorized Installers",
    "description": "Block all installation attempts except approved software",
    "mode": 0,
    "isActive": true,
    "priority": 100,
    "rules": [
      {
        "ruleType": 3,
        "criteria": "Microsoft Corporation",
        "action": 0,
        "description": "Allow Microsoft signed applications"
      },
      {
        "ruleType": 4,
        "criteria": "*.msi",
        "action": 1,
        "description": "Block all MSI installers"
      }
    ]
  }'
```

### Policy Modes

- **Whitelist Mode (0)**: Block everything by default, only allow what's explicitly permitted
- **Blacklist Mode (1)**: Allow everything by default, only block what's explicitly denied

### Rule Types

- **FileHash (0)**: Match by SHA-256 hash
- **Certificate (1)**: Match by certificate thumbprint
- **Path (2)**: Match by file path (supports wildcards)
- **Publisher (3)**: Match by publisher name
- **FileName (4)**: Match by filename (supports wildcards)

### Actions

- **Allow (0)**: Allow the operation
- **Block (1)**: Block the operation
- **Alert (2)**: Allow but generate alert

## 🚀 Deployment

### Production Checklist

#### Management API
- [ ] Change JWT secret key
- [ ] Configure production database connection
- [ ] Enable HTTPS with valid certificate
- [ ] Configure CORS for production domain
- [ ] Set up logging (Application Insights, ELK, etc.)
- [ ] Enable rate limiting
- [ ] Configure automatic backups

#### Windows Agent
- [ ] Create MSI installer
- [ ] Sign the executable
- [ ] Configure auto-update mechanism
- [ ] Set up centralized logging
- [ ] Test on various Windows versions
- [ ] Create deployment GPO

#### Admin Portal
- [ ] Build for production: `npm run build`
- [ ] Deploy to CDN or static hosting
- [ ] Configure production API URL
- [ ] Enable HTTPS
- [ ] Set up monitoring

### Recommended Infrastructure

#### Azure
- **App Service**: Host Management API
- **Static Web Apps**: Host Admin Portal
- **Azure Database for PostgreSQL**: Database
- **Application Insights**: Monitoring
- **Key Vault**: Secret management

#### AWS
- **Elastic Beanstalk**: Host Management API
- **S3 + CloudFront**: Host Admin Portal
- **RDS PostgreSQL**: Database
- **CloudWatch**: Monitoring
- **Secrets Manager**: Secret management

## 📈 Monitoring

### Key Metrics to Monitor

- **Agent Health**: Online/offline status, heartbeat intervals
- **Block Rate**: Number of blocked installations per hour/day
- **False Positives**: Legitimate software incorrectly blocked
- **Policy Sync Success**: Percentage of successful policy syncs
- **API Response Times**: P50, P95, P99 latency
- **Database Performance**: Query times, connection pool usage

### Recommended Alerts

- Agent offline for > 1 hour
- Heartbeat failures > 10%
- Block rate spike (>3x normal)
- API error rate > 1%
- Database connection failures

## 🤝 Contributing

This is a custom enterprise security solution. For modifications:

1. Test thoroughly in a non-production environment
2. Review security implications
3. Update documentation
4. Follow coding standards

## 📄 License

MIT

## ⚠️ Important Notes

### System Requirements
- Windows 10/11 or Windows Server 2016+
- Administrator privileges for agent installation
- .NET 10.0 Runtime
- Minimum 2GB RAM, 1GB disk space

### Known Limitations
- Currently Windows-only (agent)
- Requires internet connectivity for cloud features
- May impact system performance on older hardware
- Some installers may bypass detection

### Roadmap
- [ ] Offline mode with local policy management
- [ ] USB device control
- [ ] Registry protection
- [ ] Network firewall rules
- [ ] Machine learning for installer detection
- [ ] Mobile app for management
- [ ] Support for Linux and macOS agents

## 📞 Support

For issues or questions, contact your IT security team or refer to the internal wiki.

---

**Built with ❤️ for Windows Security**
