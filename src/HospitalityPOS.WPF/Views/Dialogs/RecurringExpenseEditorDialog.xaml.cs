using System.Windows;
using System.Windows.Input;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Dialog for creating or editing recurring expenses.
/// </summary>
public partial class RecurringExpenseEditorDialog : Window
{
    private readonly RecurringExpense? _existingExpense;
    private List<ExpenseCategory> _categories = [];
    private List<Supplier> _suppliers = [];
    private List<PaymentMethod> _paymentMethods = [];

    /// <summary>
    /// Gets the result recurring expense.
    /// </summary>
    public RecurringExpense? Result { get; private set; }

    /// <summary>
    /// Creates a new recurring expense editor dialog.
    /// </summary>
    /// <param name="existingExpense">Existing expense to edit, or null for new.</param>
    public RecurringExpenseEditorDialog(RecurringExpense? existingExpense)
    {
        InitializeComponent();
        _existingExpense = existingExpense;

        TitleText.Text = existingExpense == null ? "New Recurring Expense" : "Edit Recurring Expense";

        // Enable window dragging
        MouseLeftButtonDown += (s, e) => { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); };

        // Initialize frequency options
        FrequencyComboBox.ItemsSource = Enum.GetValues<RecurrenceFrequency>()
            .Select(f => new { Value = f, Display = GetFrequencyDisplay(f) })
            .ToList();
        FrequencyComboBox.DisplayMemberPath = "Display";
        FrequencyComboBox.SelectedValuePath = "Value";
        FrequencyComboBox.SelectedValue = RecurrenceFrequency.Monthly;

        // Initialize day of month options (1-31)
        DayOfMonthComboBox.ItemsSource = Enumerable.Range(1, 31).ToList();
        DayOfMonthComboBox.SelectedItem = 1;

