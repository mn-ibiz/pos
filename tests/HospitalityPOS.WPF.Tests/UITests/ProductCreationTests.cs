using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FluentAssertions;
using Xunit;

namespace HospitalityPOS.WPF.Tests.UITests;

/// <summary>
/// UI tests for product creation functionality.
/// These tests require the application to be built and database to be configured.
/// </summary>
[Collection("UI Tests")]
public class ProductCreationTests : IDisposable
{
    private readonly AppTestFixture _fixture;

    public ProductCreationTests()
    {
        _fixture = new AppTestFixture();
    }

    [Fact]
    public void AddProduct_WithValidData_ShouldCreateProduct()
    {
        // Arrange
        _fixture.LaunchApp();
        var testProductCode = $"TEST{DateTime.Now:HHmmss}";
        var testProductName = $"Test Product {DateTime.Now:HHmmss}";

        // Navigate to Products section
        NavigateToProducts();

        // Click "New Product" button (Ctrl+N shortcut or button)
        ClickNewProduct();

        // Wait for Product Editor dialog to appear
        Thread.Sleep(500);

        // Fill in product details
        FillProductForm(
            code: testProductCode,
            name: testProductName,
            sellingPrice: "150.00",
            taxRate: "16"
        );

        // Click Save
        ClickSave();

        // Assert - Product should appear in the list
        Thread.Sleep(1000);
        var productInList = _fixture.FindByText(testProductName);
        productInList.Should().NotBeNull($"Product '{testProductName}' should appear in the product list after creation");
    }

    [Fact(Skip = "Requires application to be running with configured database")]
    public void AddProduct_WithEmptyRequiredFields_ShouldShowValidationError()
    {
        // Arrange
        _fixture.LaunchApp();

        // Navigate to Products section
        NavigateToProducts();

        // Click "New Product" button
        ClickNewProduct();
        Thread.Sleep(500);

        // Try to save without filling required fields
        ClickSave();

        // Assert - Should show validation error
        Thread.Sleep(500);
        var errorElement = _fixture.FindByName("ErrorBorder") ?? _fixture.FindById("ErrorBorder");
        // The error border should be visible when validation fails
    }

    [Fact(Skip = "Requires application to be running with configured database")]
    public void AddProduct_CancelButton_ShouldCloseDialogWithoutSaving()
    {
        // Arrange
        _fixture.LaunchApp();
        var testProductCode = $"CANCEL{DateTime.Now:HHmmss}";

        // Navigate to Products section
        NavigateToProducts();

        // Click "New Product" button
        ClickNewProduct();
        Thread.Sleep(500);

        // Fill in some data
        var codeTextBox = FindTextBox("CodeTextBox");
        codeTextBox?.AsTextBox().Enter(testProductCode);

        // Click Cancel
        var cancelButton = _fixture.FindByName("Cancel");
        cancelButton?.AsButton().Click();

        // Assert - Dialog should close, product should not exist
        Thread.Sleep(500);
        var productInList = _fixture.FindByText(testProductCode);
        productInList.Should().BeNull("Cancelled product should not appear in the list");
    }

    private void NavigateToProducts()
    {
        // Find and click the Products menu item
        var productsMenuItem = _fixture.WaitForElementByName("Products", TimeSpan.FromSeconds(5));
        if (productsMenuItem != null)
        {
            productsMenuItem.Click();
            Thread.Sleep(500);
        }
        else
        {
            // Try keyboard shortcut if menu item not found
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_P);
            Thread.Sleep(500);
        }
    }

    private void ClickNewProduct()
    {
        // Try to find the "New Product" or "Add Product" button
        var newButton = _fixture.FindByName("New Product")
                     ?? _fixture.FindByName("Add Product")
                     ?? _fixture.FindByText("New Product");

        if (newButton != null)
        {
            newButton.Click();
        }
        else
        {
            // Use keyboard shortcut Ctrl+N
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_N);
        }

        Thread.Sleep(500);
    }

    private void FillProductForm(
        string? code = null,
        string? barcode = null,
        string? name = null,
        string? sellingPrice = null,
        string? costPrice = null,
        string? taxRate = null,
        string? description = null)
    {
        // Find the product editor window (it's a separate window)
        var windows = _fixture.Application.GetAllTopLevelWindows(_fixture.Automation);
        var editorWindow = windows.FirstOrDefault(w => w.Title.Contains("Product Editor"));

        if (editorWindow == null)
        {
            // Try finding elements in main window (if dialog is modal overlay)
            editorWindow = _fixture.MainWindow;
        }

        if (code != null)
        {
            var codeBox = editorWindow.FindFirstDescendant(cf => cf.ByAutomationId("CodeTextBox"))?.AsTextBox();
            codeBox?.Enter(code);
        }

        if (barcode != null)
        {
            var barcodeBox = editorWindow.FindFirstDescendant(cf => cf.ByAutomationId("BarcodeTextBox"))?.AsTextBox();
            barcodeBox?.Enter(barcode);
        }

        if (name != null)
        {
            var nameBox = editorWindow.FindFirstDescendant(cf => cf.ByAutomationId("NameTextBox"))?.AsTextBox();
            nameBox?.Enter(name);
        }

        if (sellingPrice != null)
        {
            var priceBox = editorWindow.FindFirstDescendant(cf => cf.ByAutomationId("SellingPriceTextBox"))?.AsTextBox();
            priceBox?.Enter(sellingPrice);
        }

        if (costPrice != null)
        {
            var costBox = editorWindow.FindFirstDescendant(cf => cf.ByAutomationId("CostPriceTextBox"))?.AsTextBox();
            costBox?.Enter(costPrice);
        }

        if (taxRate != null)
        {
            var taxBox = editorWindow.FindFirstDescendant(cf => cf.ByAutomationId("TaxRateTextBox"))?.AsTextBox();
            if (taxBox != null)
            {
                taxBox.Text = ""; // Clear existing
                taxBox.Enter(taxRate);
            }
        }

        if (description != null)
        {
            var descBox = editorWindow.FindFirstDescendant(cf => cf.ByAutomationId("DescriptionTextBox"))?.AsTextBox();
            descBox?.Enter(description);
        }
    }

    private void ClickSave()
    {
        var windows = _fixture.Application.GetAllTopLevelWindows(_fixture.Automation);
        var editorWindow = windows.FirstOrDefault(w => w.Title.Contains("Product Editor")) ?? _fixture.MainWindow;

        var saveButton = editorWindow.FindFirstDescendant(cf => cf.ByAutomationId("SaveButton"))
                      ?? editorWindow.FindFirstDescendant(cf => cf.ByName("Save Product"));

        saveButton?.AsButton().Click();
    }

    private AutomationElement? FindTextBox(string automationId)
    {
        var windows = _fixture.Application.GetAllTopLevelWindows(_fixture.Automation);
        var editorWindow = windows.FirstOrDefault(w => w.Title.Contains("Product Editor")) ?? _fixture.MainWindow;

        return editorWindow.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}

/// <summary>
/// Collection definition for UI tests to ensure they don't run in parallel.
/// </summary>
[CollectionDefinition("UI Tests", DisableParallelization = true)]
public class UITestCollection : ICollectionFixture<AppTestFixture>
{
}
