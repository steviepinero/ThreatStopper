# Windows Security Agent - Enterprise Deployment Package

## 📦 Package Contents

This deployment package contains everything needed to deploy the Windows Security Agent to enterprise computers:

- **WindowsSecurityAgent.Service.exe** - Main agent executable (self-contained)
- **appsettings.json** - Configuration file (MUST BE CONFIGURED)
- **Install-Agent.ps1** - Installation script
- **Uninstall-Agent.ps1** - Uninstallation script
- **Deploy-ToRemoteComputers.ps1** - Bulk deployment script
- **DEPLOYMENT_CONFIG.txt** - Detailed configuration guide
- **README.md** - This file

## 🚀 Quick Start

### Prerequisites

1. **Windows 10/11** or **Windows Server 2016+**
2. **Administrator privileges** for installation
3. **Network access** to Management API
4. **PowerShell 5.1+** (included in Windows)

### Step 1: Configure the Agent

**IMPORTANT:** Before deployment, you MUST configure `appsettings.json`

1. Open `appsettings.json` in a text editor
2. Set the following values:

```json
{
  "CloudApi": {
    "BaseUrl": "https://your-api-url.com"  // Your Management API URL
  },
  "Agent": {
    "AgentId": "",           // Generate new GUID or leave empty for auto-registration
    "ApiKey": "",            // Your API key from the admin portal
    "EncryptionKey": ""      // Generate 32-byte base64 key (see below)
  }
}
```

**Generate Encryption Key (PowerShell):**
```powershell
$bytes = New-Object byte[] 32
[Security.Cryptography.RNGCryptoServiceProvider]::Create().GetBytes($bytes)
[Convert]::ToBase64String($bytes)
```

### Step 2: Install on Single Computer

1. Copy the entire folder to the target computer
2. Open PowerShell as Administrator
3. Navigate to the folder
4. Run:
```powershell
.\Install-Agent.ps1
```

The agent will be installed as a Windows Service and started automatically.

### Step 3: Deploy to Multiple Computers

**Option A: Using Computer Names**
```powershell
.\Deploy-ToRemoteComputers.ps1 -ComputerNames "PC001","PC002","PC003"
```

**Option B: Using Computer List File**

Create a text file `computers.txt` with one computer name per line:
```
PC001
PC002
PC003
SERVER01
```

Then run:
```powershell
.\Deploy-ToRemoteComputers.ps1 -ComputerListFile "computers.txt"
```

## 📋 Deployment Methods

### Method 1: Group Policy (Recommended for Active Directory)

1. Copy this folder to a network share (e.g., `\\fileserver\software\WindowsSecurityAgent`)
2. Open Group Policy Management Console
3. Create new GPO: "Deploy Windows Security Agent"
4. Edit GPO → Computer Configuration → Policies → Windows Settings → Scripts → Startup
5. Add script: `\\fileserver\software\WindowsSecurityAgent\Install-Agent.ps1`
6. Link GPO to target OUs
7. Run `gpupdate /force` on target computers or wait for next reboot

### Method 2: SCCM/Configuration Manager

1. Create new Application in SCCM
2. Set installation command: `powershell.exe -ExecutionPolicy Bypass -File ".\Install-Agent.ps1"`
3. Set detection method: Check for service "WindowsSecurityAgent"
4. Deploy to device collections

### Method 3: Microsoft Intune

1. Package folder as .intunewin file
2. Create Win32 app in Intune
3. Set install command: `powershell.exe -ExecutionPolicy Bypass -File ".\Install-Agent.ps1"`
4. Set detection rule: Service "WindowsSecurityAgent" exists and is running
5. Assign to device groups

### Method 4: PDQ Deploy

1. Create new package in PDQ Deploy
2. Add PowerShell step with `Install-Agent.ps1`
3. Deploy to target computers

## 🔧 Post-Installation Verification

### Check Service Status
```powershell
Get-Service WindowsSecurityAgent
```

Expected output: Status = Running

### View Agent Logs
```powershell
Get-EventLog -LogName Application -Source WindowsSecurityAgent -Newest 20
```

### Test API Connection

Check the Admin Portal dashboard - the agent should appear within 5 minutes with "Online" status.

