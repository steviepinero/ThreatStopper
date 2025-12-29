# Access Request System

This document describes the Access Request system that allows users to request temporary access to blocked applications and websites.

## Overview

When a user attempts to run a blocked executable or access a blocked website, they can request access by providing a justification. Administrators can then approve or deny these requests through the admin portal.

### Key Features

- **User-initiated requests**: When an application or URL is blocked, users can submit an access request with justification
- **Admin approval workflow**: Administrators review requests and can approve or deny them
- **Time-limited access for executables**: Approved executable access expires after 1 hour
- **Indefinite access for websites**: Approved website access is permanent until revoked
- **Real-time notifications**: Users are notified when their requests are approved or denied

## Architecture

### Database Entities

#### AccessRequest
- **RequestId**: Unique identifier
- **AgentId**: Agent that made the request
- **TenantId**: Organization identifier
- **ResourceType**: "Executable" or "Url"
- **ResourceIdentifier**: Full path to exe or URL
- **ResourceName**: User-friendly display name
- **UserName**: Windows username who made the request
- **Justification**: User's explanation (required)
- **Status**: Pending, Approved, Denied, or Expired
- **RequestedAt**: Timestamp of request
- **ReviewedBy**: Admin who reviewed (if applicable)
- **ReviewedAt**: Review timestamp
- **ReviewComments**: Admin's comments
- **PolicyId/RuleId**: Policy that blocked the resource

#### AccessApproval
- **ApprovalId**: Unique identifier
- **RequestId**: Associated access request
- **AgentId**: Target agent
- **TenantId**: Organization identifier
- **ResourceType**: "Executable" or "Url"
- **ResourceIdentifier**: Resource identifier
- **ApprovedAt**: Approval timestamp
- **ExpiresAt**: Expiration time (1 hour for exes, null for URLs)
- **IsActive**: Whether approval is currently valid

### API Endpoints

#### POST /api/accessrequests
Create a new access request (called by agent)

**Request Body:**
```json
{
  "agentId": "guid",
  "resourceType": "Executable",
  "resourceIdentifier": "C:\\path\\to\\app.exe",
  "resourceName": "MyApp.exe",
  "userName": "DOMAIN\\username",
  "justification": "Need this for work project X",
  "policyId": "guid",
  "ruleId": "guid"
}
```

#### GET /api/accessrequests?tenantId={guid}&status={status}
Get access requests for a tenant
- **status** (optional): Pending, Approved, Denied, or Expired

#### POST /api/accessrequests/review
Approve or deny an access request

**Request Body:**
```json
{
  "requestId": "guid",
  "approved": true,
  "reviewComments": "Approved for project work",
  "reviewedBy": "guid"
}
```

#### POST /api/accessrequests/check-approval
Check if a resource has active approval (called by agent)

**Request Body:**
```json
{
  "agentId": "guid",
  "resourceType": "Executable",
  "resourceIdentifier": "C:\\path\\to\\app.exe"
}
```

**Response:**
```json
{
  "isApproved": true,
  "approvalId": "guid",
  "expiresAt": "2024-01-01T12:00:00Z"
}
```

### Agent Components

#### AccessRequestManager
Service that manages access requests on the agent side:
- **CheckApprovalAsync()**: Checks if a resource has active approval before blocking
- **RequestAccessAsync()**: Triggers popup and submits access request
- **NotifyApproved()**: Shows notification when request is approved
- **NotifyDenied()**: Shows notification when request is denied

#### AccessRequestForm (Windows Forms)
Popup dialog that appears when a resource is blocked:
- Displays resource name and type
- Requires user to provide justification (minimum 10 characters)
- Submits request to cloud API

#### AccessApprovalForm (Windows Forms)
Notification dialog that appears when a request is reviewed:
- Shows approval/denial status
- For approved executables: shows expiration time (1 hour)
- For approved URLs: indicates indefinite access

### PolicyEnforcer Integration

The PolicyEnforcer has been updated to:
1. **Check approvals first**: Before blocking, check if there's an active approval
2. **Allow if approved**: Skip blocking if approval is active and not expired
3. **Trigger request popup**: When blocking, trigger the access request popup

**Example flow:**
```csharp
// Check for approval first
if (_accessRequestManager != null && !string.IsNullOrWhiteSpace(processInfo.ExecutablePath))
{
    var hasApproval = await _accessRequestManager.CheckApprovalAsync("Executable", processInfo.ExecutablePath);
    if (hasApproval)
    {
        // Allow the process
        return (false, null, null, "Active approval");
    }
}

// If blocking, trigger access request
if (shouldBlock && _accessRequestManager != null)
{
    _ = _accessRequestManager.RequestAccessAsync(
        "Executable",
        processInfo.ExecutablePath,
        processInfo.ProcessName,
        processInfo.UserName,
        policyId,
        ruleId
    );
}
```

## Admin Portal

### Access Requests Page
Location: `/access-requests`

Features:
- **Filter tabs**: View Pending, Approved, Denied, or All requests
- **Auto-refresh**: Automatically refreshes every 10 seconds
- **Request cards**: Shows all request details including:
  - Machine name and username
  - Resource type and identifier
  - Justification
  - Request timestamp
  - Review status and comments (if reviewed)
