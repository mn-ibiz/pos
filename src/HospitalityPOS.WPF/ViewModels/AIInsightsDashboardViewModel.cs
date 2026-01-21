using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.AI;
using Microsoft.Extensions.Logging;

namespace HospitalityPOS.WPF.ViewModels;

public partial class AIInsightsDashboardViewModel : ObservableObject
{
    private readonly IAIInsightsService _aiInsightsService;
    private readonly ILogger<AIInsightsDashboardViewModel> _logger;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Loading AI insights...";

    [ObservableProperty]
    private SalesForecast? _todayForecast;

    [ObservableProperty]
    private decimal _currentDaySales;

    [ObservableProperty]
    private decimal _forecastVariance;

    [ObservableProperty]
    private Brush _varianceBrush = Brushes.White;

    [ObservableProperty]
    private string _performanceStatus = "Loading";

    [ObservableProperty]
    private string _performanceMessage = "";

    [ObservableProperty]
    private Brush _performanceStatusColor = Brushes.White;

    [ObservableProperty]
    private Brush _performanceStatusBackground = new SolidColorBrush(Color.FromRgb(39, 39, 42));

    [ObservableProperty]
    private int _criticalAlertCount;

    [ObservableProperty]
    private int _warningAlertCount;

    [ObservableProperty]
    private bool _hasCriticalAlerts;

    [ObservableProperty]
    private bool _hasWarningAlerts;

    [ObservableProperty]
    private string _chatInput = "";

    public ObservableCollection<AlertViewModel> Alerts { get; } = new();
    public ObservableCollection<InsightViewModel> Insights { get; } = new();
    public ObservableCollection<InventoryRecommendationViewModel> InventoryRecommendations { get; } = new();
    public ObservableCollection<ChatMessageViewModel> ChatMessages { get; } = new();

    public AIInsightsDashboardViewModel(
        IAIInsightsService aiInsightsService,
        ILogger<AIInsightsDashboardViewModel> logger)
    {
        _aiInsightsService = aiInsightsService;
        _logger = logger;

        // Add initial welcome message
        ChatMessages.Add(new ChatMessageViewModel
        {
            Message = "Hello! I'm your AI business assistant. Ask me anything about your sales, inventory, or customers.",
            IsUser = false,
            Background = new SolidColorBrush(Color.FromRgb(31, 31, 35)),
            Alignment = HorizontalAlignment.Left
        });

        _ = LoadDataAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(ChatInput)) return;

        var userMessage = ChatInput;
        ChatInput = "";

        // Add user message
        ChatMessages.Add(new ChatMessageViewModel
        {
            Message = userMessage,
            IsUser = true,
            Background = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
            Alignment = HorizontalAlignment.Right
        });

