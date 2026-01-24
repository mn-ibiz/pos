using System.Windows;
using System.Windows.Input;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Dialog for creating or editing expense budgets.
/// </summary>
public partial class ExpenseBudgetEditorDialog : Window
{
    private readonly ExpenseBudget? _existingBudget;
    private List<ExpenseCategory> _categories = [];

    /// <summary>
    /// Gets the result expense budget.
    /// </summary>
    public ExpenseBudget? Result { get; private set; }

    /// <summary>
    /// Creates a new expense budget editor dialog.
    /// </summary>
    /// <param name="existingBudget">Existing budget to edit, or null for new.</param>
    public ExpenseBudgetEditorDialog(ExpenseBudget? existingBudget)
    {
        InitializeComponent();
        _existingBudget = existingBudget;

        TitleText.Text = existingBudget == null ? "New Expense Budget" : "Edit Expense Budget";

        // Enable window dragging
        MouseLeftButtonDown += (s, e) => { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); };

        // Initialize period type options
        PeriodTypeComboBox.ItemsSource = new[]
        {
            new { Value = BudgetPeriod.Monthly, Display = "Monthly" },
            new { Value = BudgetPeriod.Quarterly, Display = "Quarterly" },
            new { Value = BudgetPeriod.Annually, Display = "Annually" }
        };
        PeriodTypeComboBox.DisplayMemberPath = "Display";
        PeriodTypeComboBox.SelectedValuePath = "Value";
        PeriodTypeComboBox.SelectedValue = BudgetPeriod.Monthly;

        // Initialize year options (current year + 1 year ahead)
        var currentYear = DateTime.Now.Year;
        YearComboBox.ItemsSource = Enumerable.Range(currentYear - 1, 3).ToList();
        YearComboBox.SelectedItem = currentYear;

        // Initialize month options
        MonthComboBox.ItemsSource = new[]
        {
            new { Value = 1, Display = "January" },
            new { Value = 2, Display = "February" },
            new { Value = 3, Display = "March" },
            new { Value = 4, Display = "April" },
            new { Value = 5, Display = "May" },
            new { Value = 6, Display = "June" },
            new { Value = 7, Display = "July" },
            new { Value = 8, Display = "August" },
            new { Value = 9, Display = "September" },
            new { Value = 10, Display = "October" },
            new { Value = 11, Display = "November" },
            new { Value = 12, Display = "December" }
        };
        MonthComboBox.DisplayMemberPath = "Display";
        MonthComboBox.SelectedValuePath = "Value";
        MonthComboBox.SelectedValue = DateTime.Now.Month;

        // Initialize quarter options
        QuarterComboBox.ItemsSource = new[]
        {
            new { Value = 1, Display = "Q1 (Jan-Mar)" },
            new { Value = 2, Display = "Q2 (Apr-Jun)" },
            new { Value = 3, Display = "Q3 (Jul-Sep)" },
            new { Value = 4, Display = "Q4 (Oct-Dec)" }
        };
        QuarterComboBox.DisplayMemberPath = "Display";
        QuarterComboBox.SelectedValuePath = "Value";
        QuarterComboBox.SelectedValue = (DateTime.Now.Month - 1) / 3 + 1;

        // Add selection changed handlers
        YearComboBox.SelectionChanged += (s, e) => UpdateDateRangeText();
        MonthComboBox.SelectionChanged += (s, e) => UpdateDateRangeText();
        QuarterComboBox.SelectionChanged += (s, e) => UpdateDateRangeText();

