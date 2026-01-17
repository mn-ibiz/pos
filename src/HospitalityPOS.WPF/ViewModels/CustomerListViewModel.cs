using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for managing and displaying loyalty members.
/// </summary>
public partial class CustomerListViewModel : ViewModelBase
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly INavigationService _navigationService;
    private readonly IExportService _exportService;

    [ObservableProperty]
    private ObservableCollection<LoyaltyMemberDto> _members = new();

    [ObservableProperty]
    private LoyaltyMemberDto? _selectedMember;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private MembershipTier? _tierFilter;

    [ObservableProperty]
    private bool _showInactiveMembers;

    // Summary stats
    [ObservableProperty]
    private int _totalMembers;

    [ObservableProperty]
    private int _activeMembers;

    [ObservableProperty]
    private int _newMembersThisMonth;

    [ObservableProperty]
    private decimal _totalPointsOutstanding;

    public ObservableCollection<MembershipTier> TierOptions { get; } = new()
    {
        MembershipTier.Bronze,
        MembershipTier.Silver,
        MembershipTier.Gold,
        MembershipTier.Platinum
    };

    public bool CanEnrollCustomers => HasPermission("Loyalty.Enroll");
    public bool CanEditMembers => HasPermission("Loyalty.Member.Edit");
    public bool CanDeactivateMembers => HasPermission("Loyalty.Member.Deactivate");
    public bool CanExportData => HasPermission("Loyalty.Export");

    public CustomerListViewModel(ILogger logger)
        : base(logger)
    {
        Title = "Loyalty Members";
        _loyaltyService = App.Services.GetRequiredService<ILoyaltyService>();
        _navigationService = App.Services.GetRequiredService<INavigationService>();
        _exportService = App.Services.GetRequiredService<IExportService>();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            await RefreshMembersAsync();
            await LoadSummaryStatsAsync();
        }, "Loading members...");
    }

    private async Task RefreshMembersAsync()
    {
        Members.Clear();

        // If there's a search term, search; otherwise, get all
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var results = await _loyaltyService.SearchMembersAsync(SearchText, 100);
            foreach (var member in results)
            {
                if (ShouldIncludeMember(member))
                {
                    Members.Add(member);
                }
            }
        }
        else
        {
            // In a real implementation, this would paginate
            // For now, search with empty term returns recent members
            var results = await _loyaltyService.SearchMembersAsync("", 200);
            foreach (var member in results)
            {
                if (ShouldIncludeMember(member))
                {
                    Members.Add(member);
                }
            }
        }
    }

    private bool ShouldIncludeMember(LoyaltyMemberDto member)
    {
        // Filter by tier if specified
        if (TierFilter.HasValue && member.Tier != TierFilter.Value)
            return false;

        // Filter by active status
        if (!ShowInactiveMembers && !member.IsActive)
            return false;

        return true;
    }

    private async Task LoadSummaryStatsAsync()
    {
        // Get all members for stats (in real implementation, use aggregation queries)
        var allMembers = await _loyaltyService.SearchMembersAsync("", 10000);
        var memberList = allMembers.ToList();

        TotalMembers = memberList.Count;
        ActiveMembers = memberList.Count(m => m.IsActive);
        NewMembersThisMonth = memberList.Count(m =>
            m.EnrolledAt.Year == DateTime.Now.Year &&
            m.EnrolledAt.Month == DateTime.Now.Month);
        TotalPointsOutstanding = memberList.Sum(m => m.PointsBalance);
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await ExecuteAsync(RefreshMembersAsync, "Searching...");
    }

    [RelayCommand]
    private async Task ApplyFiltersAsync()
    {
        await ExecuteAsync(RefreshMembersAsync, "Filtering...");
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        SearchText = string.Empty;
        TierFilter = null;
        ShowInactiveMembers = false;
        await ExecuteAsync(RefreshMembersAsync, "Loading...");
    }

    [RelayCommand]
    private void EnrollNewCustomer()
    {
        if (!RequirePermission("Loyalty.Enroll", "enroll customers"))
            return;

        _navigationService.NavigateTo<CustomerEnrollmentViewModel>();
    }

    [RelayCommand]
    private async Task ViewMemberDetailsAsync()
    {
        if (SelectedMember == null)
            return;

        // Get analytics for the selected member
        var analytics = await _loyaltyService.GetCustomerAnalyticsAsync(SelectedMember.Id);

        var tierProgress = await _loyaltyService.GetTierProgressAsync(SelectedMember.Id);

        var details = $"Name: {SelectedMember.Name ?? "(Not provided)"}\n" +
                      $"Phone: {SelectedMember.PhoneNumber}\n" +
                      $"Email: {SelectedMember.Email ?? "(Not provided)"}\n" +
                      $"Membership #: {SelectedMember.MembershipNumber}\n" +
                      $"Tier: {SelectedMember.Tier}\n" +
                      $"Points Balance: {SelectedMember.PointsBalance:N0}\n" +
                      $"Lifetime Points: {SelectedMember.LifetimePoints:N0}\n" +
                      $"Total Spend: KES {SelectedMember.TotalSpend:N2}\n" +
                      $"Visit Count: {SelectedMember.VisitCount}\n" +
                      $"Enrolled: {SelectedMember.EnrolledAt:d}\n" +
                      $"Last Visit: {SelectedMember.LastVisitAt?.ToString("d") ?? "Never"}\n" +
                      $"Status: {(SelectedMember.IsActive ? "Active" : "Inactive")}";

        if (analytics != null)
        {
            details += $"\n\nAverage Transaction: KES {analytics.AverageTransactionValue:N2}";
            details += $"\nEngagement Score: {analytics.EngagementScore}/100";
        }

        if (tierProgress != null && tierProgress.NextTier.HasValue)
        {
            details += $"\n\nProgress to {tierProgress.NextTier}: {tierProgress.ProgressPercent:N0}%";
            details += $"\nSpend needed: KES {tierProgress.SpendToNextTier:N0}";
        }

        await DialogService.ShowMessageAsync("Member Details", details);
    }

    [RelayCommand]
    private async Task ViewTransactionHistoryAsync()
    {
        if (SelectedMember == null)
            return;

        var transactions = await _loyaltyService.GetTransactionHistoryAsync(
            SelectedMember.Id,
            DateTime.Now.AddMonths(-3));

        var history = string.Join("\n",
            transactions.Take(20).Select(t =>
                $"{t.TransactionDate:d} - {t.TransactionType}: {t.Points:+#,##0;-#,##0} pts"));

        if (string.IsNullOrEmpty(history))
        {
            history = "No recent transactions.";
        }

        await DialogService.ShowMessageAsync(
            $"Transaction History - {SelectedMember.MembershipNumber}",
            history);
    }

    [RelayCommand]
    private async Task DeactivateMemberAsync()
    {
        if (SelectedMember == null)
            return;

        if (!RequirePermission("Loyalty.Member.Deactivate", "deactivate members"))
            return;

        var confirm = await DialogService.ShowConfirmationAsync(
            "Deactivate Member",
            $"Are you sure you want to deactivate {SelectedMember.Name ?? SelectedMember.PhoneNumber}?\n\n" +
            $"Their points ({SelectedMember.PointsBalance:N0}) will be preserved but they won't be able to earn or redeem.");

        if (!confirm)
            return;

        await ExecuteAsync(async () =>
        {
            var result = await _loyaltyService.DeactivateMemberAsync(
                SelectedMember.Id,
                SessionService.CurrentUserId ?? 0);

            if (result)
            {
                await RefreshMembersAsync();
                await DialogService.ShowMessageAsync("Success", "Member has been deactivated.");
            }
            else
            {
                ErrorMessage = "Failed to deactivate member.";
            }
        }, "Deactivating...");
    }

    [RelayCommand]
    private async Task ReactivateMemberAsync()
    {
        if (SelectedMember == null || SelectedMember.IsActive)
            return;

        if (!RequirePermission("Loyalty.Member.Deactivate", "manage member status"))
            return;

        await ExecuteAsync(async () =>
        {
            var result = await _loyaltyService.ReactivateMemberAsync(
                SelectedMember.Id,
                SessionService.CurrentUserId ?? 0);

            if (result)
            {
                await RefreshMembersAsync();
                await DialogService.ShowMessageAsync("Success", "Member has been reactivated.");
            }
            else
            {
                ErrorMessage = "Failed to reactivate member.";
            }
        }, "Reactivating...");
    }

    [RelayCommand]
    private async Task ExportToExcelAsync()
    {
        if (!RequirePermission("Loyalty.Export", "export customer data"))
            return;

        await ExecuteAsync(async () =>
        {
            var data = Members.Select(m => new
            {
                m.MembershipNumber,
                m.Name,
                m.PhoneNumber,
                m.Email,
                Tier = m.Tier.ToString(),
                m.PointsBalance,
                m.LifetimePoints,
                m.TotalSpend,
                m.VisitCount,
                EnrolledAt = m.EnrolledAt.ToString("yyyy-MM-dd"),
                LastVisitAt = m.LastVisitAt?.ToString("yyyy-MM-dd") ?? "",
                Status = m.IsActive ? "Active" : "Inactive"
            });

            await _exportService.ExportToExcelAsync(data, "LoyaltyMembers", "Loyalty Members");
        }, "Exporting...");
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        _navigationService.NavigateTo<LoyaltySettingsViewModel>();
    }

    partial void OnSearchTextChanged(string value)
    {
        // Debounce search
        _ = SearchAsync();
    }

    partial void OnTierFilterChanged(MembershipTier? value)
    {
        _ = ApplyFiltersAsync();
    }

    partial void OnShowInactiveMembersChanged(bool value)
    {
        _ = ApplyFiltersAsync();
    }
}