- **Approval actions**:
  - Approve button (shows "1 hour" for executables)
  - Deny button (requires reason)
  - Confirmation dialogs before actions

### Navigation
Added to the sidebar menu with lock icon 🔐

## Usage Scenarios

### Scenario 1: Blocked Executable
1. User tries to run `blocked_app.exe`
2. Agent blocks it and shows popup: "The executable 'blocked_app.exe' has been blocked..."
3. User enters justification: "Need this for urgent client work"
4. Request is submitted and appears in admin portal
5. Admin reviews and approves
6. User sees notification: "Your request to access 'blocked_app.exe' has been APPROVED. This approval expires at..."
7. User can run the executable for the next hour
8. After 1 hour, approval expires and app is blocked again

### Scenario 2: Blocked Website
1. User tries to access `example.com`
2. Hosts file blocks the domain
3. Agent shows popup (would need browser extension or notification)
4. User enters justification: "Need to access vendor portal"
5. Admin approves
6. Website is unblocked indefinitely for that machine

### Scenario 3: Request Denied
1. User requests access to suspicious executable
2. Admin reviews and clicks "Deny"
3. Admin enters reason: "This is known malware"
4. User sees notification: "Your request to access 'malware.exe' has been DENIED"
5. Executable remains blocked

## Security Considerations

1. **Time-limited executable access**: Prevents long-term exposure to potentially risky applications
2. **Justification required**: Creates audit trail and accountability
3. **Admin oversight**: All requests require explicit admin approval
4. **Automatic expiration**: Expired approvals are automatically cleaned up
5. **Per-machine approvals**: Approvals are tied to specific agent/machine combinations
6. **Audit logging**: All requests and approvals are logged

## Configuration

No additional configuration is required. The system works with existing agent and API settings.

### Database Migration

The system adds two new tables:
- `AccessRequests`
- `AccessApprovals`

These are automatically created when the API starts (using `EnsureCreated()`).

For production, you may want to create a proper migration:
```bash
cd src/ManagementAPI/ManagementAPI.WebApi
dotnet ef migrations add AddAccessRequestSystem
dotnet ef database update
```

## Future Enhancements

1. **Browser extension**: Integrate with browsers to show popups for blocked URLs
2. **Bulk approvals**: Allow admins to approve multiple requests at once
3. **Auto-approve rules**: Create rules to automatically approve certain requests
4. **Approval workflows**: Multi-level approval process for sensitive resources
5. **Time-based approvals**: Custom expiration times beyond 1 hour
6. **Temporary policies**: Create temporary allow rules instead of individual approvals
7. **Request history**: View past requests for a user/machine
8. **Analytics**: Track most frequently requested applications
9. **Email notifications**: Email admins when requests are pending
10. **Mobile app**: Review requests from mobile devices

## Testing

### Manual Testing

1. **Create a test request**:
   ```bash
   curl -X POST http://localhost:5140/api/accessrequests \
     -H "Content-Type: application/json" \
     -d '{
       "agentId": "22222222-2222-2222-2222-222222222222",
       "resourceType": "Executable",
       "resourceIdentifier": "C:\\Test\\blocked.exe",
       "resourceName": "blocked.exe",
       "userName": "TestUser",
       "justification": "Testing the access request system"
     }'
   ```

2. **View requests in admin portal**:
   - Navigate to http://localhost:3000/access-requests
   - Should see the test request in Pending tab

3. **Approve the request**:
   - Click "✅ Approve" button
   - Verify request moves to Approved tab
   - Verify expiration time is shown (1 hour from now)

4. **Check approval**:
   ```bash
   curl -X POST http://localhost:5140/api/accessrequests/check-approval \
     -H "Content-Type: application/json" \
     -d '{
       "agentId": "22222222-2222-2222-2222-222222222222",
       "resourceType": "Executable",
       "resourceIdentifier": "C:\\Test\\blocked.exe"
     }'
   ```

## Troubleshooting

### Requests not appearing in admin portal
- Check that the API is running on http://localhost:5140
- Verify the agent ID exists in the database
- Check browser console for API errors

### Approvals not working
- Verify the approval hasn't expired (check `ExpiresAt` field)
- Confirm the `IsActive` flag is true
- Check that the resource identifier matches exactly

### Popup not appearing
- Ensure the agent is running in interactive mode (not as a service)
- Check that `AccessRequestManager` is properly initialized
- Verify events are being subscribed to

## API Response Examples

### Successful Request Creation
```json
{
  "requestId": "550e8400-e29b-41d4-a716-446655440000",
  "agentId": "22222222-2222-2222-2222-222222222222",
  "tenantId": "11111111-1111-1111-1111-111111111111",
  "machineName": "TEST-PC-001",
  "resourceType": "Executable",
  "resourceIdentifier": "C:\\Test\\blocked.exe",
  "resourceName": "blocked.exe",
  "userName": "TestUser",
  "justification": "Testing the access request system",
  "status": "Pending",
  "requestedAt": "2024-01-01T10:00:00Z"
}
```

### Active Approval Check
```json
{
  "isApproved": true,
  "approvalId": "660e8400-e29b-41d4-a716-446655440000",
  "expiresAt": "2024-01-01T11:00:00Z"
}
```

### No Active Approval
```json
{
  "isApproved": false,
  "approvalId": null,
  "expiresAt": null
}
```

