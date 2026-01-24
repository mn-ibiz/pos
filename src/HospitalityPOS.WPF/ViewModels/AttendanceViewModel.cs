using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalityPOS.WPF.ViewModels;

public partial class AttendanceViewModel : ObservableObject, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private int? _employeeId;

    [ObservableProperty]
    private string _title = "Attendance";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _startDate = DateTime.Today.AddDays(-30);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    [ObservableProperty]
    private ObservableCollection<Attendance> _attendanceRecords = [];

    [ObservableProperty]
    private Attendance? _selectedRecord;

    [ObservableProperty]
    private ObservableCollection<Employee> _employees = [];

    [ObservableProperty]
    private Employee? _selectedEmployee;

    [ObservableProperty]
    private ObservableCollection<AttendanceSummary> _summaries = [];

    // Today's attendance status for quick clock in/out
    [ObservableProperty]
    private Attendance? _todayAttendance;

    [ObservableProperty]
    private bool _isClockedIn;

    [ObservableProperty]
    private bool _isOnBreak;

    public AttendanceViewModel(
        INavigationService navigationService,
        IDialogService dialogService)
    {
        _navigationService = navigationService;
        _dialogService = dialogService;
    }

    public async Task InitializeAsync(int? employeeId = null)
    {
        _employeeId = employeeId;

        if (employeeId.HasValue)
        {
            await LoadEmployeeAsync(employeeId.Value);
            Title = $"Attendance - {SelectedEmployee?.FullName}";
        }
        else
        {
            await LoadEmployeesAsync();
            Title = "Attendance Management";
        }

        await LoadAttendanceAsync();
    }

    #region INavigationAware

    public void OnNavigatedTo(object? parameter)
    {
        int? employeeId = parameter switch
        {
            int id => id,
            Employee emp => emp.Id,
            _ => null
        };

        _ = InitializeAsync(employeeId);
    }

    public void OnNavigatedFrom()
    {
        // Nothing to clean up
    }

    #endregion

    private async Task LoadEmployeeAsync(int employeeId)
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var employeeService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();
            var employee = await employeeService.GetEmployeeByIdAsync(employeeId);
            if (employee != null)
            {
                SelectedEmployee = employee;
                Employees = new ObservableCollection<Employee> { employee };
            }
        }
        catch
        {
            // Handle error
        }
    }

    private async Task LoadEmployeesAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var employeeService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();
            var employees = await employeeService.GetAllEmployeesAsync();
            Employees = new ObservableCollection<Employee>(employees);

            if (Employees.Any())
            {
                SelectedEmployee = Employees.First();
            }
        }
        catch
        {
            // Handle error
        }
    }

    [RelayCommand]
    private async Task LoadAttendanceAsync()
    {
        if (SelectedEmployee == null) return;

        try
        {
            IsLoading = true;

            using var scope = App.Services.CreateScope();
            var attendanceService = scope.ServiceProvider.GetRequiredService<IAttendanceService>();

            var records = await attendanceService.GetEmployeeAttendanceAsync(
                SelectedEmployee.Id, StartDate, EndDate);
            AttendanceRecords = new ObservableCollection<Attendance>(records);

            // Load today's status
            TodayAttendance = await attendanceService.GetTodayAttendanceAsync(SelectedEmployee.Id);
            IsClockedIn = TodayAttendance?.ClockIn.HasValue == true && !TodayAttendance?.ClockOut.HasValue == true;
            IsOnBreak = TodayAttendance?.BreakStart.HasValue == true && !TodayAttendance?.BreakEnd.HasValue == true;

            // Load summary
            var summary = await attendanceService.GetEmployeeAttendanceSummaryAsync(
                SelectedEmployee.Id, StartDate, EndDate);
            Summaries = new ObservableCollection<AttendanceSummary> { summary };
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ClockInAsync()
    {
        if (SelectedEmployee == null) return;

        try
        {
            using var scope = App.Services.CreateScope();
            var attendanceService = scope.ServiceProvider.GetRequiredService<IAttendanceService>();

            await attendanceService.ClockInAsync(SelectedEmployee.Id);
            await _dialogService.ShowSuccessAsync("Clocked in successfully.");
            await LoadAttendanceAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Error clocking in: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ClockOutAsync()
    {
        if (SelectedEmployee == null) return;

        try
        {
            using var scope = App.Services.CreateScope();
            var attendanceService = scope.ServiceProvider.GetRequiredService<IAttendanceService>();

            await attendanceService.ClockOutAsync(SelectedEmployee.Id);
            await _dialogService.ShowSuccessAsync("Clocked out successfully.");
            await LoadAttendanceAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Error clocking out: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task StartBreakAsync()
    {
        if (SelectedEmployee == null) return;

        try
        {
            using var scope = App.Services.CreateScope();
            var attendanceService = scope.ServiceProvider.GetRequiredService<IAttendanceService>();

            await attendanceService.StartBreakAsync(SelectedEmployee.Id);
            await _dialogService.ShowSuccessAsync("Break started.");
            await LoadAttendanceAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Error starting break: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task EndBreakAsync()
    {
        if (SelectedEmployee == null) return;

        try
        {
            using var scope = App.Services.CreateScope();
            var attendanceService = scope.ServiceProvider.GetRequiredService<IAttendanceService>();

            await attendanceService.EndBreakAsync(SelectedEmployee.Id);
            await _dialogService.ShowSuccessAsync("Break ended.");
            await LoadAttendanceAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Error ending break: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task AddManualEntryAsync()
    {
        if (SelectedEmployee == null) return;

        try
        {
            var result = await _dialogService.ShowManualAttendanceEntryDialogAsync(SelectedEmployee);
            if (result == null) return;

            using var scope = App.Services.CreateScope();
            var attendanceService = scope.ServiceProvider.GetRequiredService<IAttendanceService>();
            var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();

            var request = new HospitalityPOS.Core.Models.HR.MissedPunchRequest
            {
                EmployeeId = SelectedEmployee.Id,
                Date = DateOnly.FromDateTime(result.PunchTime),
                PunchType = result.EntryType,
                PunchTime = result.PunchTime,
                ManagerUserId = sessionService.CurrentUser?.Id ?? 0,
                ManagerPin = result.ManagerPin,
                Reason = string.IsNullOrWhiteSpace(result.Notes)
                    ? result.Reason
                    : $"{result.Reason} - {result.Notes}"
            };

            var clockResult = await attendanceService.AddMissedPunchAsync(request);

            if (clockResult.Success)
            {
                await _dialogService.ShowSuccessAsync("Manual Entry Added",
                    $"Manual {result.EntryType} entry has been added for {SelectedEmployee.FullName}.");
                await LoadAttendanceAsync();
            }
            else
            {
                await _dialogService.ShowErrorAsync("Failed to Add Entry", clockResult.Message ?? "Unknown error occurred.");
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Failed to add manual entry: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ViewAllSummariesAsync()
    {
        try
        {
            IsLoading = true;

            using var scope = App.Services.CreateScope();
            var attendanceService = scope.ServiceProvider.GetRequiredService<IAttendanceService>();

            var summaries = await attendanceService.GetAttendanceSummaryByDateRangeAsync(StartDate, EndDate);
            Summaries = new ObservableCollection<AttendanceSummary>(summaries);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    partial void OnSelectedEmployeeChanged(Employee? value)
    {
        if (value != null)
        {
            _ = LoadAttendanceAsync();
        }
    }
}
