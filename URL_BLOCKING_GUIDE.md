# URL Blocking Feature - Complete Guide

## 🎯 Overview

The Windows Security Platform now includes **URL/Domain blocking** capabilities! This feature blocks access to specific websites by modifying the Windows hosts file, which works across **all browsers** (Chrome, Firefox, Edge, Safari, etc.).

## ✅ What Was Added

### 1. **New Rule Types**
- **URL (Type 5)**: Block specific URLs
- **Domain (Type 6)**: Block entire domains

### 2. **URL Blocker Service**
- Located: `WindowsSecurityAgent.Core/Monitoring/UrlBlocker.cs`
- Modifies Windows hosts file to redirect blocked domains to 127.0.0.1
- Automatically flushes DNS cache when changes are made
- Supports wildcards and patterns

### 3. **Policy Sync Service**
- Located: `WindowsSecurityAgent.Core/Communication/UrlPolicySyncService.cs`
- Syncs URL blocking policies from the cloud every 10 minutes
- Automatically applies blocks to the hosts file

### 4. **Policy Management UI**
- Located: `admin-portal/src/components/Policies/PolicyList.tsx`
- Full CRUD interface for creating and managing policies
- Support for URL, Domain, and other rule types
- Real-time policy activation/deactivation

## 🚀 How to Use

### Step 1: Access the Policy Page

1. Open the Admin Portal: http://localhost:3000
2. Click on **"🛡️ Policies"** in the sidebar

### Step 2: Create a URL Blocking Policy

1. Click **"+ Create Policy"** button
2. Fill in the policy details:
   - **Name**: e.g., "Block Social Media"
   - **Description**: e.g., "Blocks access to social media sites during work hours"
   - **Mode**: Choose "Blacklist" (blocks specified URLs, allows everything else)
   - **Priority**: 100 (higher numbers = higher priority)
   - **Active**: Check this box to enable immediately

### Step 3: Add URL/Domain Rules

For each site you want to block:

1. **Rule Type**: Select "URL" or "Domain"
2. **Action**: Select "Block"
3. **Criteria**: Enter the URL or domain
   - Examples:
     - `facebook.com`
     - `twitter.com`
     - `youtube.com`
     - `reddit.com`
     - `instagram.com`
4. **Description**: Optional note about why this is blocked
5. Click **"Add Rule"**

### Step 4: Save the Policy

1. Review all your rules
2. Click **"Create Policy"**
3. The policy will be saved and synced to all agents

### Step 5: Wait for Sync

- Agents sync policies every **10 minutes** by default
- Or restart the agent service to sync immediately:
  ```powershell
  Restart-Service WindowsSecurityAgent
  ```

## 📋 Example Policies

### Example 1: Block Social Media

**Policy Name**: Block Social Media Sites
**Mode**: Blacklist
**Rules**:
- Domain: `facebook.com` → Block
- Domain: `twitter.com` → Block
- Domain: `instagram.com` → Block
- Domain: `tiktok.com` → Block

### Example 2: Block Streaming Services

**Policy Name**: Block Video Streaming
**Mode**: Blacklist
**Rules**:
- Domain: `youtube.com` → Block
- Domain: `netflix.com` → Block
- Domain: `hulu.com` → Block
- Domain: `twitch.tv` → Block

### Example 3: Block Specific URLs

**Policy Name**: Block Specific Pages
**Mode**: Blacklist
**Rules**:
- URL: `reddit.com/r/gaming` → Block
- URL: `news.ycombinator.com` → Block

## 🔧 How It Works

### Technical Implementation

1. **Policy Creation**: Admin creates a policy with URL/Domain rules in the portal
2. **API Storage**: Policy is saved to the database via Management API
3. **Agent Sync**: Windows Security Agent polls the API every 10 minutes
4. **Hosts File Update**: Agent modifies `C:\Windows\System32\drivers\etc\hosts`
5. **DNS Flush**: Agent flushes DNS cache to apply changes immediately
6. **Browser Block**: All browsers use the hosts file, so blocked sites become inaccessible

### Hosts File Example

When you block `facebook.com`, the agent adds these lines:

```
# Windows Security Agent - Blocked URLs - START
# Last updated: 2025-12-22 14:30:00
127.0.0.1 facebook.com
127.0.0.1 www.facebook.com
# Windows Security Agent - Blocked URLs - END
```

This redirects all requests to `facebook.com` to localhost (127.0.0.1), effectively blocking access.

## 🧪 Testing the Feature

### Test 1: Create a Policy

1. Go to http://localhost:3000/policies
2. Click "Create Policy"
3. Name: "Test Block"
4. Add rule: Domain = `example.com`, Action = Block
5. Click "Create Policy"

### Test 2: Verify in Database

```powershell
Invoke-RestMethod -Uri "http://localhost:5140/api/policies?tenantId=11111111-1111-1111-1111-111111111111"
```

You should see your new policy with the URL rule.

### Test 3: Force Agent Sync

