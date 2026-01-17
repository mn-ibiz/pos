# Story 18.1: eTIMS Control Unit Registration

## Story
**As an** administrator,
**I want to** register and activate the eTIMS Control Unit,
**So that** the POS can communicate with KRA servers.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/EtimsService.cs` (896 lines) - Device registration with:
  - `RegisterDeviceAsync` - Register eTIMS control unit
  - `GetActiveDeviceAsync` / `GetAllDevicesAsync` - Device retrieval
  - `ActivateDeviceAsync` / `DeactivateDeviceAsync` - Device status
  - `TestDeviceConnectionAsync` - Connection testing

## Epic
**Epic 18: Kenya eTIMS Compliance (MANDATORY)**

## Context
Kenya Revenue Authority (KRA) mandates that all VAT-registered businesses use the Electronic Tax Invoice Management System (eTIMS). Each POS installation requires a registered Control Unit (CU) with a unique Control Unit Number (CUN) assigned by KRA. This story implements the configuration and activation of the eTIMS integration.

## Acceptance Criteria

### AC1: Control Unit Configuration
**Given** the system is being set up
**When** configuring eTIMS settings
**Then** admin can enter:
- Control Unit Number (CUN) assigned by KRA
- Business KRA PIN (e.g., P051234567X)
- eTIMS API base URL (production/sandbox)
- OAuth credentials for API authentication

### AC2: Control Unit Activation
**Given** CUN and credentials are entered
**When** activating with KRA
**Then**:
- System sends activation request to KRA eTIMS API
- Receives and stores Control Unit Serial Number
- Displays activation success/failure message
- Stores activation timestamp

### AC3: eTIMS Status Display
**Given** activation is complete
**When** viewing eTIMS status
**Then** shows:
- Control Unit Serial Number
- Activation status (Active/Inactive)
- Last successful sync time
- API connection status (Green/Red indicator)

### AC4: Configuration Validation
**Given** eTIMS configuration is entered
**When** saving settings
**Then**:
- Validates CUN format
- Validates KRA PIN format (11 characters)
- Tests API connectivity
- Shows validation errors if any

## Technical Notes

### Implementation Details
```csharp
public class ETimsConfiguration
{
    public string ControlUnitNumber { get; set; }
    public string ControlUnitSerial { get; set; }
    public string BusinessPin { get; set; }
    public string ApiBaseUrl { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public ETimsStatus Status { get; set; }
}

public interface IETimsService
{
    Task<ActivationResult> ActivateControlUnitAsync(string cun, string pin);
    Task<bool> TestConnectionAsync();
    ETimsStatus GetCurrentStatus();
}
```

### API Endpoints
- KRA eTIMS Authentication: `/api/oauth/token`
- Control Unit Activation: `/api/cu/activate`
- Status Check: `/api/cu/status`

### Security Considerations
- OAuth credentials must be encrypted at rest
- API keys stored in secure configuration
- Audit log activation attempts

## Dependencies
- Epic 1: Foundation & Infrastructure (database, configuration)
- Epic 2: User Authentication (admin permissions)

## Files to Create/Modify
- `HospitalityPOS.Core/Entities/ETimsConfiguration.cs`
- `HospitalityPOS.Core/Interfaces/IETimsService.cs`
- `HospitalityPOS.Infrastructure/Services/ETimsService.cs`
- `HospitalityPOS.WPF/ViewModels/Admin/ETimsConfigViewModel.cs`
- `HospitalityPOS.WPF/Views/Admin/ETimsConfigView.xaml`
- Database migration for ETimsConfiguration table

## Testing Requirements
- Unit tests for configuration validation
- Integration tests with eTIMS sandbox
- UI tests for configuration screen

## Definition of Done
- [ ] eTIMS configuration UI implemented
- [ ] Control Unit activation workflow complete
- [ ] Status dashboard shows connection status
- [ ] OAuth token management working
- [ ] Unit tests passing
- [ ] Integration tests with sandbox passing
- [ ] Code reviewed and approved
