# Database Migration Guide for Access Request System

This guide shows how to properly create and apply database migrations for the Access Request System.

## Option 1: Automatic Creation (Development)

The current implementation uses `EnsureCreated()` which automatically creates tables when the API starts. This is suitable for development but not recommended for production.

**No action needed** - tables will be created automatically when you start the API.

## Option 2: Entity Framework Migrations (Production)

For production environments, use proper EF Core migrations:

### Step 1: Install EF Tools (if not already installed)

```bash
dotnet tool install --global dotnet-ef
```

### Step 2: Navigate to the WebApi Project

```bash
cd src\ManagementAPI\ManagementAPI.WebApi
```

### Step 3: Create Migration

```bash
dotnet ef migrations add AddAccessRequestSystem --project ..\ManagementAPI.Data
```

This will create a new migration file in `ManagementAPI.Data/Migrations/` with the access request tables.

### Step 4: Review the Migration

Open the generated migration file and verify it includes:
- `AccessRequests` table with all columns and indexes
- `AccessApprovals` table with all columns and indexes
- Foreign key relationships
- Default values if any

### Step 5: Apply Migration

For development:
```bash
dotnet ef database update
```

For production:
```bash
dotnet ef database update --connection "YourProductionConnectionString"
```

### Step 6: Verify Tables

Connect to your database and verify:
```sql
-- Check AccessRequests table
SELECT * FROM AccessRequests;

-- Check AccessApprovals table  
SELECT * FROM AccessApprovals;

-- Verify indexes
EXEC sp_helpindex 'AccessRequests';
EXEC sp_helpindex 'AccessApprovals';
```

## Option 3: Manual SQL Script (If Needed)

If you prefer to create tables manually, here's the SQL script:

### For SQLite:

```sql
-- Create AccessRequests table
CREATE TABLE AccessRequests (
    RequestId TEXT PRIMARY KEY,
    AgentId TEXT NOT NULL,
    TenantId TEXT NOT NULL,
    ResourceType TEXT NOT NULL,
    ResourceIdentifier TEXT NOT NULL,
    ResourceName TEXT,
    UserName TEXT,
    Justification TEXT,
    Status INTEGER NOT NULL,
    RequestedAt TEXT NOT NULL,
    ReviewedBy TEXT,
    ReviewedAt TEXT,
    ReviewComments TEXT,
    PolicyId TEXT,
    RuleId TEXT,
    FOREIGN KEY (AgentId) REFERENCES Agents(AgentId) ON DELETE CASCADE,
    FOREIGN KEY (TenantId) REFERENCES Organizations(TenantId)
);

-- Create indexes for AccessRequests
CREATE INDEX IX_AccessRequests_AgentId ON AccessRequests(AgentId);
CREATE INDEX IX_AccessRequests_TenantId ON AccessRequests(TenantId);
CREATE INDEX IX_AccessRequests_Status ON AccessRequests(Status);
CREATE INDEX IX_AccessRequests_RequestedAt ON AccessRequests(RequestedAt);
CREATE INDEX IX_AccessRequests_AgentId_ResourceIdentifier_Status 
    ON AccessRequests(AgentId, ResourceIdentifier, Status);

-- Create AccessApprovals table
CREATE TABLE AccessApprovals (
    ApprovalId TEXT PRIMARY KEY,
    RequestId TEXT NOT NULL,
    AgentId TEXT NOT NULL,
    TenantId TEXT NOT NULL,
    ResourceType TEXT NOT NULL,
    ResourceIdentifier TEXT NOT NULL,
    ApprovedAt TEXT NOT NULL,
    ExpiresAt TEXT,
    IsActive INTEGER NOT NULL,
    FOREIGN KEY (RequestId) REFERENCES AccessRequests(RequestId) ON DELETE CASCADE,
    FOREIGN KEY (AgentId) REFERENCES Agents(AgentId),
    FOREIGN KEY (TenantId) REFERENCES Organizations(TenantId)
);

-- Create indexes for AccessApprovals
CREATE INDEX IX_AccessApprovals_AgentId ON AccessApprovals(AgentId);
CREATE INDEX IX_AccessApprovals_TenantId ON AccessApprovals(TenantId);
CREATE INDEX IX_AccessApprovals_RequestId ON AccessApprovals(RequestId);
CREATE INDEX IX_AccessApprovals_IsActive ON AccessApprovals(IsActive);
CREATE INDEX IX_AccessApprovals_ExpiresAt ON AccessApprovals(ExpiresAt);
CREATE INDEX IX_AccessApprovals_AgentId_ResourceIdentifier_IsActive 
    ON AccessApprovals(AgentId, ResourceIdentifier, IsActive);
```

### For PostgreSQL:

```sql
-- Create AccessRequests table
CREATE TABLE "AccessRequests" (
    "RequestId" UUID PRIMARY KEY,
    "AgentId" UUID NOT NULL,
    "TenantId" UUID NOT NULL,
    "ResourceType" VARCHAR(50) NOT NULL,
    "ResourceIdentifier" VARCHAR(1000) NOT NULL,
    "ResourceName" VARCHAR(500),
    "UserName" VARCHAR(200),
    "Justification" VARCHAR(2000),
    "Status" INTEGER NOT NULL,
    "RequestedAt" TIMESTAMP NOT NULL,
    "ReviewedBy" UUID,
    "ReviewedAt" TIMESTAMP,
    "ReviewComments" VARCHAR(2000),
    "PolicyId" UUID,
    "RuleId" UUID,
    CONSTRAINT "FK_AccessRequests_Agents" FOREIGN KEY ("AgentId") 
        REFERENCES "Agents"("AgentId") ON DELETE CASCADE,
    CONSTRAINT "FK_AccessRequests_Organizations" FOREIGN KEY ("TenantId") 
        REFERENCES "Organizations"("TenantId")
);

-- Create indexes for AccessRequests
CREATE INDEX "IX_AccessRequests_AgentId" ON "AccessRequests"("AgentId");
CREATE INDEX "IX_AccessRequests_TenantId" ON "AccessRequests"("TenantId");
CREATE INDEX "IX_AccessRequests_Status" ON "AccessRequests"("Status");
CREATE INDEX "IX_AccessRequests_RequestedAt" ON "AccessRequests"("RequestedAt");
CREATE INDEX "IX_AccessRequests_AgentId_ResourceIdentifier_Status" 
    ON "AccessRequests"("AgentId", "ResourceIdentifier", "Status");

-- Create AccessApprovals table
CREATE TABLE "AccessApprovals" (
    "ApprovalId" UUID PRIMARY KEY,
    "RequestId" UUID NOT NULL,
    "AgentId" UUID NOT NULL,
    "TenantId" UUID NOT NULL,
    "ResourceType" VARCHAR(50) NOT NULL,
    "ResourceIdentifier" VARCHAR(1000) NOT NULL,
    "ApprovedAt" TIMESTAMP NOT NULL,
    "ExpiresAt" TIMESTAMP,
    "IsActive" BOOLEAN NOT NULL,
    CONSTRAINT "FK_AccessApprovals_AccessRequests" FOREIGN KEY ("RequestId") 
        REFERENCES "AccessRequests"("RequestId") ON DELETE CASCADE,
    CONSTRAINT "FK_AccessApprovals_Agents" FOREIGN KEY ("AgentId") 
        REFERENCES "Agents"("AgentId"),
    CONSTRAINT "FK_AccessApprovals_Organizations" FOREIGN KEY ("TenantId") 
        REFERENCES "Organizations"("TenantId")
);

-- Create indexes for AccessApprovals
CREATE INDEX "IX_AccessApprovals_AgentId" ON "AccessApprovals"("AgentId");
CREATE INDEX "IX_AccessApprovals_TenantId" ON "AccessApprovals"("TenantId");
CREATE INDEX "IX_AccessApprovals_RequestId" ON "AccessApprovals"("RequestId");
CREATE INDEX "IX_AccessApprovals_IsActive" ON "AccessApprovals"("IsActive");
CREATE INDEX "IX_AccessApprovals_ExpiresAt" ON "AccessApprovals"("ExpiresAt");
CREATE INDEX "IX_AccessApprovals_AgentId_ResourceIdentifier_IsActive" 
    ON "AccessApprovals"("AgentId", "ResourceIdentifier", "IsActive");
```

## Rollback Instructions

If you need to rollback the migration:

### Using EF Migrations:

```bash
# Remove the last migration (before applying it)
dotnet ef migrations remove --project ..\ManagementAPI.Data

# Revert to previous migration (after applying it)
dotnet ef database update PreviousMigrationName
```

### Manual Rollback:

```sql
-- Drop tables in correct order (due to foreign keys)
DROP TABLE AccessApprovals;
DROP TABLE AccessRequests;
```

## Verification Queries

After creating the tables, run these queries to verify everything is set up correctly:

```sql
-- Check table exists
SELECT name FROM sqlite_master WHERE type='table' AND name='AccessRequests';
SELECT name FROM sqlite_master WHERE type='table' AND name='AccessApprovals';

-- Check indexes
SELECT name FROM sqlite_master WHERE type='index' AND tbl_name='AccessRequests';
SELECT name FROM sqlite_master WHERE type='index' AND tbl_name='AccessApprovals';

-- Test insert (should work)
INSERT INTO AccessRequests (
    RequestId, AgentId, TenantId, ResourceType, ResourceIdentifier, 
    ResourceName, UserName, Justification, Status, RequestedAt
) VALUES (
    '550e8400-e29b-41d4-a716-446655440000',
    '22222222-2222-2222-2222-222222222222',
    '11111111-1111-1111-1111-111111111111',
    'Executable',
    'C:\Test\test.exe',
    'test.exe',
    'TestUser',
    'Testing the system',
    0,
    '2024-01-01 10:00:00'
);

-- Clean up test data
DELETE FROM AccessRequests WHERE RequestId = '550e8400-e29b-41d4-a716-446655440000';
```

## Troubleshooting

### Issue: Migration fails with "table already exists"

**Solution:** The table was created by `EnsureCreated()`. Either:
1. Delete the existing database and start fresh
2. Use `dotnet ef migrations add InitialCreate --output-dir Migrations` to create an initial migration that matches current state

### Issue: Foreign key constraint fails

**Solution:** Ensure the referenced tables (Agents, Organizations) exist before creating AccessRequests/AccessApprovals tables.

### Issue: Indexes not created

**Solution:** Manually create the indexes using the SQL scripts above, or ensure your migration includes the HasIndex() configurations from ApplicationDbContext.cs.

## Production Deployment Checklist

- [ ] Backup existing database
- [ ] Review migration SQL before applying
- [ ] Test migration on staging environment
- [ ] Apply migration during maintenance window
- [ ] Verify tables created successfully
- [ ] Verify indexes created successfully
- [ ] Run verification queries
- [ ] Test API endpoints with new tables
- [ ] Monitor application logs for errors
- [ ] Keep rollback script ready

## Notes

- The `Status` column uses integer values: 0=Pending, 1=Approved, 2=Denied, 3=Expired
- `IsActive` is stored as INTEGER in SQLite (0=false, 1=true) and BOOLEAN in PostgreSQL
- GUIDs are stored as TEXT in SQLite and UUID in PostgreSQL
- DateTime values are stored as TEXT (ISO 8601 format) in SQLite and TIMESTAMP in PostgreSQL
- All string lengths match the configuration in ApplicationDbContext.cs