### Manual Service Commands
```powershell
# Start service
Start-Service WindowsSecurityAgent

# Stop service
Stop-Service WindowsSecurityAgent

# Restart service
Restart-Service WindowsSecurityAgent

# Check service details
Get-Service WindowsSecurityAgent | Format-List *
```

## 🛠️ Troubleshooting

### Agent Not Appearing in Dashboard

1. Check service is running: `Get-Service WindowsSecurityAgent`
2. Check event logs for errors
3. Verify `appsettings.json` configuration:
   - Correct API URL
   - Valid API key
   - Network connectivity to API
4. Test API connectivity:
   ```powershell
   Test-NetConnection -ComputerName your-api-url.com -Port 443
   ```

### Service Won't Start

1. Check Event Viewer → Windows Logs → Application
2. Look for errors from source "WindowsSecurityAgent"
3. Common issues:
   - Missing or invalid configuration
   - Insufficient permissions
   - Port conflicts
   - Missing dependencies

### Configuration Changes Not Taking Effect

After modifying `appsettings.json`, restart the service:
```powershell
Restart-Service WindowsSecurityAgent
```

### Access Denied Errors

The service runs as LOCAL SYSTEM by default. If you need to change this:
```powershell
sc.exe config WindowsSecurityAgent obj= "DOMAIN\ServiceAccount" password= "password"
```

## 🔐 Security Best Practices

1. **Protect Configuration Files**
   - `appsettings.json` contains sensitive credentials
   - Set appropriate NTFS permissions
   - Consider encrypting configuration sections

2. **Use HTTPS**
   - Always use HTTPS for Management API
   - Don't set `SkipCertificateValidation: true` in production

3. **Rotate API Keys**
   - Regularly rotate agent API keys
   - Update configuration and restart service

4. **Monitor Agent Health**
   - Set up alerts for offline agents
   - Review audit logs regularly
   - Monitor for suspicious activity

5. **Keep Agents Updated**
   - Deploy updates using same deployment method
   - Test updates in staging environment first

## 📊 Configuration Options

### Heartbeat Interval
How often the agent reports its status (default: 300 seconds)
```json
"HeartbeatIntervalSeconds": 300
```

### Policy Sync Interval
How often the agent checks for policy updates (default: 600 seconds)
```json
"PolicySyncIntervalSeconds": 600
```

### Monitored Paths
Directories to monitor for file system changes:
```json
"MonitoredPaths": [
  "C:\\Program Files",
  "C:\\Program Files (x86)",
  "C:\\Windows\\System32"
]
```

### Enable/Disable Monitoring
```json
"Monitoring": {
  "EnableProcessMonitoring": true,
  "EnableFileSystemMonitoring": true
}
```

## 🗑️ Uninstallation

### Single Computer
```powershell
.\Uninstall-Agent.ps1
```

### Remove Files and Cache
```powershell
.\Uninstall-Agent.ps1 -RemoveFiles $true -RemoveCache $true
```

### Remote Uninstallation
```powershell
Invoke-Command -ComputerName PC001 -ScriptBlock {
    Stop-Service WindowsSecurityAgent -Force
    sc.exe delete WindowsSecurityAgent
}
```

## 📞 Support

For issues or questions:

1. Check Event Viewer logs
2. Review Admin Portal for agent status
3. Consult `DEPLOYMENT_CONFIG.txt` for detailed configuration
4. Contact your IT security team

## 📝 Version Information

- **Agent Version:** 1.0.0
- **.NET Version:** 10.0
- **Platform:** Windows x64
- **Build Type:** Self-contained (no .NET installation required)

## 🔄 Update Process

To update existing agents:

1. Stop the service: `Stop-Service WindowsSecurityAgent`
2. Replace executable and DLLs with new versions
3. Preserve `appsettings.json` (or merge changes)
4. Start the service: `Start-Service WindowsSecurityAgent`

Or simply run `Install-Agent.ps1` again - it will upgrade the existing installation.

## 📄 License

Proprietary - Internal Enterprise Use Only

---

**For detailed configuration instructions, see `DEPLOYMENT_CONFIG.txt`**

