# Story 27.1: KDS Station Configuration

## Story
**As an** administrator,
**I want to** configure KDS stations for different kitchen areas,
**So that** orders route to the correct preparation stations.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 27: Kitchen Display System (KDS)**

## Acceptance Criteria

### AC1: Station Definition
**Given** KDS setup
**When** configuring stations
**Then** can define: station name, display device, product categories

### AC2: Category Routing
**Given** multiple stations exist
**When** assigning categories
**Then** categories route to specific stations (Hot, Cold, Bar, Dessert)

### AC3: Station Activation
**Given** station configuration
**When** saving
**Then** KDS displays activate with assigned categories

## Technical Notes
```csharp
public class KdsStation
{
    public Guid Id { get; set; }
    public string Name { get; set; }  // "Hot Line", "Cold Station", "Bar", etc.
    public string DeviceIdentifier { get; set; }  // IP address or hostname
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsExpo { get; set; } = false;  // Expo sees all stations
    public KdsDisplaySettings DisplaySettings { get; set; }
    public List<KdsStationCategory> Categories { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class KdsStationCategory
{
    public Guid Id { get; set; }
    public Guid StationId { get; set; }
    public KdsStation Station { get; set; }
    public Guid CategoryId { get; set; }
    public Category Category { get; set; }
}

public class KdsDisplaySettings
{
    public int ColumnsCount { get; set; } = 4;
    public int FontSize { get; set; } = 16;
    public int WarningThresholdMinutes { get; set; } = 10;
    public int AlertThresholdMinutes { get; set; } = 15;
    public bool ShowModifiers { get; set; } = true;
    public bool AudioAlerts { get; set; } = true;
}

public interface IKdsStationService
{
    Task<KdsStation> CreateStationAsync(KdsStationDto station);
    Task<KdsStation> UpdateStationAsync(Guid id, KdsStationDto station);
    Task<bool> DeleteStationAsync(Guid id);
    Task<List<KdsStation>> GetAllStationsAsync();
    Task<KdsStation> GetStationByDeviceAsync(string deviceIdentifier);
    Task AssignCategoryAsync(Guid stationId, Guid categoryId);
    Task RemoveCategoryAsync(Guid stationId, Guid categoryId);
}
```

## Definition of Done
- [x] KdsStation entity and database table
- [x] KdsStationCategory junction table
- [x] KdsDisplaySettings configuration
- [x] Station CRUD operations
- [x] Category-to-station assignment
- [x] Device identifier validation
- [x] Station activation/deactivation
- [x] Unit tests passing

## Implementation Summary

### Entities Created (KdsEntities.cs)
- **KdsStation**: Core station entity with Name, DeviceIdentifier, DisplayOrder, IsExpo, StoreId, Status, and navigation to DisplaySettings
- **KdsStationCategory**: Junction table linking stations to categories with priority routing
- **KdsDisplaySettings**: Station display configuration (ColumnsCount, FontSize, thresholds, ShowModifiers, AudioAlerts)
- **KdsStationStatus enum**: Online, Offline, Maintenance

### DTOs Created (KdsDtos.cs)
- KdsStationDto, CreateKdsStationDto, UpdateKdsStationDto
- KdsStationCategoryDto, AssignCategoryDto, KdsDisplaySettingsDto, UpdateDisplaySettingsDto
- StationStatisticsDto for performance metrics

### Service Implementation (KdsStationService.cs ~550 lines)
- Full CRUD operations with validation
- Station status management (Online/Offline/Maintenance)
- Category assignment and removal
- Display settings management
- Device identifier validation (IP format)
- Station statistics (order counts, completion rates)
- Event-driven notifications (StationOnline, StationOffline, StationCategoriesUpdated)

### Unit Tests (KdsStationServiceTests.cs ~25 tests)
- Constructor null argument validation
- CreateStation tests (valid/duplicate name)
- GetStation tests (valid/invalid ID)
- SetStationOnline/Offline tests
- AssignCategory/RemoveCategory tests
- DeleteStation tests
- DisplaySettings tests
