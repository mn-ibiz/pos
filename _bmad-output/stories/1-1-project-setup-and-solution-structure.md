# Story 1.1: Project Setup and Solution Structure

Status: done

## Story

As a developer,
I want a properly structured C# solution with layered architecture,
So that the codebase is maintainable, testable, and follows best practices.

## Acceptance Criteria

1. **Given** a new development environment
   **When** the solution is created
   **Then** it should have the following projects:
   - HospitalityPOS.Core (domain entities, interfaces)
   - HospitalityPOS.Infrastructure (data access, EF Core)
   - HospitalityPOS.Business (services, business logic)
   - HospitalityPOS.WPF (Windows desktop application)

2. **Given** the solution is created
   **When** projects are configured
   **Then** all projects should target .NET 10 (LTS)

3. **Given** the solution is created
   **When** dependencies are configured
   **Then** NuGet packages should include:
   - Microsoft.EntityFrameworkCore (10.0.0)
   - Microsoft.EntityFrameworkCore.SqlServer (10.0.0)
   - BCrypt.Net-Next (4.0+)
   - Serilog (4.0+)
   - CommunityToolkit.Mvvm (8.4+)

4. **Given** all projects exist
   **When** project references are configured
   **Then** dependencies should follow:
   - Core has no project dependencies
   - Infrastructure references Core
   - Business references Core and Infrastructure
   - WPF references all projects

## Tasks / Subtasks