        UpdateDateRangeText();
    }

    /// <summary>
    /// Initializes the dialog with dropdown data.
    /// </summary>
    public void Initialize(List<ExpenseCategory> categories)
    {
        _categories = categories;

        // Add empty option for overall budget
        var categoryList = new List<ExpenseCategory?> { null };
        categoryList.AddRange(categories.Where(c => c.IsActive).OrderBy(c => c.Name));
        CategoryComboBox.ItemsSource = categoryList;

        // Load existing data if editing
        if (_existingBudget != null)
        {
            LoadExistingBudget();
        }
    }

    private void LoadExistingBudget()
    {
        if (_existingBudget == null) return;

        NameTextBox.Text = _existingBudget.Name;
        AmountTextBox.Text = _existingBudget.Amount.ToString("N2");
        PeriodTypeComboBox.SelectedValue = _existingBudget.Period;
        YearComboBox.SelectedItem = _existingBudget.Year;
        ThresholdTextBox.Text = _existingBudget.AlertThreshold.ToString();
        NotesTextBox.Text = _existingBudget.Notes;

        // Set category
        if (_existingBudget.ExpenseCategoryId.HasValue)
        {
            CategoryComboBox.SelectedItem = _categories.FirstOrDefault(c => c.Id == _existingBudget.ExpenseCategoryId);
        }

        // Set month or quarter
        if (_existingBudget.Month.HasValue)
        {
            MonthComboBox.SelectedValue = _existingBudget.Month;
        }
        if (_existingBudget.Quarter.HasValue)
        {
            QuarterComboBox.SelectedValue = _existingBudget.Quarter;
        }

        UpdatePeriodControls();
        UpdateDateRangeText();
    }

    private void PeriodTypeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdatePeriodControls();
        UpdateDateRangeText();
    }

    private void UpdatePeriodControls()
    {
        if (PeriodTypeComboBox.SelectedValue is BudgetPeriod period)
        {
            switch (period)
            {
                case BudgetPeriod.Monthly:
                    MonthPanel.Visibility = Visibility.Visible;
                    QuarterPanel.Visibility = Visibility.Collapsed;
                    break;
                case BudgetPeriod.Quarterly:
                    MonthPanel.Visibility = Visibility.Collapsed;
                    QuarterPanel.Visibility = Visibility.Visible;
                    break;
                case BudgetPeriod.Annually:
                    MonthPanel.Visibility = Visibility.Collapsed;
                    QuarterPanel.Visibility = Visibility.Collapsed;
                    break;
            }
        }
    }

    private void UpdateDateRangeText()
    {
        if (YearComboBox.SelectedItem == null || PeriodTypeComboBox.SelectedValue == null) return;

        var year = (int)YearComboBox.SelectedItem;
        var period = (BudgetPeriod)PeriodTypeComboBox.SelectedValue;
        var (startDate, endDate) = CalculateDateRange(period, year);

        DateRangeText.Text = $"Budget Period: {startDate:MMM d, yyyy} - {endDate:MMM d, yyyy}";
    }

    private (DateTime StartDate, DateTime EndDate) CalculateDateRange(BudgetPeriod period, int year)
    {
        return period switch
        {
            BudgetPeriod.Monthly when MonthComboBox.SelectedValue is int month =>
                (new DateTime(year, month, 1), new DateTime(year, month, DateTime.DaysInMonth(year, month))),

            BudgetPeriod.Quarterly when QuarterComboBox.SelectedValue is int quarter =>
                (new DateTime(year, (quarter - 1) * 3 + 1, 1),
                 new DateTime(year, quarter * 3, DateTime.DaysInMonth(year, quarter * 3))),

            BudgetPeriod.Annually =>
                (new DateTime(year, 1, 1), new DateTime(year, 12, 31)),

            _ => (new DateTime(year, 1, 1), new DateTime(year, 12, 31))
        };
    }

    private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !IsNumericInput(e.Text);
    }

    private static bool IsNumericInput(string text)
    {
        return text.All(c => char.IsDigit(c) || c == '.');
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateForm())
        {
            return;
        }

        try
        {
            Result = _existingBudget != null ? _existingBudget : new ExpenseBudget();

            Result.Name = NameTextBox.Text.Trim();
            Result.Amount = decimal.Parse(AmountTextBox.Text);
            Result.Period = (BudgetPeriod)PeriodTypeComboBox.SelectedValue!;
            Result.Year = (int)YearComboBox.SelectedItem!;
            Result.Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim();

            if (int.TryParse(ThresholdTextBox.Text, out var threshold))
            {
                Result.AlertThreshold = Math.Clamp(threshold, 1, 100);
            }

            // Set category
            if (CategoryComboBox.SelectedItem is ExpenseCategory category)
            {
                Result.ExpenseCategoryId = category.Id;
            }
            else
            {
                Result.ExpenseCategoryId = null;
            }

            // Set month/quarter based on period type
            Result.Month = null;
            Result.Quarter = null;

            switch (Result.Period)
            {
                case BudgetPeriod.Monthly:
                    Result.Month = (int)MonthComboBox.SelectedValue!;
                    break;
                case BudgetPeriod.Quarterly:
                    Result.Quarter = (int)QuarterComboBox.SelectedValue!;
                    break;
            }

            // Calculate date range
            var (startDate, endDate) = CalculateDateRange(Result.Period, Result.Year);
            Result.StartDate = startDate;
            Result.EndDate = endDate;

            // Preserve existing data for edits
            if (_existingBudget != null)
            {
                Result.Id = _existingBudget.Id;
                Result.SpentAmount = _existingBudget.SpentAmount;
                Result.LastCalculatedAt = _existingBudget.LastCalculatedAt;
                Result.AlertSent = _existingBudget.AlertSent;
                Result.IsActive = _existingBudget.IsActive;
                Result.CreatedAt = _existingBudget.CreatedAt;
            }
            else
            {
                Result.IsActive = true;
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Error: {ex.Message}";
        }
    }

    private bool ValidateForm()
    {
        ErrorText.Text = string.Empty;

        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            ErrorText.Text = "Budget name is required.";
            NameTextBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(AmountTextBox.Text) || !decimal.TryParse(AmountTextBox.Text, out var amount) || amount <= 0)
        {
            ErrorText.Text = "Please enter a valid budget amount.";
            AmountTextBox.Focus();
            return false;
        }

        if (PeriodTypeComboBox.SelectedValue == null)
        {
            ErrorText.Text = "Please select a period type.";
            PeriodTypeComboBox.Focus();
            return false;
        }

        if (YearComboBox.SelectedItem == null)
        {
            ErrorText.Text = "Please select a year.";
            YearComboBox.Focus();
            return false;
        }

        var period = (BudgetPeriod)PeriodTypeComboBox.SelectedValue!;
        if (period == BudgetPeriod.Monthly && MonthComboBox.SelectedValue == null)
        {
            ErrorText.Text = "Please select a month.";
            MonthComboBox.Focus();
            return false;
        }

        if (period == BudgetPeriod.Quarterly && QuarterComboBox.SelectedValue == null)
        {
            ErrorText.Text = "Please select a quarter.";
            QuarterComboBox.Focus();
            return false;
        }

        if (!string.IsNullOrWhiteSpace(ThresholdTextBox.Text))
        {
            if (!int.TryParse(ThresholdTextBox.Text, out var threshold) || threshold < 1 || threshold > 100)
            {
                ErrorText.Text = "Alert threshold must be between 1 and 100.";
                ThresholdTextBox.Focus();
                return false;
            }
        }

        return true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