```powershell
# Restart the agent to force immediate sync
Restart-Service WindowsSecurityAgent

# Wait 10 seconds
Start-Sleep -Seconds 10

# Check hosts file
Get-Content "C:\Windows\System32\drivers\etc\hosts"
```

You should see `example.com` listed in the blocked section.

### Test 4: Try to Access Blocked Site

1. Open any browser
2. Try to visit `http://example.com`
3. You should see "This site can't be reached" or similar error

## ⚙️ Configuration

### Change Sync Interval

Edit the agent's `appsettings.json`:

```json
{
  "Agent": {
    "PolicySyncIntervalSeconds": 300
  }
}
```

Default is 600 seconds (10 minutes). Set to 300 for 5-minute syncs.

### Disable URL Blocking

To temporarily disable URL blocking without deleting policies:

1. Go to Policies page
2. Click "Deactivate" on the policy
3. Wait for next sync or restart agent

## 🛡️ Security Considerations

### Administrator Privileges Required

- The agent runs as **LOCAL SYSTEM** (highest privileges)
- Modifying the hosts file requires admin rights
- Users cannot bypass blocks without admin access

### Limitations

1. **Tech-savvy users** can still bypass by:
   - Using VPN
   - Using proxy servers
   - Modifying hosts file manually (requires admin)
   - Using IP addresses instead of domains

2. **HTTPS sites** may show certificate errors instead of being completely blocked

3. **Subdomains** need separate rules:
   - Blocking `facebook.com` blocks `www.facebook.com`
   - But `m.facebook.com` needs a separate rule

### Best Practices

1. **Test policies** on a single machine first
2. **Document policies** with clear descriptions
3. **Review regularly** - some sites change domains
4. **Combine with firewall** rules for stronger enforcement
5. **Monitor audit logs** to see blocked attempts

## 🐛 Troubleshooting

### Policy Not Applying

**Check 1**: Is the policy active?
- Go to Policies page
- Ensure status shows "Active" (green)

**Check 2**: Has the agent synced?
- Check agent logs:
  ```powershell
  Get-EventLog -LogName Application -Source WindowsSecurityAgent -Newest 20
  ```
- Look for "Syncing URL blocking policies"

**Check 3**: Is the hosts file updated?
- View hosts file:
  ```powershell
  Get-Content "C:\Windows\System32\drivers\etc\hosts"
  ```
- Look for the "Windows Security Agent" section

**Check 4**: Is DNS cached?
- Flush DNS manually:
  ```powershell
  ipconfig /flushdns
  ```

### Site Still Accessible

**Cause 1**: Browser cache
- Solution: Clear browser cache or use incognito mode

**Cause 2**: DNS cache
- Solution: Run `ipconfig /flushdns`

**Cause 3**: Using IP address
- Solution: Users accessing by IP bypass hosts file (use firewall rules)

**Cause 4**: VPN/Proxy
- Solution: Block VPN/proxy applications or use network-level blocking

### Agent Service Won't Start

**Check**: Hosts file permissions
```powershell
icacls "C:\Windows\System32\drivers\etc\hosts"
```

The agent needs write access to the hosts file.

## 📊 Monitoring

### View Blocked URLs

```powershell
# See what's currently blocked
$hostsFile = Get-Content "C:\Windows\System32\drivers\etc\hosts"
$hostsFile | Select-String "127.0.0.1"
```

### Check Agent Logs

```powershell
# View recent URL blocking activity
Get-EventLog -LogName Application -Source WindowsSecurityAgent -Newest 50 | 
  Where-Object { $_.Message -like "*URL*" } | 
  Format-Table TimeGenerated, Message -AutoSize
```

### API Endpoint

Get all policies with URL rules:
```powershell
$policies = Invoke-RestMethod -Uri "http://localhost:5140/api/policies?tenantId=11111111-1111-1111-1111-111111111111"
$policies | Where-Object { $_.rules.ruleType -in @(5,6) }
```

## 🎓 Advanced Usage

### Wildcard Blocking

Block all subdomains of a site:
- Rule: Domain = `*.facebook.com`
- This blocks `m.facebook.com`, `api.facebook.com`, etc.

### Time-Based Blocking

Create multiple policies and activate/deactivate them on a schedule:
- "Work Hours Block" - Active 9am-5pm
- "After Hours Allow" - Active 5pm-9am

(Note: Scheduling requires external automation)

### Whitelist Mode

Create a policy in **Whitelist** mode:
- Only allows specified domains
- Blocks everything else
- Useful for kiosk machines or restricted environments

## 📝 Next Steps

1. ✅ Create your first URL blocking policy
2. ✅ Test on your machine (FOOTBALLHEAD)
3. ✅ Deploy to other machines
4. ✅ Monitor effectiveness
5. ✅ Adjust policies based on usage

## 🎉 Summary

You now have a fully functional URL/Domain blocking system that:
- ✅ Works across all browsers
- ✅ Managed from a central web portal
- ✅ Syncs automatically to all agents
- ✅ Requires no user interaction
- ✅ Cannot be easily bypassed by regular users

**Go to http://localhost:3000/policies and create your first policy!** 🚀

