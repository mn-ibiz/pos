using System.Windows;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Reports;
using HospitalityPOS.WPF.ViewModels;
using HospitalityPOS.WPF.Views;
using HospitalityPOS.WPF.Views.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalityPOS.WPF.Services;

/// <summary>
/// Dialog service implementation using WPF MessageBox and custom dialogs.
/// </summary>
public class DialogService : IDialogService
{
    /// <inheritdoc />
    public Task ShowMessageAsync(string title, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Application.Current.Dispatcher.Invoke(() =>
        {
            var owner = Application.Current.MainWindow;
            MessageBox.Show(
                owner,
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ShowConfirmationAsync(string title, string message)
    {
        return ShowConfirmationAsync(title, message, "Yes", "No");
    }

    /// <inheritdoc />
    public Task<bool> ShowConfirmationAsync(string title, string message, string confirmText, string cancelText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(confirmText);
        ArgumentException.ThrowIfNullOrWhiteSpace(cancelText);

        var result = Application.Current.Dispatcher.Invoke(() =>
        {
            var dialogResult = MessageBox.Show(
                Application.Current.MainWindow,
                message,
                title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            return dialogResult == MessageBoxResult.Yes;
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task ShowErrorAsync(string message)
    {
        return ShowErrorAsync("Error", message);
    }

    /// <inheritdoc />
    public Task ShowErrorAsync(string title, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(
                Application.Current.MainWindow,
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ShowWarningAsync(string title, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(
                Application.Current.MainWindow,
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string?> ShowInputAsync(string title, string prompt, string defaultValue = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        var result = Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new InputDialog(title, prompt, defaultValue);
            dialog.Owner = Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                return dialog.InputText;
            }

            return null;
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<string?> ShowPinEntryAsync(string title, string prompt, int maxLength = 4)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, 1);

        var result = Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new InputDialog(title, prompt, string.Empty, isPassword: true, maxLength: maxLength);
            dialog.Owner = Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                return dialog.InputText;
            }

            return null;
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<string?> ShowAuthorizationOverrideAsync(string actionDescription, string permissionRequired)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionDescription);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionRequired);

        var result = Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new AuthorizationOverrideDialog(actionDescription, permissionRequired);
            dialog.Owner = Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                return dialog.EnteredPin;
            }

            return null;
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<decimal?> ShowOpenWorkPeriodDialogAsync(decimal? previousClosingBalance = null)
    {
        var result = Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new OpenWorkPeriodDialog(previousClosingBalance);
            dialog.Owner = Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                return (decimal?)dialog.OpeningFloat;
            }

            return null;
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task ShowXReportDialogAsync(XReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new XReportDialog(report)
            {
                Owner = Application.Current.MainWindow
            };
            dialog.ShowDialog();
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ShowXReportDialogAsync(HospitalityPOS.Core.DTOs.XReportData report, bool autoPrint = false)
    {
        ArgumentNullException.ThrowIfNull(report);

        Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new XReportDataDialog(report, autoPrint)
            {
                Owner = Application.Current.MainWindow
            };
            dialog.ShowDialog();
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ShowCombinedXReportDialogAsync()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var viewModel = App.Services.GetRequiredService<CombinedXReportViewModel>();
            var dialog = new CombinedXReportView(viewModel)
            {
                Owner = Application.Current.MainWindow
            };
            dialog.ShowDialog();
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ShowCombinedZReportDialogAsync(int? workPeriodId = null)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var viewModel = App.Services.GetRequiredService<CombinedZReportViewModel>();
            if (workPeriodId.HasValue)
            {
                viewModel.SetWorkPeriodId(workPeriodId.Value);
            }
            var dialog = new CombinedZReportView(viewModel)
            {
                Owner = Application.Current.MainWindow
            };
            dialog.ShowDialog();
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ShowZReportDialogAsync(HospitalityPOS.Core.Models.Reports.ZReport report, bool autoPrint = false)
    {
        ArgumentNullException.ThrowIfNull(report);

        Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new ZReportDialog(report, autoPrint)
            {
                Owner = Application.Current.MainWindow
            };
            dialog.ShowDialog();
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<(decimal ClosingCash, string? Notes)?> ShowCloseWorkPeriodDialogAsync(
        decimal expectedCash,
        decimal openingFloat,
        decimal cashSales,
        decimal cashPayouts,
        IReadOnlyList<HospitalityPOS.Core.Entities.Receipt>? unsettledReceipts = null)
    {
        var result = Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new CloseWorkPeriodDialog(
                expectedCash,
                openingFloat,
                cashSales,
                cashPayouts,
                unsettledReceipts)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                return ((decimal ClosingCash, string? Notes)?)(dialog.ClosingCash, dialog.ClosingNotes);
            }

            return null;
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public async Task<CategoryEditorResult?> ShowCategoryEditorDialogAsync(
        Category? existingCategory,
        int? defaultParentId = null)
    {
        // Get all categories for parent selection using service scope
        IReadOnlyList<Category> availableParents;
        using (var scope = App.Services.CreateScope())
        {
            var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
            availableParents = await categoryService.GetAllCategoriesAsync();
        }

        var result = Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new CategoryEditorDialog(existingCategory, availableParents, defaultParentId)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.Result;
            }

            return null;
        });

        return result;
    }

    /// <inheritdoc />
    public async Task<ProductEditorResult?> ShowProductEditorDialogAsync(
        Product? existingProduct,
        int? defaultCategoryId = null)
    {
        // Get all categories for product category selection using service scope
        IReadOnlyList<Category> categories;
        using (var scope = App.Services.CreateScope())
        {
            var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
            categories = await categoryService.GetAllCategoriesAsync();
        }

        var result = Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new ProductEditorDialog(existingProduct, categories, defaultCategoryId)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.Result;
            }

            return null;
        });

        return result;
    }

    /// <inheritdoc />
    public Task<(string Pin, string Reason)?> ShowOwnershipOverrideDialogAsync(string ownerName, string actionDescription)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionDescription);

        var result = Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new OwnershipOverrideDialog(ownerName, actionDescription)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true &&
                !string.IsNullOrEmpty(dialog.EnteredPin) &&
                !string.IsNullOrEmpty(dialog.OverrideReason))
            {
                return ((string Pin, string Reason)?)(dialog.EnteredPin, dialog.OverrideReason);
            }

            return null;
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<SplitBillDialogResult?> ShowSplitBillDialogAsync(Receipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        var result = Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new SplitBillDialog(receipt)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                return new SplitBillDialogResult
                {
                    IsEqualSplit = dialog.IsEqualSplit,
                    NumberOfWays = dialog.NumberOfWays,
                    SplitRequests = dialog.SplitRequests
                };
            }

            return null;
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<List<int>?> ShowMergeBillDialogAsync(IEnumerable<Receipt> receipts)
    {
        ArgumentNullException.ThrowIfNull(receipts);

        var result = Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new MergeBillDialog(receipts)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true && dialog.SelectedReceiptIds.Count >= 2)
            {
                return dialog.SelectedReceiptIds;
            }

            return null;
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<VoidReceiptDialogResult?> ShowVoidReceiptDialogAsync(
        Receipt receipt,
        IReadOnlyList<VoidReason> voidReasons)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        ArgumentNullException.ThrowIfNull(voidReasons);

        var result = Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new VoidReceiptDialog(receipt, voidReasons)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true && dialog.SelectedVoidReasonId.HasValue)
            {
                return new VoidReceiptDialogResult
                {
                    VoidReasonId = dialog.SelectedVoidReasonId.Value,
                    AdditionalNotes = dialog.AdditionalNotes
                };
            }

            return null;
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<PaymentMethodEditorResult?> ShowPaymentMethodEditorDialogAsync(PaymentMethod? existingMethod)
    {
        var result = Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new PaymentMethodEditorDialog(existingMethod)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.Result;
            }

            return null;
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<SupplierEditorResult?> ShowSupplierEditorDialogAsync(Supplier? existingSupplier)
    {
        var result = Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new SupplierEditorDialog(existingSupplier)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.Result;
            }

            return null;
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task ShowInfoAsync(string title, string message)
    {
        return ShowMessageAsync(title, message);
    }

    /// <inheritdoc />
    public Task ShowInfoAsync(string message)
    {
        return ShowInfoAsync("Information", message);
    }

    /// <inheritdoc />
    public Task ShowSuccessAsync(string title, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(
                Application.Current.MainWindow,
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ShowSuccessAsync(string message)
    {
        return ShowSuccessAsync("Success", message);
    }

    /// <inheritdoc />
    public Task<string?> ShowActionSheetAsync(string title, string? message, string cancelText, string? destructiveText, params string[] options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(cancelText);

        var result = Application.Current.Dispatcher.Invoke(() =>
        {
            // Build options list for display
            var allOptions = new List<string>();
            if (!string.IsNullOrEmpty(destructiveText))
            {
                allOptions.Add(destructiveText);
            }
            allOptions.AddRange(options);
            allOptions.Add(cancelText);

            // Show action sheet dialog
            var dialog = new ActionSheetDialog(title, message, allOptions, cancelText, destructiveText)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.SelectedOption;
            }

            return null;
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<bool> ShowConfirmAsync(string title, string message)
    {
        return ShowConfirmationAsync(title, message);
    }

    /// <inheritdoc />
    public Task<ProductOffer?> ShowOfferEditorDialogAsync(ProductOffer? existingOffer)
    {
        var result = Application.Current.Dispatcher.Invoke(() =>
        {
            using var scope = App.Services.CreateScope();
            var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
            var offerService = scope.ServiceProvider.GetRequiredService<IOfferService>();
            var logger = scope.ServiceProvider.GetRequiredService<Serilog.ILogger>();

            var viewModel = new HospitalityPOS.WPF.ViewModels.OfferEditorViewModel(
                logger,
                productService,
                offerService,
                existingOffer);

            var dialog = new HospitalityPOS.WPF.Views.OfferEditorDialog(viewModel)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.Result;
            }

            return null;
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<DateTime?> ShowDatePickerDialogAsync(string title, string prompt, DateTime defaultDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        var result = Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new HospitalityPOS.WPF.Views.Dialogs.DatePickerDialog(title, prompt, defaultDate)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                return (DateTime?)dialog.SelectedDate;
            }

            return null;
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<SupplierPayment?> ShowSupplierPaymentDialogAsync(Supplier supplier, SupplierInvoice? invoice = null)
    {
        ArgumentNullException.ThrowIfNull(supplier);

        var result = Application.Current.Dispatcher.Invoke(() =>
        {
            using var scope = App.Services.CreateScope();
            var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();
            var supplierCreditService = scope.ServiceProvider.GetRequiredService<ISupplierCreditService>();

            var dialog = new HospitalityPOS.WPF.Views.Dialogs.SupplierPaymentDialog(supplier, invoice, sessionService, supplierCreditService)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.Result;
            }

            return null;
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<SupplierInvoice?> ShowSupplierInvoiceEditorDialogAsync(SupplierInvoice? existingInvoice, Supplier supplier)
    {
        ArgumentNullException.ThrowIfNull(supplier);

        var result = Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new HospitalityPOS.WPF.Views.Dialogs.SupplierInvoiceEditorDialog(existingInvoice, supplier)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.Result;
            }

            return null;
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task ShowSupplierStatementDialogAsync(Supplier supplier)
    {
        ArgumentNullException.ThrowIfNull(supplier);

        Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new HospitalityPOS.WPF.Views.Dialogs.SupplierStatementDialog(supplier)
            {
                Owner = Application.Current.MainWindow
            };

            dialog.ShowDialog();
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ShowHtmlPreviewAsync(string title, string? subtitle, string htmlContent, string? exportFilename = null)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new HospitalityPOS.WPF.Views.Dialogs.HtmlPreviewDialog(title, subtitle, htmlContent, exportFilename)
            {
                Owner = Application.Current.MainWindow
            };

            dialog.ShowDialog();
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<VariantOptionEditorResult?> ShowVariantOptionEditorDialogAsync(VariantOption? existingOption)
    {
        var result = Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new HospitalityPOS.WPF.Views.Dialogs.VariantOptionEditorDialog(existingOption)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.Result;
            }

            return null;
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<VariantOptionValueEditorResult?> ShowVariantValueEditorDialogAsync(VariantOptionValue? existingValue, VariantOption parentOption)
    {
        ArgumentNullException.ThrowIfNull(parentOption);

        var result = Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new HospitalityPOS.WPF.Views.Dialogs.VariantValueEditorDialog(existingValue, parentOption)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.Result;
            }

            return null;
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<ModifierGroupEditorResult?> ShowModifierGroupEditorDialogAsync(ModifierGroup? existingGroup)
    {
        var result = Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new HospitalityPOS.WPF.Views.Dialogs.ModifierGroupEditorDialog(existingGroup)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.Result;
            }

            return null;
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<ModifierItemEditorResult?> ShowModifierItemEditorDialogAsync(ModifierItem? existingItem, ModifierGroup parentGroup)
    {
        ArgumentNullException.ThrowIfNull(parentGroup);

        var result = Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new HospitalityPOS.WPF.Views.Dialogs.ModifierItemEditorDialog(existingItem, parentGroup)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.Result;
            }

            return null;
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<HospitalityPOS.WPF.Views.Dialogs.VariantSelectionResult?> ShowVariantSelectionDialogAsync(
        Product product,
        IReadOnlyList<VariantOption> availableOptions,
        IReadOnlyList<ProductVariant> productVariants)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(availableOptions);
        ArgumentNullException.ThrowIfNull(productVariants);

        var result = Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new HospitalityPOS.WPF.Views.Dialogs.VariantSelectionDialog(product, availableOptions, productVariants)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.Result;
            }

            return null;
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<HospitalityPOS.WPF.Views.Dialogs.ModifierSelectionResult?> ShowModifierSelectionDialogAsync(
        Product product,
        IReadOnlyList<ModifierGroup> modifierGroups)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(modifierGroups);

        var result = Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new HospitalityPOS.WPF.Views.Dialogs.ModifierSelectionDialog(product, modifierGroups)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.Result;
            }

            return null;
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<ExpenseEditorResult?> ShowExpenseEditorDialogAsync(Expense? existingExpense)
    {
        var result = Application.Current.Dispatcher.Invoke<ExpenseEditorResult?>(() =>
        {
            using var scope = App.Services.CreateScope();
            var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();
            var supplierService = scope.ServiceProvider.GetRequiredService<ISupplierService>();
            var paymentMethodService = scope.ServiceProvider.GetRequiredService<IPaymentMethodService>();

            // Load required data
            var categories = expenseService.GetCategoriesAsync().GetAwaiter().GetResult();
            var suppliers = supplierService.GetAllSuppliersAsync().GetAwaiter().GetResult();
            var paymentMethods = paymentMethodService.GetAllAsync().GetAwaiter().GetResult();

            var dialog = new ExpenseEditorDialog(existingExpense)
            {
                Owner = Application.Current.MainWindow
            };

            // Initialize with data
            dialog.Initialize(categories, suppliers, paymentMethods);

            if (dialog.ShowDialog() == true)
            {
                return dialog.Result;
            }

            return null;
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<ExpenseCategoryEditorResult?> ShowExpenseCategoryEditorDialogAsync(ExpenseCategory? existingCategory)
    {
        var result = Application.Current.Dispatcher.Invoke<ExpenseCategoryEditorResult?>(() =>
        {
            var dialog = new ExpenseCategoryEditorDialog(existingCategory)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.Result;
            }

            return null;
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<ManualAttendanceEntryResult?> ShowManualAttendanceEntryDialogAsync(Employee? employee)
    {
        var result = Application.Current.Dispatcher.Invoke<ManualAttendanceEntryResult?>(() =>
        {
            var dialog = new ManualAttendanceEntryDialog(employee)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.Result;
            }

            return null;
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<RecurringExpense?> ShowRecurringExpenseEditorDialogAsync(RecurringExpense? existingExpense)
    {
        var result = Application.Current.Dispatcher.Invoke<RecurringExpense?>(() =>
        {
            // Load required data
            using var scope = App.Services.CreateScope();
            var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();
            var supplierService = scope.ServiceProvider.GetRequiredService<ISupplierService>();
            var paymentMethodService = scope.ServiceProvider.GetRequiredService<IPaymentMethodService>();

            var categories = expenseService.GetCategoriesAsync().GetAwaiter().GetResult();
            var suppliers = supplierService.GetAllSuppliersAsync().GetAwaiter().GetResult();
            var paymentMethods = paymentMethodService.GetAllPaymentMethodsAsync().GetAwaiter().GetResult();

            var dialog = new RecurringExpenseEditorDialog(existingExpense)
            {
                Owner = Application.Current.MainWindow
            };

            dialog.Initialize(categories.ToList(), suppliers.ToList(), paymentMethods.ToList());

            if (dialog.ShowDialog() == true)
            {
                return dialog.Result;
            }

            return null;
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<ExpenseBudget?> ShowExpenseBudgetEditorDialogAsync(ExpenseBudget? existingBudget)
    {
        var result = Application.Current.Dispatcher.Invoke<ExpenseBudget?>(() =>
        {
            // Load required data
            using var scope = App.Services.CreateScope();
            var expenseService = scope.ServiceProvider.GetRequiredService<IExpenseService>();

            var categories = expenseService.GetCategoriesAsync().GetAwaiter().GetResult();

            var dialog = new ExpenseBudgetEditorDialog(existingBudget)
            {
                Owner = Application.Current.MainWindow
            };

            dialog.Initialize(categories.ToList());

            if (dialog.ShowDialog() == true)
            {
                return dialog.Result;
            }

            return null;
        });

        return Task.FromResult(result);
    }
}
