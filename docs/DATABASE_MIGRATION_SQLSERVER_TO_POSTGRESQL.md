# Database Migration Guide: SQL Server to PostgreSQL

## Executive Summary

This document outlines the migration strategy for transitioning the HospitalityPOS system from Microsoft SQL Server to PostgreSQL. The migration is driven by the need for:

- **Easy deployment** for non-technical users
- **Multi-machine networked access** (multiple POS terminals connecting to a single database)
- **Cost efficiency** (no licensing limitations)
- **Cross-platform support** (Windows, macOS, Linux)

### Migration Complexity: LOW-MODERATE
### Estimated Effort: 2-3 Days
### Risk Level: LOW

---

## Table of Contents

1. [Current Architecture Analysis](#1-current-architecture-analysis)
2. [Why PostgreSQL](#2-why-postgresql)
3. [Prerequisites](#3-prerequisites)
4. [Migration Steps](#4-migration-steps)
5. [Code Changes](#5-code-changes)
6. [Configuration Changes](#6-configuration-changes)
7. [Data Type Mappings](#7-data-type-mappings)
8. [Migration Scripts](#8-migration-scripts)
9. [Testing Plan](#9-testing-plan)
10. [Deployment Strategy](#10-deployment-strategy)
11. [Rollback Plan](#11-rollback-plan)
12. [Post-Migration Checklist](#12-post-migration-checklist)

---

## 1. Current Architecture Analysis

### Database Technology Stack
| Component | Current |
|-----------|---------|
| Database Engine | SQL Server Express (SQLEXPRESS01) |
| ORM | Entity Framework Core 10.0.0 |
| Connection | Windows Authentication (Trusted Connection) |
| Database Name | posdb |

### Architecture Strengths (Migration-Friendly)
- **100% LINQ to Entities** - No raw SQL queries
- **No Stored Procedures** - All business logic in C# code
- **No Triggers** - Event handling in application layer
- **Clean Repository Pattern** - Abstracted data access
- **Unit of Work Pattern** - Transaction management in code

### SQL Server-Specific Features in Use
| Feature | Files Affected | Migration Action |
|---------|---------------|------------------|
| `GETUTCDATE()` | 5+ configuration files | Replace with `DateTime.UtcNow` |
| `Identity` columns | All migrations | Auto-handled by EF Core |
| `datetime2` | All DateTime fields | Maps to `timestamp` |
| `nvarchar(max)` | Text fields | Maps to `text` |
| `decimal(18,2)` | Money fields | Maps to `numeric(18,2)` |
| `EnableRetryOnFailure()` | DI configuration | Use Npgsql equivalent |

### Key Files Inventory
| File | Path | Changes Required |
|------|------|------------------|
| DbContext | `src/HospitalityPOS.Infrastructure/Data/POSDbContext.cs` | Minor |
| DI Configuration | `src/HospitalityPOS.Infrastructure/Extensions/ServiceCollectionExtensions.cs` | Moderate |
| App Settings | `src/HospitalityPOS.WPF/appsettings.json` | Connection string |
| Design-Time Factory | `src/HospitalityPOS.Infrastructure/Data/DesignTimeDbContextFactory.cs` | Provider change |
| Entity Configurations | `src/HospitalityPOS.Infrastructure/Data/Configurations/*.cs` | Remove SQL Server defaults |
| Migrations | `src/HospitalityPOS.Infrastructure/Data/Migrations/*` | Regenerate completely |

---

## 2. Why PostgreSQL

### Comparison Matrix

| Requirement | PostgreSQL | SQL Server Express | SQLite |
|-------------|------------|-------------------|--------|
| Multi-machine network access | Full support | Full support | NOT SUPPORTED |
| Free & open source | Yes | Yes (10GB limit) | Yes |
| Easy installation | Moderate | Complex | Very Easy |
| Cross-platform | Yes | Windows only | Yes |
| Large datasets (1GB+) | Excellent | Excellent | Good |
| Concurrent connections | Unlimited | Limited | File-lock based |
| EF Core support | Excellent | Excellent | Good |

### PostgreSQL Advantages
1. **True client-server architecture** - Designed for multi-client concurrent access
2. **No database size limits** - Unlike SQL Server Express (10GB limit)
3. **Cross-platform** - Future-proof for Mac/Linux deployment
4. **Active community** - Extensive documentation and support
5. **Performance** - Comparable to SQL Server for OLTP workloads
6. **JSON support** - Native JSON/JSONB types if needed later

---

## 3. Prerequisites

### Development Environment
```bash
# Install PostgreSQL (Windows)
# Download from: https://www.postgresql.org/download/windows/
# Or use Chocolatey:
choco install postgresql16

# Install PostgreSQL (via Scoop)
scoop install postgresql

# Verify installation
psql --version
```

### NuGet Packages to Install
```xml
<!-- Remove these packages -->
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.0" />

<!-- Add these packages -->
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
```

### PostgreSQL Server Setup
```sql
-- Connect as postgres superuser
-- Create database and user for POS

CREATE USER posuser WITH PASSWORD 'your_secure_password';
CREATE DATABASE posdb OWNER posuser;
GRANT ALL PRIVILEGES ON DATABASE posdb TO posuser;

-- Connect to posdb and grant schema permissions
\c posdb
GRANT ALL ON SCHEMA public TO posuser;
```

---

## 4. Migration Steps

### Phase 1: Package Updates (15 minutes)

#### Step 1.1: Update NuGet Packages

**File: `src/HospitalityPOS.Infrastructure/HospitalityPOS.Infrastructure.csproj`**

```xml
<!-- REMOVE -->
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.0" />

<!-- ADD -->
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
```

#### Step 1.2: Restore Packages
```bash
cd src/HospitalityPOS.Infrastructure
dotnet restore
```

---

### Phase 2: Code Changes (2-4 hours)

#### Step 2.1: Update Service Collection Extensions

**File: `src/HospitalityPOS.Infrastructure/Extensions/ServiceCollectionExtensions.cs`**

```csharp
// BEFORE (SQL Server)
services.AddDbContext<POSDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);

        sqlOptions.MigrationsAssembly(typeof(POSDbContext).Assembly.FullName);
        sqlOptions.CommandTimeout(30);
    });
});

// AFTER (PostgreSQL)
services.AddDbContext<POSDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);

        npgsqlOptions.MigrationsAssembly(typeof(POSDbContext).Assembly.FullName);
        npgsqlOptions.CommandTimeout(30);
    });
});
```

#### Step 2.2: Update Design-Time Factory

**File: `src/HospitalityPOS.Infrastructure/Data/DesignTimeDbContextFactory.cs`**

```csharp
// BEFORE
optionsBuilder.UseSqlServer(connectionString, options =>
{
    options.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
});

// AFTER
optionsBuilder.UseNpgsql(connectionString, options =>
{
    options.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
});
```

#### Step 2.3: Update DbContext Factory Registration

```csharp
// BEFORE
services.AddDbContextFactory<POSDbContext>(options =>
{
    options.UseSqlServer(connectionString, ...);
});

// AFTER
services.AddDbContextFactory<POSDbContext>(options =>
{
    options.UseNpgsql(connectionString, ...);
});
```

---

### Phase 3: Configuration File Changes (30 minutes)

#### Step 3.1: Update Entity Configurations

Replace all `GETUTCDATE()` with proper C# defaults.

**Files to update:**
- `FloorConfiguration.cs`
- `LoyaltyConfiguration.cs`
- Any other files using `.HasDefaultValueSql("GETUTCDATE()")`

```csharp
// BEFORE (SQL Server specific)
builder.Property(e => e.CreatedAt)
    .HasDefaultValueSql("GETUTCDATE()");

// AFTER (Database agnostic - Option 1: Remove default, set in code)
builder.Property(e => e.CreatedAt)
    .IsRequired();

// AFTER (Database agnostic - Option 2: Use PostgreSQL function)
builder.Property(e => e.CreatedAt)
    .HasDefaultValueSql("CURRENT_TIMESTAMP");

// AFTER (Database agnostic - Option 3: Use ValueGenerator)
builder.Property(e => e.CreatedAt)
    .HasDefaultValue(DateTime.UtcNow); // Note: This sets migration-time value
```

**Recommended Approach: Use Value Generators**

Create a new file: `src/HospitalityPOS.Infrastructure/Data/ValueGenerators/UtcNowGenerator.cs`

```csharp
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace HospitalityPOS.Infrastructure.Data.ValueGenerators;

public class UtcNowGenerator : ValueGenerator<DateTime>
{
    public override DateTime Next(EntityEntry entry) => DateTime.UtcNow;
    public override bool GeneratesTemporaryValues => false;
}
```

Then in configurations:
```csharp
builder.Property(e => e.CreatedAt)
    .HasValueGenerator<UtcNowGenerator>()
    .ValueGeneratedOnAdd();
```

---

### Phase 4: Connection String Update (5 minutes)

#### Step 4.1: Update appsettings.json

**File: `src/HospitalityPOS.WPF/appsettings.json`**

```json
{
  "ConnectionStrings": {
    // BEFORE (SQL Server)
    "DefaultConnection": "Server=localhost\\SQLEXPRESS01;Database=posdb;Trusted_Connection=True;TrustServerCertificate=True;"

    // AFTER (PostgreSQL)
    "DefaultConnection": "Host=localhost;Port=5432;Database=posdb;Username=posuser;Password=your_secure_password;"
  }
}
```

#### PostgreSQL Connection String Options

```
# Basic
Host=localhost;Database=posdb;Username=posuser;Password=xxx;

# With port (if non-default)
Host=192.168.1.100;Port=5432;Database=posdb;Username=posuser;Password=xxx;

# With SSL (production)
Host=dbserver;Database=posdb;Username=posuser;Password=xxx;SSL Mode=Require;Trust Server Certificate=true;

# With connection pooling
Host=localhost;Database=posdb;Username=posuser;Password=xxx;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=100;

# Full production example
Host=192.168.1.100;Port=5432;Database=posdb;Username=posuser;Password=xxx;SSL Mode=Prefer;Pooling=true;Minimum Pool Size=10;Maximum Pool Size=100;Connection Idle Lifetime=300;
```

---

### Phase 5: Migration Regeneration (1-2 hours)

#### Step 5.1: Delete Existing Migrations

```bash
# Navigate to Infrastructure project
cd src/HospitalityPOS.Infrastructure

# Delete all migration files (keep the folder)
rm -rf Data/Migrations/*

# Or on Windows PowerShell
Remove-Item -Path "Data\Migrations\*" -Recurse -Force
```

#### Step 5.2: Create New Initial Migration

```bash
# From solution root
cd src/HospitalityPOS.Infrastructure

# Create new migration for PostgreSQL
dotnet ef migrations add InitialCreate_PostgreSQL --startup-project ../HospitalityPOS.WPF

# Verify migration was created
ls Data/Migrations/
```

#### Step 5.3: Review Generated Migration

Open the generated migration file and verify:
- No SQL Server-specific syntax
- Column types are PostgreSQL compatible
- Identity columns use PostgreSQL serial/identity

#### Step 5.4: Apply Migration to Database

```bash
# Update database
dotnet ef database update --startup-project ../HospitalityPOS.WPF

# Or generate SQL script for review
dotnet ef migrations script --startup-project ../HospitalityPOS.WPF -o migration.sql
```

---

## 5. Code Changes

### Complete File Change List

| File | Change Type | Description |
|------|-------------|-------------|
| `HospitalityPOS.Infrastructure.csproj` | Package | Replace SqlServer with Npgsql |
| `ServiceCollectionExtensions.cs` | Code | `UseSqlServer` → `UseNpgsql` |
| `DesignTimeDbContextFactory.cs` | Code | `UseSqlServer` → `UseNpgsql` |
| `appsettings.json` | Config | Update connection string |
| `appsettings.Development.json` | Config | Update connection string |
| `FloorConfiguration.cs` | Code | Replace `GETUTCDATE()` |
| `LoyaltyConfiguration.cs` | Code | Replace `GETUTCDATE()` |
| All Migrations | Delete | Regenerate for PostgreSQL |

### Using Statements to Update

```csharp
// REMOVE
using Microsoft.EntityFrameworkCore.SqlServer;

// ADD
using Npgsql.EntityFrameworkCore.PostgreSQL;
```

---

## 6. Configuration Changes

### PostgreSQL-Specific Optimizations

Add to `POSDbContext.cs` OnConfiguring:

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    if (!optionsBuilder.IsConfigured)
    {
        // PostgreSQL specific optimizations
        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            // Enable retry on transient failures
            options.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);

            // Set command timeout
            options.CommandTimeout(30);

            // Use lowercase naming convention (PostgreSQL standard)
            // options.UseSnakeCaseNamingConvention(); // Optional - requires additional package
        });
    }

    base.OnConfiguring(optionsBuilder);
}
```

### Optional: Snake Case Naming Convention

PostgreSQL convention uses snake_case for identifiers. To maintain consistency:

```bash
# Install package
dotnet add package EFCore.NamingConventions
```

```csharp
// In ServiceCollectionExtensions.cs
options.UseNpgsql(connectionString)
       .UseSnakeCaseNamingConvention();
```

> **Note:** Only use this for new projects. For migration, keep existing PascalCase names.

---

## 7. Data Type Mappings

### Automatic Mappings (EF Core handles these)

| SQL Server | PostgreSQL | Notes |
|------------|------------|-------|
| `int` | `integer` | Automatic |
| `bigint` | `bigint` | Automatic |
| `smallint` | `smallint` | Automatic |
| `bit` | `boolean` | Automatic |
| `nvarchar(n)` | `character varying(n)` | Automatic |
| `nvarchar(max)` | `text` | Automatic |
| `varchar(n)` | `character varying(n)` | Automatic |
| `datetime` | `timestamp without time zone` | Automatic |
| `datetime2` | `timestamp without time zone` | Automatic |
| `datetimeoffset` | `timestamp with time zone` | Automatic |
| `decimal(p,s)` | `numeric(p,s)` | Automatic |
| `float` | `double precision` | Automatic |
| `real` | `real` | Automatic |
| `uniqueidentifier` | `uuid` | Automatic |
| `varbinary(max)` | `bytea` | Automatic |
| `image` | `bytea` | Automatic |

### Manual Attention Required

| SQL Server | PostgreSQL | Action |
|------------|------------|--------|
| `IDENTITY(1,1)` | `GENERATED ALWAYS AS IDENTITY` | EF Core handles |
| `GETUTCDATE()` | `CURRENT_TIMESTAMP` or C# code | Update configurations |
| `NEWID()` | `gen_random_uuid()` | Update configurations |
| `DATEDIFF()` | `DATE_PART()` or `-` operator | If used in raw SQL |

---

## 8. Migration Scripts

### Data Migration (If Existing Data Exists)

If you have existing data in SQL Server that needs to be migrated:

#### Option A: Using pg_dump/pg_restore (Recommended for large datasets)

1. Export from SQL Server to CSV
2. Import to PostgreSQL using `COPY` command

```sql
-- PostgreSQL import
COPY products(id, code, name, selling_price, ...)
FROM '/path/to/products.csv'
WITH (FORMAT csv, HEADER true);
```

#### Option B: Using ETL Tool

- **pgLoader** - Can directly migrate from SQL Server to PostgreSQL
- **DBeaver** - GUI tool with data transfer capabilities

```bash
# pgLoader example
pgloader mssql://user:pass@localhost/posdb postgresql://posuser:pass@localhost/posdb
```

#### Option C: Application-Level Migration

Create a one-time migration utility:

```csharp
public class DataMigrationService
{
    private readonly SqlServerDbContext _sqlServer;
    private readonly PostgreSqlDbContext _postgresql;

    public async Task MigrateAllDataAsync()
    {
        // Migrate in order respecting foreign keys
        await MigrateUsersAsync();
        await MigrateCategoriesAsync();
        await MigrateProductsAsync();
        await MigrateOrdersAsync();
        // ... etc
    }

    private async Task MigrateProductsAsync()
    {
        var products = await _sqlServer.Products.ToListAsync();
        await _postgresql.Products.AddRangeAsync(products);
        await _postgresql.SaveChangesAsync();
    }
}
```

---

## 9. Testing Plan

### Unit Tests

```csharp
[Fact]
public async Task CanConnectToPostgreSQL()
{
    // Arrange
    var options = new DbContextOptionsBuilder<POSDbContext>()
        .UseNpgsql("Host=localhost;Database=posdb_test;Username=test;Password=test")
        .Options;

    // Act
    using var context = new POSDbContext(options);
    var canConnect = await context.Database.CanConnectAsync();

    // Assert
    Assert.True(canConnect);
}
```

### Integration Tests Checklist

- [ ] Database connection successful
- [ ] All migrations apply cleanly
- [ ] CRUD operations on all 60+ entities
- [ ] Complex queries (joins, aggregations)
- [ ] Transaction handling (commit/rollback)
- [ ] Concurrent access from multiple connections
- [ ] Large dataset performance (30,000+ products)
- [ ] Decimal precision for monetary values
- [ ] DateTime handling (UTC consistency)
- [ ] Soft delete query filters working
- [ ] Audit fields (CreatedAt, UpdatedAt) populated

### Performance Tests

```csharp
[Fact]
public async Task ProductQueryPerformance()
{
    // Test with 30,000 products
    var stopwatch = Stopwatch.StartNew();

    var products = await _context.Products
        .Where(p => p.CategoryId == testCategoryId)
        .OrderBy(p => p.Name)
        .Take(100)
        .ToListAsync();

    stopwatch.Stop();

    // Should complete in under 100ms with proper indexing
    Assert.True(stopwatch.ElapsedMilliseconds < 100);
}
```

### Manual Testing Checklist

- [ ] Login/Authentication works
- [ ] Product search with LIKE queries
- [ ] Order creation and payment processing
- [ ] Receipt generation
- [ ] Inventory updates
- [ ] Report generation
- [ ] Multi-terminal concurrent access
- [ ] Network disconnection handling

---

## 10. Deployment Strategy

### Server Installation (One-Time Setup)

#### Windows Server

```powershell
# Download PostgreSQL 16 installer
# https://www.postgresql.org/download/windows/

# Silent install (for automation)
postgresql-16-windows-x64.exe --mode unattended --superpassword "postgres_admin_pass" --serverport 5432

# Configure firewall
netsh advfirewall firewall add rule name="PostgreSQL" dir=in action=allow protocol=TCP localport=5432

# Configure pg_hba.conf for network access
# Edit: C:\Program Files\PostgreSQL\16\data\pg_hba.conf
# Add line for your network:
# host    all    all    192.168.1.0/24    scram-sha-256
```

#### Create POS Database and User

```sql
-- Connect as postgres superuser
psql -U postgres

-- Create dedicated user
CREATE USER posuser WITH PASSWORD 'secure_password_here' LOGIN;

-- Create database
CREATE DATABASE posdb
    WITH OWNER = posuser
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.UTF-8'
    LC_CTYPE = 'en_US.UTF-8'
    TEMPLATE = template0;

-- Grant permissions
GRANT ALL PRIVILEGES ON DATABASE posdb TO posuser;

-- Connect to posdb
\c posdb

-- Grant schema permissions
GRANT ALL ON SCHEMA public TO posuser;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO posuser;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO posuser;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO posuser;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO posuser;
```

### Client Deployment

#### Installer Configuration

Create two installer types:

**1. Server Installer (for main machine)**
- PostgreSQL 16
- POS Application
- Database initialization
- Firewall rules

**2. Client Installer (for terminal machines)**
- POS Application only
- Configuration wizard for server IP

#### Configuration Wizard

```csharp
public class DatabaseConnectionWizard
{
    public async Task<bool> TestConnectionAsync(string host, int port, string database, string user, string password)
    {
        var connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={password}";

        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed");
            return false;
        }
    }

    public void SaveConfiguration(string connectionString)
    {
        // Save to appsettings.json or encrypted config
        var config = new { ConnectionStrings = new { DefaultConnection = connectionString } };
        File.WriteAllText("appsettings.local.json", JsonSerializer.Serialize(config));
    }
}
```

### Docker Deployment (Alternative)

```yaml
# docker-compose.yml
version: '3.8'

services:
  postgres:
    image: postgres:16
    container_name: pos-database
    environment:
      POSTGRES_USER: posuser
      POSTGRES_PASSWORD: ${DB_PASSWORD}
      POSTGRES_DB: posdb
    volumes:
      - pos_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"
    restart: unless-stopped

volumes:
  pos_data:
```

---

## 11. Rollback Plan

### Before Migration

1. **Full SQL Server backup**
   ```sql
   BACKUP DATABASE posdb TO DISK = 'C:\Backups\posdb_pre_migration.bak'
   ```

2. **Export data to CSV** (safety net)
   ```bash
   bcp "SELECT * FROM Products" queryout products.csv -c -t, -S localhost\SQLEXPRESS01 -d posdb -T
   ```

3. **Git branch for code changes**
   ```bash
   git checkout -b feature/postgresql-migration
   ```

### Rollback Steps

If migration fails:

1. **Restore SQL Server backup**
   ```sql
   RESTORE DATABASE posdb FROM DISK = 'C:\Backups\posdb_pre_migration.bak'
   ```

2. **Revert code changes**
   ```bash
   git checkout main
   git branch -D feature/postgresql-migration
   ```

3. **Redeploy original application**

### Parallel Running (Recommended)

For zero-downtime migration:

1. Keep SQL Server running during transition
2. Deploy PostgreSQL alongside
3. Migrate data
4. Test thoroughly with subset of terminals
5. Gradually switch all terminals
6. Decommission SQL Server after validation period (1-2 weeks)

---

## 12. Post-Migration Checklist

### Immediate (Day 1)

- [ ] All terminals can connect to PostgreSQL
- [ ] Login/logout works
- [ ] Sales transactions complete successfully
- [ ] Receipts print correctly
- [ ] End-of-day reports generate
- [ ] Database backups configured

### Short-Term (Week 1)

- [ ] Performance acceptable under normal load
- [ ] No data integrity issues
- [ ] Audit logs capturing correctly
- [ ] All scheduled jobs running
- [ ] Error logging working

### Long-Term (Month 1)

- [ ] Database size growth as expected
- [ ] Backup/restore tested
- [ ] Disaster recovery plan documented
- [ ] Old SQL Server decommissioned
- [ ] Documentation updated

### PostgreSQL Maintenance Setup

```sql
-- Enable automatic vacuuming (usually default)
-- Check current settings
SHOW autovacuum;

-- Create maintenance schedule
-- Add to cron/Task Scheduler:

-- Daily: VACUUM ANALYZE
-- Weekly: REINDEX DATABASE posdb
-- Monthly: Full backup verification
```

### Monitoring Queries

```sql
-- Check database size
SELECT pg_size_pretty(pg_database_size('posdb'));

-- Check table sizes
SELECT
    relname as table_name,
    pg_size_pretty(pg_total_relation_size(relid)) as total_size
FROM pg_catalog.pg_statio_user_tables
ORDER BY pg_total_relation_size(relid) DESC;

-- Check active connections
SELECT count(*) FROM pg_stat_activity WHERE datname = 'posdb';

-- Check slow queries (requires pg_stat_statements extension)
SELECT query, calls, mean_time, total_time
FROM pg_stat_statements
ORDER BY mean_time DESC
LIMIT 10;
```

---

## Appendix A: Quick Reference Commands

### EF Core Commands

```bash
# Add migration
dotnet ef migrations add MigrationName --startup-project ../HospitalityPOS.WPF

# Update database
dotnet ef database update --startup-project ../HospitalityPOS.WPF

# Generate SQL script
dotnet ef migrations script --startup-project ../HospitalityPOS.WPF -o script.sql

# Remove last migration
dotnet ef migrations remove --startup-project ../HospitalityPOS.WPF

# List migrations
dotnet ef migrations list --startup-project ../HospitalityPOS.WPF
```

### PostgreSQL Commands

```bash
# Connect to database
psql -h localhost -U posuser -d posdb

# List databases
\l

# List tables
\dt

# Describe table
\d products

# Exit
\q
```

### Connection String Builder

```csharp
var builder = new NpgsqlConnectionStringBuilder
{
    Host = "192.168.1.100",
    Port = 5432,
    Database = "posdb",
    Username = "posuser",
    Password = "password",
    Pooling = true,
    MinPoolSize = 5,
    MaxPoolSize = 100,
    ConnectionIdleLifetime = 300
};

string connectionString = builder.ConnectionString;
```

---

## Appendix B: Troubleshooting

### Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| Connection refused | PostgreSQL not running | Start PostgreSQL service |
| Authentication failed | Wrong credentials | Verify username/password |
| Database does not exist | Database not created | Run CREATE DATABASE |
| Permission denied | User lacks privileges | GRANT permissions |
| Connection timeout | Firewall blocking | Open port 5432 |
| SSL required | Server configured for SSL | Add `SSL Mode=Require` to connection string |

### Logs Location

- **Windows**: `C:\Program Files\PostgreSQL\16\data\log\`
- **Linux**: `/var/log/postgresql/`

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-01-24 | AI Assistant | Initial creation |

---

## References

- [Npgsql EF Core Provider Documentation](https://www.npgsql.org/efcore/)
- [PostgreSQL Official Documentation](https://www.postgresql.org/docs/)
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [PostgreSQL Connection Strings](https://www.connectionstrings.com/postgresql/)
