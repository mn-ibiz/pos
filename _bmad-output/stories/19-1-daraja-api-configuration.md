# Story 19.1: Daraja API Configuration

## Story
**As an** administrator,
**I want to** configure M-Pesa Daraja API credentials,
**So that** the POS can initiate M-Pesa payments.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/MpesaService.cs` - Daraja API with:
  - `ConfigureAsync` - Set consumer key, secret, passkey, shortcode
  - `GetAccessTokenAsync` - OAuth token with caching
  - Environment switching (Sandbox/Production)
  - Encrypted credential storage

## Epic
**Epic 19: M-Pesa Daraja API Integration**

## Context
M-Pesa handles approximately 83% of mobile money transactions in Kenya. Integration with Safaricom's Daraja API enables STK Push (Lipa na M-Pesa), allowing the POS to trigger payment prompts directly on customers' phones for a seamless checkout experience.

## Acceptance Criteria

### AC1: Credential Configuration
**Given** Safaricom Developer Portal credentials
**When** configuring M-Pesa settings
**Then** admin can enter:
- Consumer Key
- Consumer Secret
- Passkey for Lipa na M-Pesa
- Business Shortcode (Paybill/Till number)
- Environment (Sandbox/Production)

### AC2: Connection Validation
**Given** credentials are entered
**When** testing connection
**Then**:
- System attempts OAuth token generation
- Displays success/failure message
- Shows token expiry time if successful
- Logs validation attempt

### AC3: Callback URL Configuration
**Given** configuration is complete
**When** callback URL is set
**Then**:
- URL is validated for HTTPS
- System can receive payment confirmations
- Callback endpoint is tested
- Fallback handling configured

### AC4: Secure Storage
**Given** credentials are saved
**When** storing in database
**Then**:
- Consumer Secret is encrypted at rest
- Passkey is encrypted at rest
- Only admins with specific permission can view/edit
- Audit log entry created

## Technical Notes

### Implementation Details
```csharp
public class MPesaConfiguration
{
    public Guid Id { get; set; }
    public string ConsumerKey { get; set; }
    public string ConsumerSecretEncrypted { get; set; }
    public string PasskeyEncrypted { get; set; }
    public string BusinessShortcode { get; set; }
    public MPesaEnvironment Environment { get; set; }
    public string CallbackUrl { get; set; }
    public string TimeoutUrl { get; set; }
    public DateTime? LastValidated { get; set; }
    public bool IsActive { get; set; }
}

public enum MPesaEnvironment
{
    Sandbox,
    Production
}

public interface IMPesaConfigService
{
    Task<MPesaConfiguration> GetConfigurationAsync();
    Task SaveConfigurationAsync(MPesaConfiguration config);
    Task<ValidationResult> ValidateCredentialsAsync();
    Task<string> GetAccessTokenAsync();
}
```

### OAuth Token Management
```csharp
public class MPesaAuthService
{
    private string _cachedToken;
    private DateTime _tokenExpiry;

    public async Task<string> GetAccessTokenAsync()
    {
        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
            return _cachedToken;

        var config = await _configService.GetConfigurationAsync();
        var credentials = $"{config.ConsumerKey}:{Decrypt(config.ConsumerSecretEncrypted)}";
        var encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

        var request = new HttpRequestMessage(HttpMethod.Get, GetAuthUrl(config.Environment));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);

        var response = await _httpClient.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<OAuthResponse>();

        _cachedToken = result.AccessToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(result.ExpiresIn - 60); // Buffer

        return _cachedToken;
    }

    private string GetAuthUrl(MPesaEnvironment env) => env switch
    {
        MPesaEnvironment.Sandbox => "https://sandbox.safaricom.co.ke/oauth/v1/generate?grant_type=client_credentials",
        MPesaEnvironment.Production => "https://api.safaricom.co.ke/oauth/v1/generate?grant_type=client_credentials",
        _ => throw new ArgumentException("Invalid environment")
    };
}
```

## Dependencies
- Epic 1: Foundation & Infrastructure
- Epic 2: User Authentication (admin permissions)

## Files to Create/Modify
- `HospitalityPOS.Core/Entities/MPesaConfiguration.cs`
- `HospitalityPOS.Core/Interfaces/IMPesaConfigService.cs`
- `HospitalityPOS.Infrastructure/Services/MPesaAuthService.cs`
- `HospitalityPOS.WPF/ViewModels/Admin/MPesaConfigViewModel.cs`
- `HospitalityPOS.WPF/Views/Admin/MPesaConfigView.xaml`
- Database migration for MPesaConfiguration table

## Testing Requirements
- Unit tests for token management
- Integration tests with Daraja sandbox
- Tests for credential encryption
- UI tests for configuration screen

## Definition of Done
- [ ] Configuration UI implemented
- [ ] OAuth token generation working
- [ ] Credentials encrypted at rest
- [ ] Connection validation working
- [ ] Sandbox integration tested
- [ ] Unit tests passing
- [ ] Code reviewed and approved
