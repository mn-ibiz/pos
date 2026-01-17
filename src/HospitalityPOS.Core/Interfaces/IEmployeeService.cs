using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for employee management operations.
/// </summary>
public interface IEmployeeService
{
    // Employee CRUD
    Task<Employee> CreateEmployeeAsync(Employee employee, CancellationToken cancellationToken = default);
    Task<Employee?> GetEmployeeByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Employee?> GetEmployeeByNumberAsync(string employeeNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Employee>> GetAllEmployeesAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Employee>> SearchEmployeesAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<Employee> UpdateEmployeeAsync(Employee employee, CancellationToken cancellationToken = default);
    Task<Employee> TerminateEmployeeAsync(int employeeId, DateTime terminationDate, string? reason = null, CancellationToken cancellationToken = default);
    Task<Employee> ReactivateEmployeeAsync(int employeeId, CancellationToken cancellationToken = default);

    // Employee number generation
    Task<string> GenerateEmployeeNumberAsync(CancellationToken cancellationToken = default);

    // Employee statistics
    Task<int> GetActiveEmployeeCountAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Employee>> GetEmployeesByDepartmentAsync(string department, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalMonthlyPayrollAsync(CancellationToken cancellationToken = default);

    // Link to User
    Task<Employee> LinkToUserAsync(int employeeId, int userId, CancellationToken cancellationToken = default);
    Task<Employee> UnlinkFromUserAsync(int employeeId, CancellationToken cancellationToken = default);

    // Salary components
    Task<IReadOnlyList<SalaryComponent>> GetAllSalaryComponentsAsync(CancellationToken cancellationToken = default);
    Task<SalaryComponent> CreateSalaryComponentAsync(SalaryComponent component, CancellationToken cancellationToken = default);
    Task<SalaryComponent> UpdateSalaryComponentAsync(SalaryComponent component, CancellationToken cancellationToken = default);
    Task<bool> DeleteSalaryComponentAsync(int componentId, CancellationToken cancellationToken = default);

    // Employee salary configuration
    Task<IReadOnlyList<EmployeeSalaryComponent>> GetEmployeeSalaryComponentsAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<EmployeeSalaryComponent> AddEmployeeSalaryComponentAsync(EmployeeSalaryComponent component, CancellationToken cancellationToken = default);
    Task<EmployeeSalaryComponent> UpdateEmployeeSalaryComponentAsync(EmployeeSalaryComponent component, CancellationToken cancellationToken = default);
    Task<bool> RemoveEmployeeSalaryComponentAsync(int componentId, CancellationToken cancellationToken = default);
}