- [x] Task 1: Create Visual Studio Solution (AC: #1)
  - [x] Create new blank solution named "HospitalityPOS"
  - [x] Create HospitalityPOS.Core class library project
  - [x] Create HospitalityPOS.Infrastructure class library project
  - [x] Create HospitalityPOS.Business class library project
  - [x] Create HospitalityPOS.WPF WPF Application project

- [x] Task 2: Configure Target Framework (AC: #2)
  - [x] Set all projects to target net10.0-windows
  - [x] Enable nullable reference types
  - [x] Configure C# language version to 14 (default in .NET 10)

- [x] Task 3: Add NuGet Packages (AC: #3)
  - [x] Add EF Core packages to Infrastructure project
  - [x] Add BCrypt.Net-Next to Infrastructure project
  - [x] Add Serilog packages to all projects
  - [x] Add CommunityToolkit.Mvvm to WPF project
  - [x] Add Microsoft.Extensions.DependencyInjection to WPF project

- [x] Task 4: Configure Project References (AC: #4)
  - [x] Add Core reference to Infrastructure
  - [x] Add Core and Infrastructure references to Business
  - [x] Add all project references to WPF
  - [x] Verify no circular dependencies

- [x] Task 5: Create Base Folder Structure
  - [x] Create Entities, Interfaces, Enums, DTOs folders in Core
  - [x] Create Data, Repositories, Printing folders in Infrastructure
  - [x] Create Services, Validators folders in Business
  - [x] Create Views, ViewModels, Controls, Resources folders in WPF

## Dev Notes

### Architecture Pattern
This project follows a layered architecture pattern:
- **Core**: Contains domain entities, interfaces, enums, and DTOs. Zero dependencies on other projects.
- **Infrastructure**: Implements data access using EF Core, handles printing, and hardware integration.
- **Business**: Contains all business logic in service classes.
- **WPF**: The presentation layer using MVVM pattern.

### Key NuGet Packages

| Package | Version | Purpose | Project |
|---------|---------|---------|---------|
| Microsoft.EntityFrameworkCore | 10.0.0 | ORM with LeftJoin/RightJoin, named filters | Infrastructure |
| Microsoft.EntityFrameworkCore.SqlServer | 10.0.0 | SQL Server provider with JSON support | Infrastructure |
| Microsoft.EntityFrameworkCore.Tools | 10.0.0 | Migrations | Infrastructure |
| BCrypt.Net-Next | 4.0.3 | Password hashing | Infrastructure |
| Serilog | 4.1.0 | Structured logging | All |
| Serilog.Sinks.File | 6.0.0 | File logging | WPF |
| CommunityToolkit.Mvvm | 8.4.0 | MVVM framework | WPF |
| Microsoft.Extensions.DependencyInjection | 10.0.0 | DI container | WPF |
| Microsoft.Extensions.Hosting | 10.0.0 | Host builder | WPF |

### Project File Configuration - Core

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>14</LangVersion>
  </PropertyGroup>
</Project>
```

### Project File Configuration - WPF

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>14</LangVersion>
    <UseWPF>true</UseWPF>
    <ApplicationIcon>Resources\pos-icon.ico</ApplicationIcon>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.0" />
    <PackageReference Include="Serilog" Version="4.1.0" />
    <PackageReference Include="Serilog.Sinks.File" Version="6.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\HospitalityPOS.Core\HospitalityPOS.Core.csproj" />
    <ProjectReference Include="..\HospitalityPOS.Infrastructure\HospitalityPOS.Infrastructure.csproj" />
    <ProjectReference Include="..\HospitalityPOS.Business\HospitalityPOS.Business.csproj" />
  </ItemGroup>
</Project>
```

### Project File Configuration - Infrastructure

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>14</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
    <PackageReference Include="Serilog" Version="4.1.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\HospitalityPOS.Core\HospitalityPOS.Core.csproj" />
  </ItemGroup>
</Project>
```

### .NET 10 & C# 14 Features to Leverage

```csharp
// C# 14 - Field-backed properties in ViewModels
public partial class ProductViewModel : ObservableObject
{
    public string Name
    {
        get => field ?? string.Empty;
        set => SetProperty(ref field, value);
    }

    public decimal Price
    {
        get => field;
        set
        {
            if (value < 0) throw new ArgumentException("Price cannot be negative");
            SetProperty(ref field, value);
        }
    }
}

// C# 14 - Extension members for validation
public extension class MpesaValidation for string
{
    public bool IsValidMpesaCode => Length == 10 && All(char.IsLetterOrDigit);
}

// EF Core 10 - Named query filters
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>()
        .HasQueryFilter("SoftDelete", p => p.IsActive);

    modelBuilder.Entity<Order>()
        .HasQueryFilter("CurrentWorkPeriod", o => o.WorkPeriodId == _currentWorkPeriodId);
}

// EF Core 10 - LeftJoin for reports
var salesReport = await context.Products
    .LeftJoin(
        context.OrderItems.Where(oi => oi.Order.SettledAt >= startDate),
        p => p.Id,
        oi => oi.ProductId,
        (product, orderItem) => new { product, orderItem })
    .GroupBy(x => x.product)
    .Select(g => new ProductSalesReport
    {
        ProductName = g.Key.Name,
        QuantitySold = g.Sum(x => x.orderItem != null ? x.orderItem.Quantity : 0),
        TotalSales = g.Sum(x => x.orderItem != null ? x.orderItem.TotalAmount : 0)
    })
    .ToListAsync();
```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#9-Technical-Architecture]
- [Source: _bmad-output/architecture.md#Project-Structure]
- [Source: _bmad-output/project-context.md#Technology-Stack]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
- Created complete solution structure with 4 main projects and 3 test projects
- All projects configured for .NET 10 with C# 14 language features
- Implemented base interfaces (IEntity, IRepository, IUnitOfWork) in Core
- Created BaseEntity with soft-delete and audit support
- Implemented POSDbContext with automatic audit field updates
- Created generic Repository pattern implementation
- Implemented UnitOfWork for transaction management
- Created MVVM infrastructure with ViewModelBase
- Added touch-optimized WPF styles and resources
- Created comprehensive unit tests for all base classes
- Added global configuration files (.editorconfig, Directory.Build.props, global.json)
- Tests validate BaseEntity, Result DTOs, BaseService, and ViewModelBase

### File List
- HospitalityPOS.sln
- src/HospitalityPOS.WPF/appsettings.json
- tests/HospitalityPOS.Business.Tests/Repositories/RepositoryTests.cs
- global.json
- Directory.Build.props
- .editorconfig
- .gitignore
- src/HospitalityPOS.Core/HospitalityPOS.Core.csproj
- src/HospitalityPOS.Core/Entities/BaseEntity.cs
- src/HospitalityPOS.Core/Interfaces/IEntity.cs
- src/HospitalityPOS.Core/Interfaces/IRepository.cs
- src/HospitalityPOS.Core/Interfaces/IUnitOfWork.cs
- src/HospitalityPOS.Core/Enums/SystemEnums.cs
- src/HospitalityPOS.Core/DTOs/ResultDto.cs
- src/HospitalityPOS.Infrastructure/HospitalityPOS.Infrastructure.csproj
- src/HospitalityPOS.Infrastructure/Data/POSDbContext.cs
- src/HospitalityPOS.Infrastructure/Data/UnitOfWork.cs
- src/HospitalityPOS.Infrastructure/Repositories/Repository.cs
- src/HospitalityPOS.Infrastructure/Extensions/ServiceCollectionExtensions.cs
- src/HospitalityPOS.Business/HospitalityPOS.Business.csproj
- src/HospitalityPOS.Business/Services/BaseService.cs
- src/HospitalityPOS.Business/Extensions/ServiceCollectionExtensions.cs
- src/HospitalityPOS.WPF/HospitalityPOS.WPF.csproj
- src/HospitalityPOS.WPF/App.xaml
- src/HospitalityPOS.WPF/App.xaml.cs
- src/HospitalityPOS.WPF/Views/MainWindow.xaml
- src/HospitalityPOS.WPF/Views/MainWindow.xaml.cs
- src/HospitalityPOS.WPF/ViewModels/ViewModelBase.cs
- src/HospitalityPOS.WPF/Resources/Colors.xaml
- src/HospitalityPOS.WPF/Resources/Styles.xaml
- tests/HospitalityPOS.Core.Tests/HospitalityPOS.Core.Tests.csproj
- tests/HospitalityPOS.Core.Tests/Entities/BaseEntityTests.cs
- tests/HospitalityPOS.Core.Tests/DTOs/ResultTests.cs
- tests/HospitalityPOS.Business.Tests/HospitalityPOS.Business.Tests.csproj
- tests/HospitalityPOS.Business.Tests/Services/BaseServiceTests.cs
- tests/HospitalityPOS.WPF.Tests/HospitalityPOS.WPF.Tests.csproj
- tests/HospitalityPOS.WPF.Tests/ViewModels/ViewModelBaseTests.cs

## Senior Developer Review (AI)

**Review Date:** 2025-12-30
**Outcome:** Changes Requested → Auto-Fixed

### Action Items
- [x] [HIGH] Fix hardcoded connection string in App.xaml.cs - moved to appsettings.json
- [x] [HIGH] Fix invalid color hex code #5555770 in Colors.xaml → fixed to #555577
- [x] [HIGH] Add missing IRepository<T> DI registration - now calls AddInfrastructureServices()
- [x] [HIGH] Create appsettings.json for configuration
- [x] [MED] Add Repository<T> unit tests - created comprehensive test suite
- [x] [MED] Add QueryNoTracking() for read-only performance optimization
- [x] [MED] Add Configuration packages to WPF project
- [x] [MED] Configure appsettings.json to copy to output

### Summary
8 issues found and auto-fixed. All HIGH and MEDIUM issues addressed. Story ready for deployment.

## Change Log
- 2025-12-30: Code review complete - 8 issues fixed (4 HIGH, 4 MEDIUM)
- 2025-12-30: Initial implementation - All 5 tasks completed. Solution structure created with layered architecture, base classes, and unit tests.
