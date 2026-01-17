# Story 16.1: Employee Management

Status: done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/EmployeeService.cs` (227 lines) - Employee management with:
  - `CreateEmployeeAsync` / `UpdateEmployeeAsync` - CRUD operations
  - `GetEmployeeByIdAsync` / `GetEmployeeByNumberAsync` - Retrieval
  - `SearchEmployeesAsync` - Search by name/number
  - `TerminateEmployeeAsync` / `ReactivateEmployeeAsync` - Employment status
  - `LinkToUserAsync` / `UnlinkFromUserAsync` - User account linking

## Story

As an HR manager,
I want to maintain employee records,
So that I have all staff information in one place.

## Acceptance Criteria

1. **Given** access to Employee module
   **When** creating an employee
   **Then** can enter: name, ID, contact, position, salary, bank details, statutory IDs

2. **Given** an employee exists
   **When** editing record
   **Then** changes are saved with audit trail

3. **Given** employee leaves
   **When** terminating employment
   **Then** record is deactivated with termination date, not deleted

## Tasks / Subtasks

- [ ] Task 1: Create Employee Entity
  - [ ] Create Employee class with all fields
  - [ ] Add EF Core configuration
  - [ ] Create migration
  - [ ] Link to Users table (optional)

- [ ] Task 2: Create Employee List View
  - [ ] Create EmployeesView.xaml
  - [ ] Create EmployeesViewModel
  - [ ] Display employee grid
  - [ ] Search and filter

- [ ] Task 3: Create Employee Editor
  - [ ] Create EmployeeEditorView.xaml
  - [ ] Personal details tab
  - [ ] Employment details tab
  - [ ] Salary & banking tab

- [ ] Task 4: Implement Employee Actions
  - [ ] Create new employee
  - [ ] Edit employee
  - [ ] Terminate employment
  - [ ] View employment history

## Dev Notes

### Employee Entity

```csharp
public class Employee
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;
    public decimal BasicSalary { get; set; }
    public PayFrequency PayFrequency { get; set; } = PayFrequency.Monthly;
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? TaxId { get; set; }       // KRA PIN
    public string? NssfNumber { get; set; }
    public string? NhifNumber { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum EmploymentType { FullTime, PartTime, Contract }
public enum PayFrequency { Weekly, BiWeekly, Monthly }
```

## Dev Agent Record

### Agent Model Used
{{agent_model_name_version}}

### Completion Notes List

### File List
