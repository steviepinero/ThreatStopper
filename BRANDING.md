# 🛡️ ThreatStopper - Branding Guide

## Product Name
**ThreatStopper** - Enterprise Endpoint Protection Platform

## Tagline
"Advanced endpoint protection for modern enterprises"

## Description
ThreatStopper is an enterprise-grade endpoint protection platform designed to replace complex Group Policy configurations with an intuitive, cloud-based management system. It provides comprehensive application control, URL blocking, security monitoring, and centralized policy management for Windows environments.

## Key Features
- **Application Control**: Block unauthorized software installations
- **URL/Domain Blocking**: Block malicious websites at the network level
- **Real-time Monitoring**: Process and file system monitoring
- **Centralized Management**: Cloud-based admin portal
- **Policy Engine**: Flexible whitelist/blacklist policies
- **Audit Logging**: Comprehensive security event tracking

## Visual Identity

### Icon/Emoji
🛡️ (Shield) - Represents protection and security

### Color Scheme
- **Primary Orange**: Vibrant Orange (#f97316) - Energy, vigilance, and protection
- **Dark Orange**: Deep Orange (#ea580c) - Hover states and emphasis
- **Darker Orange**: Burnt Orange (#c2410c) - Strong accents
- **Success Green**: Emerald (#4caf50) - Online status and success states
- **Alert Red**: Bright Red (#f44336) - Blocks, errors, and critical alerts
- **Warning Amber**: Amber (#ff9800) - Offline status and warnings
- **Background**: Light Gray (#f5f5f5) - Clean and professional
- **Text**: Dark Gray (#333) - Primary text

## Component Names

### Agent Service
- **Service Name**: `WindowsSecurityAgent` (internal, for compatibility)
- **Display Name**: `ThreatStopper Agent`
- **Description**: "Advanced endpoint protection - monitors and enforces security policies for enterprise endpoints"
- **Event Log Source**: `ThreatStopper`

### Admin Portal
- **Name**: ThreatStopper Admin Portal
- **URL Title**: "ThreatStopper - Admin Portal"
- **Dashboard Header**: "🛡️ ThreatStopper Dashboard"

### Hosts File Markers
```
# ThreatStopper - Blocked URLs - START
# ThreatStopper - Blocked URLs - END
```

## Installation Scripts
All PowerShell scripts display:
```
=====================================================================
ThreatStopper Agent - [Script Purpose]
=====================================================================
```

## File Structure
```
ThreatStopper/
├── Agent (WindowsSecurityAgent)
├── Management API
├── Admin Portal
└── Documentation
```

## Marketing Points
1. **Simple**: Replace complex Group Policy with intuitive web interface
2. **Powerful**: Multi-layered protection (apps, URLs, processes, files)
3. **Centralized**: Manage all endpoints from one dashboard
4. **Real-time**: Instant policy updates and monitoring
5. **Comprehensive**: Full audit trail and reporting

## Competitive Positioning
- **vs. ThreatLocker**: Open-source alternative with URL blocking
- **vs. Group Policy**: Modern, cloud-based, user-friendly
- **vs. Traditional AV**: Proactive application control, not just reactive scanning

## Target Audience
- Small to medium enterprises (10-1000 endpoints)
- IT administrators seeking GPO alternatives
- Security-conscious organizations
- MSPs managing multiple clients

## Use Cases
1. **Prevent Ransomware**: Block unauthorized executables
2. **Web Filtering**: Block malicious domains and phishing sites
3. **Compliance**: Audit and control software installations
4. **Remote Management**: Manage distributed workforce endpoints
5. **Zero Trust**: Whitelist-only application environments

---

**Version**: 1.0
**Last Updated**: December 2025