        // Set default dates
        StartDatePicker.SelectedDate = DateTime.Today;
    }

    /// <summary>
    /// Initializes the dialog with dropdown data.
    /// </summary>
    public void Initialize(
        List<ExpenseCategory> categories,
        List<Supplier> suppliers,
        List<PaymentMethod> paymentMethods)
    {
        _categories = categories;
        _suppliers = suppliers;
        _paymentMethods = paymentMethods;

        CategoryComboBox.ItemsSource = categories.Where(c => c.IsActive).OrderBy(c => c.Name);

        // Add empty option for optional dropdowns
        var supplierList = new List<Supplier?> { null };
        supplierList.AddRange(suppliers.Where(s => s.IsActive).OrderBy(s => s.Name));
        SupplierComboBox.ItemsSource = supplierList;

        var paymentMethodList = new List<PaymentMethod?> { null };
        paymentMethodList.AddRange(paymentMethods.Where(p => p.IsActive).OrderBy(p => p.Name));
        PaymentMethodComboBox.ItemsSource = paymentMethodList;

        // Load existing data if editing
        if (_existingExpense != null)
        {
            LoadExistingExpense();
        }
    }

    private void LoadExistingExpense()
    {
        if (_existingExpense == null) return;

        NameTextBox.Text = _existingExpense.Name;
        DescriptionTextBox.Text = _existingExpense.Description;
        AmountTextBox.Text = _existingExpense.Amount.ToString("N2");
        FrequencyComboBox.SelectedValue = _existingExpense.Frequency;
        DayOfMonthComboBox.SelectedItem = _existingExpense.DayOfMonth;
        StartDatePicker.SelectedDate = _existingExpense.StartDate;
        EndDatePicker.SelectedDate = _existingExpense.EndDate;
        AutoGenerateCheckBox.IsChecked = _existingExpense.AutoGenerate;
        AutoApproveCheckBox.IsChecked = _existingExpense.AutoApprove;
        IsEstimatedCheckBox.IsChecked = _existingExpense.IsEstimatedAmount;
        ReminderDaysTextBox.Text = _existingExpense.ReminderDaysBefore.ToString();
        NotesTextBox.Text = _existingExpense.Notes;

        // Set category
        CategoryComboBox.SelectedItem = _categories.FirstOrDefault(c => c.Id == _existingExpense.ExpenseCategoryId);

        // Set supplier
        if (_existingExpense.SupplierId.HasValue)
        {
            SupplierComboBox.SelectedItem = _suppliers.FirstOrDefault(s => s.Id == _existingExpense.SupplierId);
        }

        // Set payment method
        if (_existingExpense.PaymentMethodId.HasValue)
        {
            PaymentMethodComboBox.SelectedItem = _paymentMethods.FirstOrDefault(p => p.Id == _existingExpense.PaymentMethodId);
        }

        UpdateDayOfMonthVisibility();
    }

    private void FrequencyComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdateDayOfMonthVisibility();
    }

    private void UpdateDayOfMonthVisibility()
    {
        if (FrequencyComboBox.SelectedValue is RecurrenceFrequency frequency)
        {
            // Show day of month for Monthly, Quarterly, Annually
            DayOfMonthPanel.Visibility = frequency switch
            {
                RecurrenceFrequency.Monthly => Visibility.Visible,
                RecurrenceFrequency.Quarterly => Visibility.Visible,
                RecurrenceFrequency.Annually => Visibility.Visible,
                _ => Visibility.Collapsed
            };
        }
    }

    private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Allow only digits and decimal point
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
            Result = _existingExpense != null ? _existingExpense : new RecurringExpense();

            Result.Name = NameTextBox.Text.Trim();
            Result.Description = DescriptionTextBox.Text.Trim();
            Result.Amount = decimal.Parse(AmountTextBox.Text);
            Result.ExpenseCategoryId = ((ExpenseCategory)CategoryComboBox.SelectedItem).Id;
            Result.Frequency = (RecurrenceFrequency)FrequencyComboBox.SelectedValue!;
            Result.StartDate = StartDatePicker.SelectedDate!.Value;
            Result.EndDate = EndDatePicker.SelectedDate;
            Result.AutoGenerate = AutoGenerateCheckBox.IsChecked == true;
            Result.AutoApprove = AutoApproveCheckBox.IsChecked == true;
            Result.IsEstimatedAmount = IsEstimatedCheckBox.IsChecked == true;
            Result.Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim();

            if (DayOfMonthComboBox.SelectedItem != null)
            {
                Result.DayOfMonth = (int)DayOfMonthComboBox.SelectedItem;
            }

            if (int.TryParse(ReminderDaysTextBox.Text, out var reminderDays))
            {
                Result.ReminderDaysBefore = reminderDays;
            }

            if (SupplierComboBox.SelectedItem is Supplier supplier)
            {
                Result.SupplierId = supplier.Id;
            }
            else
            {
                Result.SupplierId = null;
            }

            if (PaymentMethodComboBox.SelectedItem is PaymentMethod paymentMethod)
            {
                Result.PaymentMethodId = paymentMethod.Id;
            }
            else
            {
                Result.PaymentMethodId = null;
            }

            // Calculate next due date
            Result.NextDueDate = CalculateNextDueDate(Result);

            // Preserve existing data for edits
            if (_existingExpense != null)
            {
                Result.Id = _existingExpense.Id;
                Result.LastGeneratedDate = _existingExpense.LastGeneratedDate;
                Result.OccurrenceCount = _existingExpense.OccurrenceCount;
                Result.IsActive = _existingExpense.IsActive;
                Result.CreatedAt = _existingExpense.CreatedAt;
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
            ErrorText.Text = "Name is required.";
            NameTextBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(AmountTextBox.Text) || !decimal.TryParse(AmountTextBox.Text, out var amount) || amount <= 0)
        {
            ErrorText.Text = "Please enter a valid amount.";
            AmountTextBox.Focus();
            return false;
        }

        if (CategoryComboBox.SelectedItem == null)
        {
            ErrorText.Text = "Please select a category.";
            CategoryComboBox.Focus();
            return false;
        }

        if (FrequencyComboBox.SelectedValue == null)
        {
            ErrorText.Text = "Please select a frequency.";
            FrequencyComboBox.Focus();
            return false;
        }

        if (!StartDatePicker.SelectedDate.HasValue)
        {
            ErrorText.Text = "Please select a start date.";
            StartDatePicker.Focus();
            return false;
        }

        if (EndDatePicker.SelectedDate.HasValue && EndDatePicker.SelectedDate < StartDatePicker.SelectedDate)
        {
            ErrorText.Text = "End date must be after start date.";
            EndDatePicker.Focus();
            return false;
        }

        return true;
    }

    private static DateTime CalculateNextDueDate(RecurringExpense expense)
    {
        var today = DateTime.Today;
        var startDate = expense.StartDate.Date;

        if (startDate > today)
        {
            // If start date is in future, next due is start date
            return startDate;
        }

        // Calculate based on frequency
        return expense.Frequency switch
        {
            RecurrenceFrequency.Daily => today,
            RecurrenceFrequency.Weekly => GetNextWeeklyDate(today, expense.DayOfWeek ?? 1),
            RecurrenceFrequency.BiWeekly => GetNextBiWeeklyDate(startDate, today),
            RecurrenceFrequency.Monthly => GetNextMonthlyDate(today, expense.DayOfMonth),
            RecurrenceFrequency.Quarterly => GetNextQuarterlyDate(today, expense.DayOfMonth),
            RecurrenceFrequency.Annually => GetNextAnnualDate(startDate, today),
            _ => today
        };
    }

    private static DateTime GetNextWeeklyDate(DateTime today, int dayOfWeek)
    {
        var daysUntil = ((dayOfWeek - (int)today.DayOfWeek + 7) % 7);
        if (daysUntil == 0) daysUntil = 7;
        return today.AddDays(daysUntil);
    }

    private static DateTime GetNextBiWeeklyDate(DateTime startDate, DateTime today)
    {
        var weeksSinceStart = (today - startDate).Days / 7;
        var weeksUntilNext = (2 - (weeksSinceStart % 2)) % 2;
        if (weeksUntilNext == 0) weeksUntilNext = 2;
        return today.AddDays(weeksUntilNext * 7);
    }

    private static DateTime GetNextMonthlyDate(DateTime today, int dayOfMonth)
    {
        var nextDate = new DateTime(today.Year, today.Month, Math.Min(dayOfMonth, DateTime.DaysInMonth(today.Year, today.Month)));
        if (nextDate <= today)
        {
            nextDate = nextDate.AddMonths(1);
            nextDate = new DateTime(nextDate.Year, nextDate.Month, Math.Min(dayOfMonth, DateTime.DaysInMonth(nextDate.Year, nextDate.Month)));
        }
        return nextDate;
    }

    private static DateTime GetNextQuarterlyDate(DateTime today, int dayOfMonth)
    {
        var currentQuarter = (today.Month - 1) / 3;
        var quarterStartMonth = currentQuarter * 3 + 1;
        var nextQuarterStartMonth = quarterStartMonth + 3;

        var nextYear = today.Year;
        if (nextQuarterStartMonth > 12)
        {
            nextQuarterStartMonth = 1;
            nextYear++;
        }

        return new DateTime(nextYear, nextQuarterStartMonth, Math.Min(dayOfMonth, DateTime.DaysInMonth(nextYear, nextQuarterStartMonth)));
    }

    private static DateTime GetNextAnnualDate(DateTime startDate, DateTime today)
    {
        var nextDate = new DateTime(today.Year, startDate.Month, startDate.Day);
        if (nextDate <= today)
        {
            nextDate = nextDate.AddYears(1);
        }
        return nextDate;
    }

    private static string GetFrequencyDisplay(RecurrenceFrequency frequency)
    {
        return frequency switch
        {
            RecurrenceFrequency.Daily => "Daily",
            RecurrenceFrequency.Weekly => "Weekly",
            RecurrenceFrequency.BiWeekly => "Bi-Weekly",
            RecurrenceFrequency.Monthly => "Monthly",
            RecurrenceFrequency.Quarterly => "Quarterly",
            RecurrenceFrequency.Annually => "Annually",
            _ => frequency.ToString()
        };
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
