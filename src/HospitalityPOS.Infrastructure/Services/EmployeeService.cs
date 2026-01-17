using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service implementation for employee management operations.
/// </summary>
public class EmployeeService : IEmployeeService
{
    private readonly POSDbContext _context;

    public EmployeeService(POSDbContext context)
    {
        _context = context;
    }

    public async Task<Employee> CreateEmployeeAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(employee.EmployeeNumber))
        {
            employee.EmployeeNumber = await GenerateEmployeeNumberAsync(cancellationToken);
        }

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync(cancellationToken);
        return employee;
    }

    public async Task<Employee?> GetEmployeeByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .Include(e => e.User)
            .Include(e => e.EmployeeSalaryComponents)
                .ThenInclude(esc => esc.SalaryComponent)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Employee?> GetEmployeeByNumberAsync(string employeeNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.EmployeeNumber == employeeNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<Employee>> GetAllEmployeesAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Employees.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(e => e.IsActive && e.TerminationDate == null);
        }

        return await query
            .Include(e => e.User)
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Employee>> SearchEmployeesAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var term = searchTerm.ToLower();
        return await _context.Employees
            .Where(e => e.FirstName.ToLower().Contains(term) ||
                       e.LastName.ToLower().Contains(term) ||
                       e.EmployeeNumber.ToLower().Contains(term) ||
                       (e.Phone != null && e.Phone.Contains(term)) ||
                       (e.Email != null && e.Email.ToLower().Contains(term)))
            .Include(e => e.User)
            .OrderBy(e => e.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Employee> UpdateEmployeeAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        _context.Employees.Update(employee);
        await _context.SaveChangesAsync(cancellationToken);
        return employee;
    }

    public async Task<Employee> TerminateEmployeeAsync(int employeeId, DateTime terminationDate, string? reason = null, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FindAsync([employeeId], cancellationToken)
            ?? throw new InvalidOperationException($"Employee with ID {employeeId} not found.");

        employee.TerminationDate = terminationDate;
        employee.IsActive = false;

        await _context.SaveChangesAsync(cancellationToken);
        return employee;
    }

    public async Task<Employee> ReactivateEmployeeAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FindAsync([employeeId], cancellationToken)
            ?? throw new InvalidOperationException($"Employee with ID {employeeId} not found.");

        employee.TerminationDate = null;
        employee.IsActive = true;

        await _context.SaveChangesAsync(cancellationToken);
        return employee;
    }

    public async Task<string> GenerateEmployeeNumberAsync(CancellationToken cancellationToken = default)
    {
        var lastEmployee = await _context.Employees
            .OrderByDescending(e => e.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var nextNumber = (lastEmployee?.Id ?? 0) + 1;
        return $"EMP{nextNumber:D5}";
    }

    public async Task<int> GetActiveEmployeeCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .CountAsync(e => e.IsActive && e.TerminationDate == null, cancellationToken);
    }

    public async Task<IReadOnlyList<Employee>> GetEmployeesByDepartmentAsync(string department, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .Where(e => e.Department == department && e.IsActive)
            .OrderBy(e => e.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalMonthlyPayrollAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .Where(e => e.IsActive && e.TerminationDate == null && e.PayFrequency == PayFrequency.Monthly)
            .SumAsync(e => e.BasicSalary, cancellationToken);
    }

    public async Task<Employee> LinkToUserAsync(int employeeId, int userId, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FindAsync([employeeId], cancellationToken)
            ?? throw new InvalidOperationException($"Employee with ID {employeeId} not found.");

        employee.UserId = userId;
        await _context.SaveChangesAsync(cancellationToken);
        return employee;
    }

    public async Task<Employee> UnlinkFromUserAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FindAsync([employeeId], cancellationToken)
            ?? throw new InvalidOperationException($"Employee with ID {employeeId} not found.");

        employee.UserId = null;
        await _context.SaveChangesAsync(cancellationToken);
        return employee;
    }

    public async Task<IReadOnlyList<SalaryComponent>> GetAllSalaryComponentsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SalaryComponents
            .Where(sc => sc.IsActive)
            .OrderBy(sc => sc.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<SalaryComponent> CreateSalaryComponentAsync(SalaryComponent component, CancellationToken cancellationToken = default)
    {
        _context.SalaryComponents.Add(component);
        await _context.SaveChangesAsync(cancellationToken);
        return component;
    }

    public async Task<SalaryComponent> UpdateSalaryComponentAsync(SalaryComponent component, CancellationToken cancellationToken = default)
    {
        _context.SalaryComponents.Update(component);
        await _context.SaveChangesAsync(cancellationToken);
        return component;
    }

    public async Task<bool> DeleteSalaryComponentAsync(int componentId, CancellationToken cancellationToken = default)
    {
        var component = await _context.SalaryComponents.FindAsync([componentId], cancellationToken);
        if (component == null) return false;

        // Soft delete
        component.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<EmployeeSalaryComponent>> GetEmployeeSalaryComponentsAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeSalaryComponents
            .Include(esc => esc.SalaryComponent)
            .Where(esc => esc.EmployeeId == employeeId && (esc.EffectiveTo == null || esc.EffectiveTo >= DateTime.Today))
            .OrderBy(esc => esc.SalaryComponent.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<EmployeeSalaryComponent> AddEmployeeSalaryComponentAsync(EmployeeSalaryComponent component, CancellationToken cancellationToken = default)
    {
        _context.EmployeeSalaryComponents.Add(component);
        await _context.SaveChangesAsync(cancellationToken);
        return component;
    }

    public async Task<EmployeeSalaryComponent> UpdateEmployeeSalaryComponentAsync(EmployeeSalaryComponent component, CancellationToken cancellationToken = default)
    {
        _context.EmployeeSalaryComponents.Update(component);
        await _context.SaveChangesAsync(cancellationToken);
        return component;
    }

    public async Task<bool> RemoveEmployeeSalaryComponentAsync(int componentId, CancellationToken cancellationToken = default)
    {
        var component = await _context.EmployeeSalaryComponents.FindAsync([componentId], cancellationToken);
        if (component == null) return false;

        // End the component rather than delete
        component.EffectiveTo = DateTime.Today;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
