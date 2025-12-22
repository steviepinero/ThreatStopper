====================================================================
WINDOWS SECURITY PLATFORM - ENTERPRISE DEPLOYMENT PACKAGE
====================================================================

Welcome! This package contains everything you need to deploy the
Windows Security Agent to computers on your enterprise network.

====================================================================
📦 WHAT'S INCLUDED
====================================================================

WindowsSecurityAgent/
  └── Complete deployment package with:
      • WindowsSecurityAgent.Service.exe (73 MB executable)
      • Installation and uninstallation scripts
      • Configuration files
      • Complete documentation

DEPLOYMENT_GUIDE.md
  └── Complete guide for deploying the entire platform
      (Management API, Admin Portal, and Agents)

PACKAGE_INFO.txt
  └── Detailed information about this package

====================================================================
🚀 QUICK START (3 STEPS)
====================================================================

STEP 1: Configure
-----------------
Edit: WindowsSecurityAgent\appsettings.json

Set these values:
  • CloudApi.BaseUrl = "https://your-management-api-url.com"
  • Agent.EncryptionKey = [Generate using PowerShell command below]

Generate encryption key:
  $bytes = New-Object byte[] 32
  [Security.Cryptography.RNGCryptoServiceProvider]::Create().GetBytes($bytes)
  [Convert]::ToBase64String($bytes)

STEP 2: Install on Test Computer
---------------------------------
1. Copy WindowsSecurityAgent folder to the computer
2. Right-click Install-Agent.bat
3. Select "Run as Administrator"

OR use PowerShell:
  cd WindowsSecurityAgent
  .\Install-Agent.ps1

STEP 3: Verify
--------------
• Check service: Get-Service WindowsSecurityAgent
• Check Admin Portal - agent should appear within 5 minutes
• Status should show "Online"

====================================================================
📋 DEPLOYMENT METHODS
====================================================================

Choose the method that works best for your environment:

1. SINGLE COMPUTER
   └── Double-click Install-Agent.bat (as Administrator)
       OR run Install-Agent.ps1 in PowerShell

2. MULTIPLE COMPUTERS (Remote PowerShell)
   └── Create computers.txt with computer names
       Run: Deploy-ToRemoteComputers.ps1 -ComputerListFile "computers.txt"

3. GROUP POLICY (Active Directory)
   └── Copy to network share
       Create GPO with startup script
       Deploy to target OUs

4. SCCM/Configuration Manager
   └── Create application package
       Deploy to device collections

5. MICROSOFT INTUNE
   └── Package as Win32 app
       Deploy to device groups

====================================================================
📖 DOCUMENTATION
====================================================================

START HERE:
  WindowsSecurityAgent\QUICK_START.txt
    └── Fastest way to get started

DETAILED GUIDES:
  WindowsSecurityAgent\README.md
    └── Complete agent deployment documentation

  WindowsSecurityAgent\DEPLOYMENT_CONFIG.txt
    └── Detailed configuration reference

  DEPLOYMENT_GUIDE.md
    └── Full platform deployment (API, Portal, Agents)

  PACKAGE_INFO.txt
    └── Package contents and specifications

====================================================================
⚠️ IMPORTANT NOTES
====================================================================

BEFORE DEPLOYMENT:
  ✓ Configure appsettings.json (REQUIRED!)
  ✓ Test on a single computer first
  ✓ Verify agent appears in Admin Portal
  ✓ Test policy enforcement

SYSTEM REQUIREMENTS:
  • Windows 10 (1809+), Windows 11, or Server 2016+
  • Administrator privileges
  • Network access to Management API (HTTPS)
  • 100 MB disk space, 256 MB RAM

SECURITY:
  • Runs with SYSTEM privileges
  • Protect appsettings.json (contains credentials)
  • Always use HTTPS for Management API
  • Test in staging before production

====================================================================
🔧 TROUBLESHOOTING
====================================================================

Service won't start?
  → Check Event Viewer → Application → WindowsSecurityAgent
  → Verify appsettings.json is configured correctly

Agent not in dashboard?
  → Wait 5 minutes for first heartbeat
  → Check API URL is correct
  → Test connectivity: Test-NetConnection your-api-url.com -Port 443

Need more help?
  → See WindowsSecurityAgent\README.md
  → Check Event Logs for errors
  → Review configuration in appsettings.json

====================================================================
📞 SUPPORT
====================================================================

Documentation: See files listed above
Logs: Event Viewer → Application → WindowsSecurityAgent
Dashboard: Check Admin Portal for agent status
Contact: Your IT security team

====================================================================
✅ DEPLOYMENT CHECKLIST
====================================================================

Pre-Deployment:
  [ ] Management API is deployed and accessible
  [ ] Admin Portal is deployed and working
  [ ] appsettings.json is configured
  [ ] Encryption key is generated
  [ ] API credentials are obtained

Testing:
  [ ] Installed on test computer
  [ ] Agent appears in dashboard
  [ ] Status shows "Online"
  [ ] Policy enforcement tested
  [ ] Logs are clean (no errors)

Production Deployment:
  [ ] Deployment method selected
  [ ] Target computers identified
  [ ] Deployment scheduled
  [ ] Rollback plan prepared
  [ ] Monitoring configured

Post-Deployment:
  [ ] All agents online in dashboard
  [ ] Heartbeats updating regularly
  [ ] Policies syncing correctly
  [ ] Security events being logged
  [ ] No errors in Event Logs

====================================================================
🎯 NEXT STEPS
====================================================================

1. Read QUICK_START.txt in WindowsSecurityAgent folder
2. Configure appsettings.json
3. Test on one computer
4. Verify in Admin Portal
5. Plan enterprise rollout
6. Deploy to production
7. Monitor and maintain

====================================================================

Good luck with your deployment! 🛡️

For detailed instructions, start with:
  WindowsSecurityAgent\QUICK_START.txt

====================================================================