        try
        {
            // Get AI response
            var response = await _aiInsightsService.AnswerBusinessQuestionAsync(userMessage);

            ChatMessages.Add(new ChatMessageViewModel
            {
                Message = response.Answer,
                IsUser = false,
                Background = new SolidColorBrush(Color.FromRgb(31, 31, 35)),
                Alignment = HorizontalAlignment.Left
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AI response");
            ChatMessages.Add(new ChatMessageViewModel
            {
                Message = "I'm sorry, I encountered an error processing your question. Please try again.",
                IsUser = false,
                Background = new SolidColorBrush(Color.FromRgb(127, 29, 29)),
                Alignment = HorizontalAlignment.Left
            });
        }
    }

    [RelayCommand]
    private async Task AskQuestionAsync(string question)
    {
        ChatInput = question;
        await SendMessageAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Loading AI dashboard...";

            var summary = await _aiInsightsService.GetDashboardSummaryAsync();

            if (summary != null)
            {
                // Forecast
                TodayForecast = summary.TodaysForecast;
                CurrentDaySales = summary.CurrentDaySales ?? 0;
                ForecastVariance = summary.ForecastVariance ?? 0;

                UpdatePerformanceStatus(summary.PerformanceStatus);

                VarianceBrush = ForecastVariance >= 0
                    ? new SolidColorBrush(Color.FromRgb(34, 197, 94))
                    : new SolidColorBrush(Color.FromRgb(239, 68, 68));

                // Alerts
                Alerts.Clear();
                CriticalAlertCount = summary.CriticalAlertCount;
                WarningAlertCount = summary.WarningAlertCount;
                HasCriticalAlerts = CriticalAlertCount > 0;
                HasWarningAlerts = WarningAlertCount > 0;

                if (summary.TopAlerts != null)
                {
                    foreach (var alert in summary.TopAlerts.Take(5))
                    {
                        Alerts.Add(new AlertViewModel
                        {
                            Title = alert.Title,
                            Description = alert.Description,
                            Severity = alert.Severity.ToString(),
                            SeverityColor = GetSeverityColor(alert.Severity),
                            SeverityBackground = GetSeverityBackground(alert.Severity),
                            BackgroundColor = GetAlertBackground(alert.Severity),
                            Icon = GetAlertIcon(alert.AnomalyType),
                            IconBackground = GetSeverityBackground(alert.Severity)
                        });
                    }
                }

                // Insights
                Insights.Clear();
                if (summary.TopInsights != null)
                {
                    foreach (var insight in summary.TopInsights.Take(5))
                    {
                        Insights.Add(new InsightViewModel
                        {
                            Title = insight.Title,
                            Summary = insight.Summary,
                            Category = insight.Category.ToString(),
                            Recommendation = insight.Recommendation,
                            HasRecommendation = !string.IsNullOrEmpty(insight.Recommendation),
                            Impact = insight.Impact.ToString(),
                            ImpactColor = GetImpactColor(insight.Impact),
                            ImpactBackground = GetImpactBackground(insight.Impact),
                            Icon = GetInsightIcon(insight.InsightType),
                            IconBackground = GetCategoryBackground(insight.Category)
                        });
                    }
                }

                // Inventory recommendations
                InventoryRecommendations.Clear();
                if (summary.UrgentInventoryRecommendations != null)
                {
                    foreach (var rec in summary.UrgentInventoryRecommendations.Take(5))
                    {
                        InventoryRecommendations.Add(new InventoryRecommendationViewModel
                        {
                            ProductName = rec.ProductName,
                            DaysOfStockRemaining = rec.DaysOfStockRemaining,
                            RecommendedQuantity = rec.RecommendedQuantity,
                            Urgency = rec.Urgency.ToString(),
                            UrgencyColor = GetUrgencyColor(rec.Urgency),
                            UrgencyBackground = GetUrgencyBackground(rec.Urgency)
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading AI dashboard");
            StatusMessage = "Error loading insights";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdatePerformanceStatus(string status)
    {
        switch (status)
        {
            case "AboveTarget":
                PerformanceStatus = "Above Target";
                PerformanceMessage = "Sales exceeding forecast";
                PerformanceStatusColor = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                PerformanceStatusBackground = new SolidColorBrush(Color.FromArgb(30, 34, 197, 94));
                break;
            case "BelowTarget":
                PerformanceStatus = "Below Target";
                PerformanceMessage = "Sales below forecast";
                PerformanceStatusColor = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                PerformanceStatusBackground = new SolidColorBrush(Color.FromArgb(30, 239, 68, 68));
                break;
            default:
                PerformanceStatus = "On Track";
                PerformanceMessage = "Meeting expectations";
                PerformanceStatusColor = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                PerformanceStatusBackground = new SolidColorBrush(Color.FromArgb(30, 59, 130, 246));
                break;
        }
    }

    private static Brush GetSeverityColor(AnomalySeverity severity) => severity switch
    {
        AnomalySeverity.Critical => new SolidColorBrush(Color.FromRgb(239, 68, 68)),
        AnomalySeverity.High => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
        AnomalySeverity.Warning => new SolidColorBrush(Color.FromRgb(251, 191, 36)),
        _ => new SolidColorBrush(Color.FromRgb(59, 130, 246))
    };

    private static Brush GetSeverityBackground(AnomalySeverity severity) => severity switch
    {
        AnomalySeverity.Critical => new SolidColorBrush(Color.FromArgb(40, 239, 68, 68)),
        AnomalySeverity.High => new SolidColorBrush(Color.FromArgb(40, 245, 158, 11)),
        AnomalySeverity.Warning => new SolidColorBrush(Color.FromArgb(40, 251, 191, 36)),
        _ => new SolidColorBrush(Color.FromArgb(40, 59, 130, 246))
    };

    private static Brush GetAlertBackground(AnomalySeverity severity) => severity switch
    {
        AnomalySeverity.Critical => new SolidColorBrush(Color.FromRgb(69, 26, 3)),
        AnomalySeverity.High => new SolidColorBrush(Color.FromRgb(69, 26, 3)),
        _ => new SolidColorBrush(Color.FromRgb(31, 31, 35))
    };

    private static string GetAlertIcon(AnomalyType type) => type switch
    {
        AnomalyType.SalesAnomaly => "📊",
        AnomalyType.VoidAnomaly => "❌",
        AnomalyType.DiscountAnomaly => "🏷️",
        AnomalyType.CashVariance => "💰",
        AnomalyType.InventoryDiscrepancy => "📦",
        _ => "⚠️"
    };

    private static Brush GetImpactColor(InsightImpact impact) => impact switch
    {
        InsightImpact.Critical => new SolidColorBrush(Color.FromRgb(239, 68, 68)),
        InsightImpact.High => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
        InsightImpact.Medium => new SolidColorBrush(Color.FromRgb(59, 130, 246)),
        _ => new SolidColorBrush(Color.FromRgb(34, 197, 94))
    };

    private static Brush GetImpactBackground(InsightImpact impact) => impact switch
    {
        InsightImpact.Critical => new SolidColorBrush(Color.FromArgb(40, 239, 68, 68)),
        InsightImpact.High => new SolidColorBrush(Color.FromArgb(40, 245, 158, 11)),
        InsightImpact.Medium => new SolidColorBrush(Color.FromArgb(40, 59, 130, 246)),
        _ => new SolidColorBrush(Color.FromArgb(40, 34, 197, 94))
    };

    private static string GetInsightIcon(InsightType type) => type switch
    {
        InsightType.Opportunity => "💡",
        InsightType.Risk => "⚠️",
        InsightType.Trend => "📈",
        InsightType.Achievement => "🏆",
        InsightType.Recommendation => "💬",
        _ => "📊"
    };

    private static Brush GetCategoryBackground(InsightCategory category) => category switch
    {
        InsightCategory.Sales => new SolidColorBrush(Color.FromArgb(40, 34, 197, 94)),
        InsightCategory.Inventory => new SolidColorBrush(Color.FromArgb(40, 59, 130, 246)),
        InsightCategory.Customers => new SolidColorBrush(Color.FromArgb(40, 139, 92, 246)),
        InsightCategory.Employees => new SolidColorBrush(Color.FromArgb(40, 6, 182, 212)),
        InsightCategory.Financial => new SolidColorBrush(Color.FromArgb(40, 245, 158, 11)),
        _ => new SolidColorBrush(Color.FromArgb(40, 107, 114, 128))
    };

    private static Brush GetUrgencyColor(UrgencyLevel urgency) => urgency switch
    {
        UrgencyLevel.Critical => new SolidColorBrush(Color.FromRgb(239, 68, 68)),
        UrgencyLevel.High => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
        UrgencyLevel.Medium => new SolidColorBrush(Color.FromRgb(59, 130, 246)),
        _ => new SolidColorBrush(Color.FromRgb(34, 197, 94))
    };

    private static Brush GetUrgencyBackground(UrgencyLevel urgency) => urgency switch
    {
        UrgencyLevel.Critical => new SolidColorBrush(Color.FromArgb(40, 239, 68, 68)),
        UrgencyLevel.High => new SolidColorBrush(Color.FromArgb(40, 245, 158, 11)),
        UrgencyLevel.Medium => new SolidColorBrush(Color.FromArgb(40, 59, 130, 246)),
        _ => new SolidColorBrush(Color.FromArgb(40, 34, 197, 94))
    };
}

public class AlertViewModel
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Severity { get; set; } = "";
    public Brush SeverityColor { get; set; } = Brushes.White;
    public Brush SeverityBackground { get; set; } = Brushes.Transparent;
    public Brush BackgroundColor { get; set; } = Brushes.Transparent;
    public string Icon { get; set; } = "";
    public Brush IconBackground { get; set; } = Brushes.Transparent;
}

public class InsightViewModel
{
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Category { get; set; } = "";
    public string Recommendation { get; set; } = "";
    public bool HasRecommendation { get; set; }
    public string Impact { get; set; } = "";
    public Brush ImpactColor { get; set; } = Brushes.White;
    public Brush ImpactBackground { get; set; } = Brushes.Transparent;
    public string Icon { get; set; } = "";
    public Brush IconBackground { get; set; } = Brushes.Transparent;
}

public class InventoryRecommendationViewModel
{
    public string ProductName { get; set; } = "";
    public int DaysOfStockRemaining { get; set; }
    public decimal RecommendedQuantity { get; set; }
    public string Urgency { get; set; } = "";
    public Brush UrgencyColor { get; set; } = Brushes.White;
    public Brush UrgencyBackground { get; set; } = Brushes.Transparent;
}

public class ChatMessageViewModel
{
    public string Message { get; set; } = "";
    public bool IsUser { get; set; }
    public Brush Background { get; set; } = Brushes.Transparent;
    public HorizontalAlignment Alignment { get; set; }
}
