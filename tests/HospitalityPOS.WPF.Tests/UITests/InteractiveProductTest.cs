using System.IO;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using Xunit;
using Xunit.Abstractions;

namespace HospitalityPOS.WPF.Tests.UITests;

/// <summary>
/// Interactive UI tests that can be run manually.
/// To run: dotnet test --filter "FullyQualifiedName~InteractiveProductTest" -- xUnit.Parallelization=false
/// </summary>
public class InteractiveProductTest : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly AppTestFixture _fixture;

    public InteractiveProductTest(ITestOutputHelper output)
    {
        _output = output;
        _fixture = new AppTestFixture();
    }

    /// <summary>
    /// Runs a complete product creation test flow.
    /// Prerequisites: Application must be buildable and database configured.
    /// </summary>
    [Fact]
    public void ManualTest_CreateProduct()
    {
        _output.WriteLine("Starting HospitalityPOS application...");
        _fixture.LaunchApp(TimeSpan.FromSeconds(60));
        _output.WriteLine($"Application launched. Main window: {_fixture.MainWindow.Title}");

        // Wait for initialization
        Thread.Sleep(3000);

        // Bring window to foreground and focus
        _fixture.MainWindow.SetForeground();
        _fixture.MainWindow.Focus();
        Thread.Sleep(1000);

        // Take initial screenshot to see what's displayed
        TakeScreenshot("01_initial");

        // Step 1: Handle Mode Selection screen - select Admin mode
        _output.WriteLine("Looking for Admin mode button by AutomationId...");
        var adminButton = _fixture.MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("AdminModeButton"));
        if (adminButton != null)
        {
            _output.WriteLine("Found Admin button by AutomationId");

            // Try to invoke the button using the Invoke pattern (more reliable for WPF buttons)
            var button = adminButton.AsButton();
            if (button != null)
            {
                _output.WriteLine("Invoking button...");
                button.Invoke();
            }
            else
            {
                _output.WriteLine("Falling back to Click...");
                adminButton.Click();
            }

            Thread.Sleep(2000);
            TakeScreenshot("02_after_admin_click");

            // Handle Admin Login screen
            _output.WriteLine("Handling Admin Login...");
            HandleAdminLogin();
        }
        else
        {
            // Fallback: try to find by text
            _output.WriteLine("AdminModeButton not found, trying by text...");
            adminButton = FindElementByTextContains("Admin");
            if (adminButton != null)
            {
                // Try to find the parent button
                var parent = adminButton.Parent;
                while (parent != null && parent.ControlType != FlaUI.Core.Definitions.ControlType.Button)
                {
                    parent = parent.Parent;
                }
                if (parent != null)
                {
                    _output.WriteLine("Found Admin parent button, clicking...");
                    parent.Click();
                    Thread.Sleep(2000);
                    HandleAdminLogin();
                }
                else
                {
                    _output.WriteLine("Could not find Admin button parent, clicking text...");
                    adminButton.Click();
                    Thread.Sleep(2000);
                }
            }
            else
            {
                _output.WriteLine("Admin button not found - might already be logged in or different screen");
                PrintVisibleElements();
            }
            TakeScreenshot("02_after_admin_click");
        }

        // Step 2: Find and click Products in sidebar
        _output.WriteLine("Looking for Products menu item...");
        var productsButton = FindElementByTextContains("Products");
        if (productsButton != null)
        {
            _output.WriteLine("Found Products button, clicking...");
            productsButton.Click();
            Thread.Sleep(1000);
            TakeScreenshot("03_products_view");
        }
        else
        {
            _output.WriteLine("Products button not found in sidebar");
            PrintVisibleElements();
        }

        // Step 3: Look for Add/New Product button
        _output.WriteLine("Looking for New Product button...");
        var newButton = FindElementByTextContains("New Product")
                     ?? FindElementByTextContains("Add Product")
                     ?? FindElementByTextContains("New");

        if (newButton != null)
        {
            _output.WriteLine("Found New Product button, clicking...");
            newButton.Click();
            Thread.Sleep(1000);
        }
        else
        {
            // Use keyboard shortcut Ctrl+N
            _output.WriteLine("Trying Ctrl+N shortcut...");
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_N);
            Thread.Sleep(1000);
        }

        TakeScreenshot("04_product_editor");

        // Find the product editor window or dialog
        var windows = _fixture.Application.GetAllTopLevelWindows(_fixture.Automation);
        _output.WriteLine($"Found {windows.Length} windows");
        foreach (var w in windows)
        {
            _output.WriteLine($"  - Window: {w.Title}");
        }

        // Product Editor could be a separate window or a modal in the main window
        var editorWindow = windows.FirstOrDefault(w => w.Title.Contains("Product Editor"))
                        ?? _fixture.MainWindow;

        // Fill form - try to find fields
        var testCode = $"TEST{DateTime.Now:HHmmss}";
        var testName = $"Test Product {DateTime.Now:HHmmss}";

        _output.WriteLine($"Filling product code: {testCode}");
        var codeBox = editorWindow.FindFirstDescendant(cf => cf.ByAutomationId("CodeTextBox"))?.AsTextBox();
        if (codeBox != null)
        {
            codeBox.Enter(testCode);
            _output.WriteLine("  Code entered successfully");
        }
        else
        {
            _output.WriteLine("  CodeTextBox not found");
        }

        _output.WriteLine($"Filling product name: {testName}");
        var nameBox = editorWindow.FindFirstDescendant(cf => cf.ByAutomationId("NameTextBox"))?.AsTextBox();
        if (nameBox != null)
        {
            nameBox.Enter(testName);
            _output.WriteLine("  Name entered successfully");
        }
        else
        {
            _output.WriteLine("  NameTextBox not found");
        }

        _output.WriteLine("Filling selling price: 250");
        var priceBox = editorWindow.FindFirstDescendant(cf => cf.ByAutomationId("SellingPriceTextBox"))?.AsTextBox();
        if (priceBox != null)
        {
            priceBox.Enter("250");
            _output.WriteLine("  Price entered successfully");
        }
        else
        {
            _output.WriteLine("  SellingPriceTextBox not found");
        }

        // Select or create a category
        _output.WriteLine("Selecting category...");
        SelectOrCreateCategory(editorWindow, "Beverages");

        TakeScreenshot("05_form_filled");

        // Click Save
        _output.WriteLine("Looking for Save button...");
        var saveButton = editorWindow.FindFirstDescendant(cf => cf.ByAutomationId("SaveButton"))?.AsButton()
                      ?? FindElementByTextContains("Save Product")?.AsButton()
                      ?? FindElementByTextContains("Save")?.AsButton();

        if (saveButton != null)
        {
            _output.WriteLine("Clicking Save button...");
            saveButton.Invoke();
            Thread.Sleep(3000);
            TakeScreenshot("06_after_save");

            // Check if dialog closed (product saved successfully)
            var createProductHeader = FindElementByTextContains("Create Product");
            if (createProductHeader == null)
            {
                _output.WriteLine("Product saved successfully - dialog closed!");

                // Verify product appears in list
                Thread.Sleep(1000);
                var productInList = FindElementByTextContains(testName);
                if (productInList != null)
                {
                    _output.WriteLine($"Verified: Product '{testName}' appears in the product list!");
                }
                else
                {
                    _output.WriteLine("Product may have been saved but not visible in current view");
                }
            }
            else
            {
                _output.WriteLine("Dialog still open - checking for errors...");
                PrintVisibleElements();
            }
        }
        else
        {
            _output.WriteLine("Save button not found");
        }

        TakeScreenshot("07_final");
        _output.WriteLine("Test completed!");
    }

    private void TakeScreenshot(string name)
    {
        var screenshotPath = Path.Combine(Path.GetTempPath(), $"pos_test_{name}_{DateTime.Now:HHmmss}.png");
        _output.WriteLine($"Screenshot: {screenshotPath}");
        _fixture.TakeScreenshot(screenshotPath);
    }

    private void PrintVisibleElements()
    {
        _output.WriteLine("Visible elements:");
        try
        {
            var elements = _fixture.MainWindow.FindAllDescendants();
            var namedElements = elements.Where(e => !string.IsNullOrEmpty(e.Name)).Take(20);
            foreach (var e in namedElements)
            {
                _output.WriteLine($"  [{e.ControlType}] '{e.Name}'");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"  Error listing elements: {ex.Message}");
        }
    }

    private void HandleAdminLogin()
    {
        _output.WriteLine("Looking for test credentials button...");

        // First, try to find and click the "Fill Test Credentials" button (debug feature)
        var fillTestCredsButton = FindElementByTextContains("Fill Test Credentials");
        if (fillTestCredsButton != null)
        {
            _output.WriteLine("Found 'Fill Test Credentials' button, clicking...");
            fillTestCredsButton.Click();
            Thread.Sleep(500);

            // Now click Sign In
            var signInButton = FindElementByTextContains("SIGN IN");
            if (signInButton != null)
            {
                _output.WriteLine("Clicking SIGN IN button...");
                signInButton.Click();
                Thread.Sleep(2000);
                TakeScreenshot("03_after_login");

                // Check for any error dialogs and dismiss them
                DismissErrorDialog();
                return;
            }
        }

        // Fallback: Manual credential entry
        _output.WriteLine("Fill Test Credentials not found, entering manually...");

        // Default admin credentials from DatabaseSeeder
        const string defaultUsername = "admin";
        const string defaultPassword = "Admin@1234";

        // Find username field
        var usernameBox = _fixture.MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("UsernameTextBox"))?.AsTextBox();
        if (usernameBox != null)
        {
            _output.WriteLine($"Entering username: {defaultUsername}");
            usernameBox.Enter(defaultUsername);
        }
        else
        {
            _output.WriteLine("Username field not found");
            return;
        }

        // Find password field
        var passwordBox = _fixture.MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("PasswordBox"));
        if (passwordBox != null)
        {
            _output.WriteLine("Entering password...");
            passwordBox.Focus();
            Thread.Sleep(200);

            // Type password using keyboard
            foreach (var c in defaultPassword)
            {
                if (c == '@')
                {
                    Keyboard.TypeSimultaneously(VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_2);
                }
                else
                {
                    Keyboard.Type(c.ToString());
                }
                Thread.Sleep(50);
            }
        }
        else
        {
            _output.WriteLine("Password field not found");
            return;
        }

        Thread.Sleep(500);

        // Find and click login button
        var loginButton = _fixture.MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("LoginButton"));
        if (loginButton != null)
        {
            _output.WriteLine("Clicking login button...");
            var button = loginButton.AsButton();
            button?.Invoke();
            Thread.Sleep(2000);
            TakeScreenshot("03_after_login");
        }
        else
        {
            var signInButton = FindElementByTextContains("SIGN IN");
            if (signInButton != null)
            {
                _output.WriteLine("Clicking SIGN IN button...");
                signInButton.Click();
                Thread.Sleep(2000);
                TakeScreenshot("03_after_login");
            }
            else
            {
                _output.WriteLine("Login button not found");
            }
        }

        // Check for any error dialogs and dismiss them
        DismissErrorDialog();
    }

    private void DismissErrorDialog()
    {
        // Look for error dialog and dismiss it
        var okButton = _fixture.MainWindow.FindFirstDescendant(cf => cf.ByName("OK"));
        if (okButton != null)
        {
            _output.WriteLine("Dismissing error dialog...");
            okButton.Click();
            Thread.Sleep(500);
        }
    }

    private void SelectOrCreateCategory(AutomationElement window, string categoryName)
    {
        var categoryCombo = window.FindFirstDescendant(cf => cf.ByAutomationId("CategoryComboBox"));
        if (categoryCombo == null)
        {
            _output.WriteLine("  CategoryComboBox not found");
            return;
        }

        var comboBox = categoryCombo.AsComboBox();
        if (comboBox == null)
        {
            _output.WriteLine("  Could not cast to ComboBox");
            return;
        }

        _output.WriteLine($"  Selecting category using keyboard navigation...");

        // Focus the combobox
        categoryCombo.Focus();
        Thread.Sleep(200);

        // Open dropdown with Down arrow
        Keyboard.Press(VirtualKeyShort.DOWN);
        Thread.Sleep(300);

        // Check if dropdown opened by looking for items
        var items = comboBox.Items;
        _output.WriteLine($"  Found {items.Length} categories in dropdown");

        if (items.Length > 0)
        {
            // Press Down to select first item, then Enter to confirm
            _output.WriteLine("  Pressing Down and Enter to select first category...");
            Keyboard.Press(VirtualKeyShort.DOWN);
            Thread.Sleep(200);
            Keyboard.Press(VirtualKeyShort.ENTER);
            Thread.Sleep(300);

            // Verify selection
            var selectedText = comboBox.EditableText;
            _output.WriteLine($"  Selected category: {selectedText}");
            return;
        }

        // No categories exist - need to create one
        _output.WriteLine($"  No categories found, creating new category: {categoryName}");

        // Press Escape to close empty dropdown
        Keyboard.Press(VirtualKeyShort.ESCAPE);
        Thread.Sleep(200);

        // Type the category name in the editable combobox
        var editableText = categoryCombo.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Edit));
        if (editableText != null)
        {
            editableText.AsTextBox()?.Enter(categoryName);
            Thread.Sleep(500);

            // Look for the "+ Add" button which should appear
            var addButton = window.FindFirstDescendant(cf => cf.ByAutomationId("AddCategoryButton"));
            if (addButton != null)
            {
                _output.WriteLine("  Clicking '+ Add' button to create category...");
                addButton.Click();
                Thread.Sleep(1000);
            }
            else
            {
                // Try finding by text
                var addByText = FindElementByTextContains("+ Add");
                if (addByText != null)
                {
                    _output.WriteLine("  Clicking '+ Add' button (found by text)...");
                    addByText.Click();
                    Thread.Sleep(1000);
                }
                else
                {
                    _output.WriteLine("  '+ Add' button not found - category may not be created");
                }
            }
        }
        else
        {
            // Fallback: focus and type
            categoryCombo.Focus();
            Thread.Sleep(100);
            Keyboard.Type(categoryName);
            Thread.Sleep(500);
        }
    }

    /// <summary>
    /// Attaches to a running instance and inspects the UI tree.
    /// Useful for debugging automation IDs.
    /// </summary>
    [Fact(Skip = "Run manually to inspect running app")]
    public void InspectRunningApp()
    {
        _output.WriteLine("Attaching to running HospitalityPOS...");

        try
        {
            _fixture.AttachToApp("HospitalityPOS");
            _output.WriteLine($"Attached! Main window: {_fixture.MainWindow.Title}");

            // Print UI tree
            PrintUITree(_fixture.MainWindow, 0);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Failed to attach: {ex.Message}");
            _output.WriteLine("Make sure HospitalityPOS is running.");
        }
    }

    private void PrintUITree(AutomationElement element, int depth)
    {
        if (depth > 5) return; // Limit depth

        var indent = new string(' ', depth * 2);
        var automationId = element.AutomationId;
        var name = element.Name;
        var type = element.ControlType;

        _output.WriteLine($"{indent}[{type}] Name='{name}' AutomationId='{automationId}'");

        try
        {
            foreach (var child in element.FindAllChildren())
            {
                PrintUITree(child, depth + 1);
            }
        }
        catch { }
    }

    private AutomationElement? FindElementByTextContains(string text)
    {
        try
        {
            var allElements = _fixture.MainWindow.FindAllDescendants();
            return allElements.FirstOrDefault(e =>
                (e.Name?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (e.AutomationId?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}
