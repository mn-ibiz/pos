using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Infrastructure.Data;

/// <summary>
/// Seeds the database with default data.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Seeds all default data into the database.
    /// Uses a transaction to ensure atomicity - either all data is seeded or none.
    /// Wrapped in execution strategy to support SQL Server retry on failure.
    /// </summary>
    public static async Task SeedAsync(POSDbContext context)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                await SeedRolesAsync(context);
                await SeedPermissionsAsync(context);
                // Save roles and permissions before creating admin user (required for FK relationships)
                await context.SaveChangesAsync();
                await SeedAdminUserAsync(context);
                await SeedPaymentMethodsAsync(context);
                await SeedSystemSettingsAsync(context);
                await SeedChartOfAccountsAsync(context);
                await SeedSalaryComponentsAsync(context);
                await SeedPointsConfigurationAsync(context);
                await SeedTierConfigurationsAsync(context);
                await SeedSampleCategoriesAndProductsAsync(context);
                await SeedExpenseCategoriesAsync(context);

                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    private static async Task SeedRolesAsync(POSDbContext context)
    {
        if (await context.Roles.AnyAsync()) return;

        var roles = new List<Role>
        {
            new() { Name = "Administrator", Description = "Full system access", IsSystem = true },
            new() { Name = "Manager", Description = "Day-to-day operational management", IsSystem = true },
            new() { Name = "Supervisor", Description = "Floor supervision", IsSystem = true },
            new() { Name = "Cashier", Description = "Payment processing", IsSystem = true },
            new() { Name = "Waiter", Description = "Order taking and service", IsSystem = true }
        };

        await context.Roles.AddRangeAsync(roles);
    }

    private static async Task SeedPermissionsAsync(POSDbContext context)
    {
        if (await context.Permissions.AnyAsync()) return;

        var permissions = new List<Permission>
        {
            // Work Period
            new() { Name = "WorkPeriod.Open", Category = "WorkPeriod", Description = "Open work period" },
            new() { Name = "WorkPeriod.Close", Category = "WorkPeriod", Description = "Close work period" },
            new() { Name = "WorkPeriod.ViewHistory", Category = "WorkPeriod", Description = "View work period history" },

            // Sales
            new() { Name = "Sales.Create", Category = "Sales", Description = "Create sales orders" },
            new() { Name = "Sales.ViewOwn", Category = "Sales", Description = "View own sales" },
            new() { Name = "Sales.ViewAll", Category = "Sales", Description = "View all sales" },
            new() { Name = "Sales.Modify", Category = "Sales", Description = "Modify sales orders" },
            new() { Name = "Sales.Void", Category = "Sales", Description = "Void sales orders" },

            // Receipts
            new() { Name = "Receipts.View", Category = "Receipts", Description = "View receipts" },
            new() { Name = "Receipts.Split", Category = "Receipts", Description = "Split receipts" },
            new() { Name = "Receipts.Merge", Category = "Receipts", Description = "Merge receipts" },
            new() { Name = "Receipts.Reprint", Category = "Receipts", Description = "Reprint receipts" },
            new() { Name = "Receipts.Void", Category = "Receipts", Description = "Void receipts" },

            // Products
            new() { Name = "Products.View", Category = "Products", Description = "View products" },
            new() { Name = "Products.Create", Category = "Products", Description = "Create products" },
            new() { Name = "Products.Edit", Category = "Products", Description = "Edit products" },
            new() { Name = "Products.Delete", Category = "Products", Description = "Delete products" },
            new() { Name = "Products.SetPrices", Category = "Products", Description = "Set product prices" },
            new() { Name = "Products.Manage", Category = "Products", Description = "Manage products and categories" },

            // Inventory
            new() { Name = "Inventory.View", Category = "Inventory", Description = "View inventory" },
            new() { Name = "Inventory.Adjust", Category = "Inventory", Description = "Adjust inventory" },
            new() { Name = "Inventory.ReceivePurchase", Category = "Inventory", Description = "Receive purchase orders" },
            new() { Name = "Inventory.FullAccess", Category = "Inventory", Description = "Full inventory access" },

            // Users
            new() { Name = "Users.View", Category = "Users", Description = "View users" },
            new() { Name = "Users.Create", Category = "Users", Description = "Create users" },
            new() { Name = "Users.Edit", Category = "Users", Description = "Edit users" },
            new() { Name = "Users.Delete", Category = "Users", Description = "Delete users" },
            new() { Name = "Users.AssignRoles", Category = "Users", Description = "Assign roles to users" },

            // Reports
            new() { Name = "Reports.XReport", Category = "Reports", Description = "Generate X reports" },
            new() { Name = "Reports.ZReport", Category = "Reports", Description = "Generate Z reports" },
            new() { Name = "Reports.Sales", Category = "Reports", Description = "View sales reports" },
            new() { Name = "Reports.Inventory", Category = "Reports", Description = "View inventory reports" },
            new() { Name = "Reports.Audit", Category = "Reports", Description = "View audit reports" },

            // Discounts
            new() { Name = "Discounts.Apply10", Category = "Discounts", Description = "Apply up to 10% discount" },
            new() { Name = "Discounts.Apply20", Category = "Discounts", Description = "Apply up to 20% discount" },
            new() { Name = "Discounts.Apply50", Category = "Discounts", Description = "Apply up to 50% discount" },
            new() { Name = "Discounts.ApplyAny", Category = "Discounts", Description = "Apply any discount" },

            // Settings
            new() { Name = "Settings.View", Category = "Settings", Description = "View settings" },
            new() { Name = "Settings.Modify", Category = "Settings", Description = "Modify settings" }
        };

        await context.Permissions.AddRangeAsync(permissions);
    }

    private static async Task SeedAdminUserAsync(POSDbContext context)
    {
        if (await context.Users.AnyAsync()) return;

        // Default admin password: Admin@1234 (meets complexity requirements)
        // User must change password on first login
        const string defaultPassword = "Admin@1234";
        const int bcryptWorkFactor = 12;

        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Administrator");
        if (adminRole is null) return;

        var adminUser = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword, bcryptWorkFactor),
            FullName = "System Administrator",
            DisplayName = "Admin",
            Email = "admin@example.com",
            MustChangePassword = true,
            PasswordChangedAt = null,
            IsActive = true
        };

        await context.Users.AddAsync(adminUser);
        await context.SaveChangesAsync();

        // Assign Administrator role
        var userRole = new UserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        };

        await context.UserRoles.AddAsync(userRole);

        // Assign all permissions to Administrator role if not already done
        var allPermissions = await context.Permissions.ToListAsync();
        var existingRolePermissions = await context.RolePermissions
            .Where(rp => rp.RoleId == adminRole.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        foreach (var permission in allPermissions)
        {
            if (!existingRolePermissions.Contains(permission.Id))
            {
                await context.RolePermissions.AddAsync(new RolePermission
                {
                    RoleId = adminRole.Id,
                    PermissionId = permission.Id
                });
            }
        }
    }

    private static async Task SeedPaymentMethodsAsync(POSDbContext context)
    {
        if (await context.PaymentMethods.AnyAsync()) return;

        var paymentMethods = new List<PaymentMethod>
        {
            new() { Name = "Cash", Code = "CASH", IsActive = true, RequiresReference = false, DisplayOrder = 1 },
            new() { Name = "M-Pesa", Code = "MPESA", IsActive = true, RequiresReference = true, DisplayOrder = 2 },
            new() { Name = "Airtel Money", Code = "AIRTEL", IsActive = true, RequiresReference = true, DisplayOrder = 3 },
            new() { Name = "Credit Card", Code = "CARD", IsActive = true, RequiresReference = false, DisplayOrder = 4 },
            new() { Name = "Debit Card", Code = "DEBIT", IsActive = true, RequiresReference = false, DisplayOrder = 5 }
        };

        await context.PaymentMethods.AddRangeAsync(paymentMethods);
    }

    private static async Task SeedSystemSettingsAsync(POSDbContext context)
    {
        if (await context.SystemSettings.AnyAsync()) return;

        var settings = new List<SystemSetting>
        {
            // Business Settings
            new() { SettingKey = "BusinessName", SettingValue = "Hospitality POS", SettingType = "String", Category = "Business", Description = "Business name on receipts" },
            new() { SettingKey = "BusinessAddress", SettingValue = "", SettingType = "String", Category = "Business", Description = "Business address on receipts" },
            new() { SettingKey = "BusinessPhone", SettingValue = "", SettingType = "String", Category = "Business", Description = "Contact phone number" },
            new() { SettingKey = "BusinessEmail", SettingValue = "", SettingType = "String", Category = "Business", Description = "Contact email" },
            new() { SettingKey = "KRAPinNumber", SettingValue = "", SettingType = "String", Category = "Business", Description = "KRA PIN Number" },

            // Tax Settings
            new() { SettingKey = "TaxRate", SettingValue = "16", SettingType = "Decimal", Category = "Tax", Description = "Default VAT rate" },
            new() { SettingKey = "TaxIncluded", SettingValue = "true", SettingType = "Bool", Category = "Tax", Description = "Prices include VAT" },

            // Regional Settings
            new() { SettingKey = "Currency", SettingValue = "KES", SettingType = "String", Category = "Regional", Description = "Currency code" },
            new() { SettingKey = "CurrencySymbol", SettingValue = "KSh", SettingType = "String", Category = "Regional", Description = "Currency symbol" },
            new() { SettingKey = "DecimalPlaces", SettingValue = "2", SettingType = "Int", Category = "Regional", Description = "Decimal places for amounts" },

            // Receipt Settings
            new() { SettingKey = "AutoSettleOnPrint", SettingValue = "false", SettingType = "Bool", Category = "Receipts", Description = "Auto-settle receipts when printed" },
            new() { SettingKey = "ReceiptFooterMessage", SettingValue = "Thank you for your business!", SettingType = "String", Category = "Receipts", Description = "Footer message on receipts" },

            // Security Settings
            new() { SettingKey = "SessionTimeout", SettingValue = "30", SettingType = "Int", Category = "Security", Description = "Session timeout in minutes" },
            new() { SettingKey = "MaxFailedLoginAttempts", SettingValue = "5", SettingType = "Int", Category = "Security", Description = "Max failed login attempts before lockout" },
            new() { SettingKey = "LockoutMinutes", SettingValue = "15", SettingType = "Int", Category = "Security", Description = "Lockout duration in minutes" },
            new() { SettingKey = "RequireVoidReason", SettingValue = "true", SettingType = "Bool", Category = "Security", Description = "Require reason for voids" },

            // Operations Settings
            new() { SettingKey = "BusinessMode", SettingValue = "Restaurant", SettingType = "String", Category = "Operations", Description = "Business mode (Restaurant, Supermarket, Hybrid)" },
            new() { SettingKey = "EnableTableManagement", SettingValue = "true", SettingType = "Bool", Category = "Operations", Description = "Enable table management" },
            new() { SettingKey = "EnableKitchenDisplay", SettingValue = "true", SettingType = "Bool", Category = "Operations", Description = "Enable kitchen display" },
            new() { SettingKey = "EnableBarcodeAutoFocus", SettingValue = "false", SettingType = "Bool", Category = "Operations", Description = "Enable barcode auto-focus for supermarket mode" }
        };

        await context.SystemSettings.AddRangeAsync(settings);
    }

    private static async Task SeedChartOfAccountsAsync(POSDbContext context)
    {
        if (await context.ChartOfAccounts.AnyAsync()) return;

        var accounts = new List<ChartOfAccount>
        {
            // Assets
            new() { AccountCode = "1000", AccountName = "Cash", AccountType = AccountType.Asset, IsSystemAccount = true },
            new() { AccountCode = "1010", AccountName = "Bank Account", AccountType = AccountType.Asset, IsSystemAccount = true },
            new() { AccountCode = "1100", AccountName = "Accounts Receivable", AccountType = AccountType.Asset, IsSystemAccount = true },
            new() { AccountCode = "1200", AccountName = "Inventory", AccountType = AccountType.Asset, IsSystemAccount = true },

            // Liabilities
            new() { AccountCode = "2000", AccountName = "Accounts Payable", AccountType = AccountType.Liability, IsSystemAccount = true },
            new() { AccountCode = "2100", AccountName = "Salaries Payable", AccountType = AccountType.Liability, IsSystemAccount = true },
            new() { AccountCode = "2200", AccountName = "Tax Payable", AccountType = AccountType.Liability, IsSystemAccount = true },

            // Revenue
            new() { AccountCode = "4000", AccountName = "Sales Revenue", AccountType = AccountType.Revenue, IsSystemAccount = true },
            new() { AccountCode = "4100", AccountName = "Other Income", AccountType = AccountType.Revenue, IsSystemAccount = true },

            // Expenses
            new() { AccountCode = "5000", AccountName = "Cost of Goods Sold", AccountType = AccountType.Expense, IsSystemAccount = true },
            new() { AccountCode = "5100", AccountName = "Salaries Expense", AccountType = AccountType.Expense, IsSystemAccount = true },
            new() { AccountCode = "5200", AccountName = "Rent Expense", AccountType = AccountType.Expense, IsSystemAccount = true },
            new() { AccountCode = "5300", AccountName = "Utilities Expense", AccountType = AccountType.Expense, IsSystemAccount = true },
            new() { AccountCode = "5400", AccountName = "Supplies Expense", AccountType = AccountType.Expense, IsSystemAccount = true },
            new() { AccountCode = "5500", AccountName = "Other Expenses", AccountType = AccountType.Expense, IsSystemAccount = true }
        };

        await context.ChartOfAccounts.AddRangeAsync(accounts);
    }

    private static async Task SeedSalaryComponentsAsync(POSDbContext context)
    {
        if (await context.SalaryComponents.AnyAsync()) return;

        var components = new List<SalaryComponent>
        {
            // Earnings
            new() { Name = "Basic Salary", ComponentType = ComponentType.Earning, IsFixed = true, IsTaxable = true, IsStatutory = false, DisplayOrder = 1 },
            new() { Name = "House Allowance", ComponentType = ComponentType.Earning, IsFixed = true, IsTaxable = true, IsStatutory = false, DisplayOrder = 2 },
            new() { Name = "Transport Allowance", ComponentType = ComponentType.Earning, IsFixed = true, IsTaxable = true, IsStatutory = false, DisplayOrder = 3 },
            new() { Name = "Overtime", ComponentType = ComponentType.Earning, IsFixed = false, IsTaxable = true, IsStatutory = false, DisplayOrder = 4 },
            new() { Name = "Bonus", ComponentType = ComponentType.Earning, IsFixed = true, IsTaxable = true, IsStatutory = false, DisplayOrder = 5 },

            // Deductions - Statutory (Kenya)
            new() { Name = "PAYE", ComponentType = ComponentType.Deduction, IsFixed = false, IsTaxable = false, IsStatutory = true, DisplayOrder = 10, Description = "Pay As You Earn Tax" },
            new() { Name = "NHIF", ComponentType = ComponentType.Deduction, IsFixed = false, IsTaxable = false, IsStatutory = true, DisplayOrder = 11, Description = "National Hospital Insurance Fund" },
            new() { Name = "NSSF", ComponentType = ComponentType.Deduction, IsFixed = false, IsTaxable = false, IsStatutory = true, DisplayOrder = 12, Description = "National Social Security Fund" },
            new() { Name = "Housing Levy", ComponentType = ComponentType.Deduction, IsFixed = false, DefaultPercent = 1.5m, IsTaxable = false, IsStatutory = true, DisplayOrder = 13, Description = "Affordable Housing Levy" },

            // Deductions - Non-Statutory
            new() { Name = "Loan Repayment", ComponentType = ComponentType.Deduction, IsFixed = true, IsTaxable = false, IsStatutory = false, DisplayOrder = 20 },
            new() { Name = "Advance Deduction", ComponentType = ComponentType.Deduction, IsFixed = true, IsTaxable = false, IsStatutory = false, DisplayOrder = 21 }
        };

        await context.SalaryComponents.AddRangeAsync(components);
    }

    private static async Task SeedPointsConfigurationAsync(POSDbContext context)
    {
        if (await context.PointsConfigurations.AnyAsync()) return;

        var defaultConfig = new PointsConfiguration
        {
            Name = "Default",
            EarningRate = 100m,         // KSh 100 = 1 point
            RedemptionValue = 1m,       // 1 point = KSh 1
            MinimumRedemptionPoints = 100,
            MaximumRedemptionPoints = 0, // No limit
            MaxRedemptionPercentage = 50, // Max 50% of transaction
            EarnOnDiscountedItems = true,
            EarnOnTax = false,
            PointsExpiryDays = 365,     // Points expire after 1 year
            IsDefault = true,
            Description = "Default loyalty points configuration for retail customers",
            IsActive = true
        };

        await context.PointsConfigurations.AddAsync(defaultConfig);
    }

    private static async Task SeedTierConfigurationsAsync(POSDbContext context)
    {
        if (await context.TierConfigurations.AnyAsync()) return;

        var tiers = new List<TierConfiguration>
        {
            new()
            {
                Tier = MembershipTier.Bronze,
                Name = "Bronze",
                Description = "Entry-level membership for all new loyalty members",
                SpendThreshold = 0m,
                PointsThreshold = 0m,
                PointsMultiplier = 1.0m,
                DiscountPercent = 0m,
                FreeDelivery = false,
                PriorityService = false,
                SortOrder = 1,
                ColorCode = "#CD7F32",
                IconName = "StarOutline",
                IsActive = true
            },
            new()
            {
                Tier = MembershipTier.Silver,
                Name = "Silver",
                Description = "Achieved after KSh 25,000 lifetime spend or 250 points",
                SpendThreshold = 25000m,
                PointsThreshold = 250m,
                PointsMultiplier = 1.25m,
                DiscountPercent = 5m,
                FreeDelivery = false,
                PriorityService = false,
                SortOrder = 2,
                ColorCode = "#C0C0C0",
                IconName = "StarHalf",
                IsActive = true
            },
            new()
            {
                Tier = MembershipTier.Gold,
                Name = "Gold",
                Description = "Achieved after KSh 75,000 lifetime spend or 750 points",
                SpendThreshold = 75000m,
                PointsThreshold = 750m,
                PointsMultiplier = 1.5m,
                DiscountPercent = 10m,
                FreeDelivery = true,
                PriorityService = false,
                SortOrder = 3,
                ColorCode = "#FFD700",
                IconName = "Star",
                IsActive = true
            },
            new()
            {
                Tier = MembershipTier.Platinum,
                Name = "Platinum",
                Description = "Premium tier for VIP customers - KSh 150,000 lifetime spend or 1,500 points",
                SpendThreshold = 150000m,
                PointsThreshold = 1500m,
                PointsMultiplier = 2.0m,
                DiscountPercent = 15m,
                FreeDelivery = true,
                PriorityService = true,
                SortOrder = 4,
                ColorCode = "#E5E4E2",
                IconName = "StarCircle",
                IsActive = true
            }
        };

        await context.TierConfigurations.AddRangeAsync(tiers);
    }

    private static async Task SeedSampleCategoriesAndProductsAsync(POSDbContext context)
    {
        if (await context.Categories.AnyAsync()) return;

        // Sample categories - works for both Restaurant and Supermarket modes
        var categories = new List<Category>
        {
            // Restaurant categories
            new() { Name = "Beverages", DisplayOrder = 1, IsActive = true },
            new() { Name = "Main Dishes", DisplayOrder = 2, IsActive = true },
            new() { Name = "Appetizers", DisplayOrder = 3, IsActive = true },
            new() { Name = "Desserts", DisplayOrder = 4, IsActive = true },
            // Supermarket categories
            new() { Name = "Groceries", DisplayOrder = 5, IsActive = true },
            new() { Name = "Dairy", DisplayOrder = 6, IsActive = true },
            new() { Name = "Bakery", DisplayOrder = 7, IsActive = true }
        };

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();

        // Get categories for product assignment
        var beverages = categories.First(c => c.Name == "Beverages");
        var mainDishes = categories.First(c => c.Name == "Main Dishes");
        var appetizers = categories.First(c => c.Name == "Appetizers");
        var desserts = categories.First(c => c.Name == "Desserts");
        var groceries = categories.First(c => c.Name == "Groceries");
        var dairy = categories.First(c => c.Name == "Dairy");
        var bakery = categories.First(c => c.Name == "Bakery");

        // Sample products
        var products = new List<Product>
        {
            // Beverages
            new() { Code = "BEV001", Name = "Coffee", SellingPrice = 150m, CategoryId = beverages.Id, SKU = "BEV001", IsActive = true, TrackInventory = false },
            new() { Code = "BEV002", Name = "Tea", SellingPrice = 100m, CategoryId = beverages.Id, SKU = "BEV002", IsActive = true, TrackInventory = false },
            new() { Code = "BEV003", Name = "Soda (500ml)", SellingPrice = 80m, CategoryId = beverages.Id, SKU = "BEV003", IsActive = true, TrackInventory = true, StockQuantity = 100 },
            new() { Code = "BEV004", Name = "Fresh Juice", SellingPrice = 200m, CategoryId = beverages.Id, SKU = "BEV004", IsActive = true, TrackInventory = false },
            new() { Code = "BEV005", Name = "Mineral Water", SellingPrice = 50m, CategoryId = beverages.Id, SKU = "BEV005", IsActive = true, TrackInventory = true, StockQuantity = 200 },

            // Main Dishes
            new() { Code = "MAIN001", Name = "Chicken Curry", SellingPrice = 650m, CategoryId = mainDishes.Id, SKU = "MAIN001", IsActive = true, TrackInventory = false },
            new() { Code = "MAIN002", Name = "Beef Stew", SellingPrice = 750m, CategoryId = mainDishes.Id, SKU = "MAIN002", IsActive = true, TrackInventory = false },
            new() { Code = "MAIN003", Name = "Fish Fillet", SellingPrice = 850m, CategoryId = mainDishes.Id, SKU = "MAIN003", IsActive = true, TrackInventory = false },
            new() { Code = "MAIN004", Name = "Ugali & Sukuma", SellingPrice = 250m, CategoryId = mainDishes.Id, SKU = "MAIN004", IsActive = true, TrackInventory = false },
            new() { Code = "MAIN005", Name = "Pilau Rice", SellingPrice = 400m, CategoryId = mainDishes.Id, SKU = "MAIN005", IsActive = true, TrackInventory = false },
            new() { Code = "MAIN006", Name = "Nyama Choma (500g)", SellingPrice = 800m, CategoryId = mainDishes.Id, SKU = "MAIN006", IsActive = true, TrackInventory = false },

            // Appetizers
            new() { Code = "APP001", Name = "Samosa (2pcs)", SellingPrice = 100m, CategoryId = appetizers.Id, SKU = "APP001", IsActive = true, TrackInventory = false },
            new() { Code = "APP002", Name = "Spring Rolls", SellingPrice = 150m, CategoryId = appetizers.Id, SKU = "APP002", IsActive = true, TrackInventory = false },
            new() { Code = "APP003", Name = "Chips (Fries)", SellingPrice = 200m, CategoryId = appetizers.Id, SKU = "APP003", IsActive = true, TrackInventory = false },
            new() { Code = "APP004", Name = "Soup of the Day", SellingPrice = 180m, CategoryId = appetizers.Id, SKU = "APP004", IsActive = true, TrackInventory = false },

            // Desserts
            new() { Code = "DES001", Name = "Ice Cream", SellingPrice = 150m, CategoryId = desserts.Id, SKU = "DES001", IsActive = true, TrackInventory = false },
            new() { Code = "DES002", Name = "Cake Slice", SellingPrice = 200m, CategoryId = desserts.Id, SKU = "DES002", IsActive = true, TrackInventory = false },
            new() { Code = "DES003", Name = "Fruit Salad", SellingPrice = 180m, CategoryId = desserts.Id, SKU = "DES003", IsActive = true, TrackInventory = false },

            // Groceries (Supermarket)
            new() { Code = "GRO001", Name = "Sugar (1kg)", SellingPrice = 180m, CategoryId = groceries.Id, SKU = "GRO001", Barcode = "6001234567890", IsActive = true, TrackInventory = true, StockQuantity = 50 },
            new() { Code = "GRO002", Name = "Rice (2kg)", SellingPrice = 350m, CategoryId = groceries.Id, SKU = "GRO002", Barcode = "6001234567891", IsActive = true, TrackInventory = true, StockQuantity = 40 },
            new() { Code = "GRO003", Name = "Cooking Oil (1L)", SellingPrice = 280m, CategoryId = groceries.Id, SKU = "GRO003", Barcode = "6001234567892", IsActive = true, TrackInventory = true, StockQuantity = 30 },
            new() { Code = "GRO004", Name = "Salt (500g)", SellingPrice = 50m, CategoryId = groceries.Id, SKU = "GRO004", Barcode = "6001234567893", IsActive = true, TrackInventory = true, StockQuantity = 100 },
            new() { Code = "GRO005", Name = "Maize Flour (2kg)", SellingPrice = 180m, CategoryId = groceries.Id, SKU = "GRO005", Barcode = "6001234567894", IsActive = true, TrackInventory = true, StockQuantity = 60 },

            // Dairy
            new() { Code = "DAI001", Name = "Fresh Milk (500ml)", SellingPrice = 65m, CategoryId = dairy.Id, SKU = "DAI001", Barcode = "6001234567895", IsActive = true, TrackInventory = true, StockQuantity = 50 },
            new() { Code = "DAI002", Name = "Yoghurt (500ml)", SellingPrice = 120m, CategoryId = dairy.Id, SKU = "DAI002", Barcode = "6001234567896", IsActive = true, TrackInventory = true, StockQuantity = 30 },
            new() { Code = "DAI003", Name = "Butter (250g)", SellingPrice = 250m, CategoryId = dairy.Id, SKU = "DAI003", Barcode = "6001234567897", IsActive = true, TrackInventory = true, StockQuantity = 20 },
            new() { Code = "DAI004", Name = "Cheese (200g)", SellingPrice = 350m, CategoryId = dairy.Id, SKU = "DAI004", Barcode = "6001234567898", IsActive = true, TrackInventory = true, StockQuantity = 15 },

            // Bakery
            new() { Code = "BAK001", Name = "Bread Loaf", SellingPrice = 60m, CategoryId = bakery.Id, SKU = "BAK001", Barcode = "6001234567899", IsActive = true, TrackInventory = true, StockQuantity = 30 },
            new() { Code = "BAK002", Name = "Buns (6 pack)", SellingPrice = 80m, CategoryId = bakery.Id, SKU = "BAK002", Barcode = "6001234567900", IsActive = true, TrackInventory = true, StockQuantity = 25 },
            new() { Code = "BAK003", Name = "Croissant", SellingPrice = 100m, CategoryId = bakery.Id, SKU = "BAK003", Barcode = "6001234567901", IsActive = true, TrackInventory = true, StockQuantity = 20 },
            new() { Code = "BAK004", Name = "Doughnut", SellingPrice = 50m, CategoryId = bakery.Id, SKU = "BAK004", Barcode = "6001234567902", IsActive = true, TrackInventory = true, StockQuantity = 40 }
        };

        await context.Products.AddRangeAsync(products);
    }

    private static async Task SeedExpenseCategoriesAsync(POSDbContext context)
    {
        if (await context.ExpenseCategories.AnyAsync()) return;

        // Parent categories
        var cogsParent = new ExpenseCategory
        {
            Name = "Cost of Goods Sold (COGS)",
            Description = "Direct costs of products sold - Food, Beverage, Packaging",
            Type = ExpenseCategoryType.COGS,
            Icon = "ShoppingCart",
            Color = "#EF4444",
            SortOrder = 1,
            IsSystemCategory = true,
            IsActive = true
        };

        var laborParent = new ExpenseCategory
        {
            Name = "Labor Costs",
            Description = "Employee wages, benefits, and related expenses",
            Type = ExpenseCategoryType.Labor,
            Icon = "Users",
            Color = "#F59E0B",
            SortOrder = 2,
            IsSystemCategory = true,
            IsActive = true
        };

        var occupancyParent = new ExpenseCategory
        {
            Name = "Occupancy",
            Description = "Rent, utilities, and property-related expenses",
            Type = ExpenseCategoryType.Occupancy,
            Icon = "Building",
            Color = "#3B82F6",
            SortOrder = 3,
            IsSystemCategory = true,
            IsActive = true
        };

        var operatingParent = new ExpenseCategory
        {
            Name = "Operating Expenses",
            Description = "Day-to-day operational expenses",
            Type = ExpenseCategoryType.Operating,
            Icon = "Cog",
            Color = "#8B5CF6",
            SortOrder = 4,
            IsSystemCategory = true,
            IsActive = true
        };

        var marketingParent = new ExpenseCategory
        {
            Name = "Marketing & Advertising",
            Description = "Promotional and advertising expenses",
            Type = ExpenseCategoryType.Marketing,
            Icon = "Megaphone",
            Color = "#EC4899",
            SortOrder = 5,
            IsSystemCategory = true,
            IsActive = true
        };

        var adminParent = new ExpenseCategory
        {
            Name = "Administrative",
            Description = "Office, professional services, and administrative costs",
            Type = ExpenseCategoryType.Administrative,
            Icon = "Briefcase",
            Color = "#6366F1",
            SortOrder = 6,
            IsSystemCategory = true,
            IsActive = true
        };

        var otherParent = new ExpenseCategory
        {
            Name = "Other Expenses",
            Description = "Miscellaneous and other expenses",
            Type = ExpenseCategoryType.Other,
            Icon = "DotsHorizontal",
            Color = "#6B7280",
            SortOrder = 7,
            IsSystemCategory = true,
            IsActive = true
        };

        await context.ExpenseCategories.AddRangeAsync(new[]
        {
            cogsParent, laborParent, occupancyParent, operatingParent,
            marketingParent, adminParent, otherParent
        });
        await context.SaveChangesAsync();

        // COGS Subcategories
        var cogsSubcategories = new List<ExpenseCategory>
        {
            new() { Name = "Food Purchases", Description = "Raw food ingredients", ParentCategoryId = cogsParent.Id, Type = ExpenseCategoryType.COGS, Icon = "Apple", Color = "#FCA5A5", SortOrder = 1, IsActive = true },
            new() { Name = "Beverage Purchases", Description = "Drinks and beverage supplies", ParentCategoryId = cogsParent.Id, Type = ExpenseCategoryType.COGS, Icon = "Beer", Color = "#FCA5A5", SortOrder = 2, IsActive = true },
            new() { Name = "Packaging & Disposables", Description = "Takeaway containers, napkins, etc.", ParentCategoryId = cogsParent.Id, Type = ExpenseCategoryType.COGS, Icon = "Package", Color = "#FCA5A5", SortOrder = 3, IsActive = true },
        };

        // Labor Subcategories
        var laborSubcategories = new List<ExpenseCategory>
        {
            new() { Name = "Wages & Salaries", Description = "Employee base pay", ParentCategoryId = laborParent.Id, Type = ExpenseCategoryType.Labor, Icon = "CurrencyDollar", Color = "#FCD34D", SortOrder = 1, IsActive = true },
            new() { Name = "Payroll Taxes", Description = "PAYE, NSSF, NHIF contributions", ParentCategoryId = laborParent.Id, Type = ExpenseCategoryType.Labor, Icon = "Receipt", Color = "#FCD34D", SortOrder = 2, IsActive = true },
            new() { Name = "Employee Benefits", Description = "Health insurance, meals, etc.", ParentCategoryId = laborParent.Id, Type = ExpenseCategoryType.Labor, Icon = "Heart", Color = "#FCD34D", SortOrder = 3, IsActive = true },
            new() { Name = "Training & Development", Description = "Staff training costs", ParentCategoryId = laborParent.Id, Type = ExpenseCategoryType.Labor, Icon = "AcademicCap", Color = "#FCD34D", SortOrder = 4, IsActive = true },
        };

        // Occupancy Subcategories
        var occupancySubcategories = new List<ExpenseCategory>
        {
            new() { Name = "Rent/Lease", Description = "Monthly rent payments", ParentCategoryId = occupancyParent.Id, Type = ExpenseCategoryType.Occupancy, Icon = "Home", Color = "#93C5FD", SortOrder = 1, IsActive = true },
            new() { Name = "Electricity", Description = "Power bills", ParentCategoryId = occupancyParent.Id, Type = ExpenseCategoryType.Occupancy, Icon = "LightningBolt", Color = "#93C5FD", SortOrder = 2, IsActive = true },
            new() { Name = "Water & Sewer", Description = "Water utility bills", ParentCategoryId = occupancyParent.Id, Type = ExpenseCategoryType.Occupancy, Icon = "Drop", Color = "#93C5FD", SortOrder = 3, IsActive = true },
            new() { Name = "Gas", Description = "Cooking gas and heating", ParentCategoryId = occupancyParent.Id, Type = ExpenseCategoryType.Occupancy, Icon = "Fire", Color = "#93C5FD", SortOrder = 4, IsActive = true },
            new() { Name = "Internet & Phone", Description = "Telecommunications", ParentCategoryId = occupancyParent.Id, Type = ExpenseCategoryType.Occupancy, Icon = "Wifi", Color = "#93C5FD", SortOrder = 5, IsActive = true },
            new() { Name = "Property Insurance", Description = "Building and contents insurance", ParentCategoryId = occupancyParent.Id, Type = ExpenseCategoryType.Occupancy, Icon = "Shield", Color = "#93C5FD", SortOrder = 6, IsActive = true },
            new() { Name = "Trash Removal", Description = "Waste management services", ParentCategoryId = occupancyParent.Id, Type = ExpenseCategoryType.Occupancy, Icon = "Trash", Color = "#93C5FD", SortOrder = 7, IsActive = true },
        };

        // Operating Subcategories
        var operatingSubcategories = new List<ExpenseCategory>
        {
            new() { Name = "Cleaning Supplies", Description = "Cleaning materials and chemicals", ParentCategoryId = operatingParent.Id, Type = ExpenseCategoryType.Operating, Icon = "Sparkles", Color = "#C4B5FD", SortOrder = 1, IsActive = true },
            new() { Name = "Repairs & Maintenance", Description = "Equipment and facility repairs", ParentCategoryId = operatingParent.Id, Type = ExpenseCategoryType.Operating, Icon = "Wrench", Color = "#C4B5FD", SortOrder = 2, IsActive = true },
            new() { Name = "Equipment (Small)", Description = "Small equipment purchases", ParentCategoryId = operatingParent.Id, Type = ExpenseCategoryType.Operating, Icon = "Cube", Color = "#C4B5FD", SortOrder = 3, IsActive = true },
            new() { Name = "Office Supplies", Description = "Stationery and office materials", ParentCategoryId = operatingParent.Id, Type = ExpenseCategoryType.Operating, Icon = "Pencil", Color = "#C4B5FD", SortOrder = 4, IsActive = true },
            new() { Name = "Bank & CC Fees", Description = "Bank charges and card processing fees", ParentCategoryId = operatingParent.Id, Type = ExpenseCategoryType.Operating, Icon = "CreditCard", Color = "#C4B5FD", SortOrder = 5, IsActive = true },
            new() { Name = "Licenses & Permits", Description = "Business licenses and permits", ParentCategoryId = operatingParent.Id, Type = ExpenseCategoryType.Operating, Icon = "DocumentText", Color = "#C4B5FD", SortOrder = 6, IsActive = true },
            new() { Name = "Delivery & Transport", Description = "Delivery and transportation costs", ParentCategoryId = operatingParent.Id, Type = ExpenseCategoryType.Operating, Icon = "Truck", Color = "#C4B5FD", SortOrder = 7, IsActive = true },
        };

        // Marketing Subcategories
        var marketingSubcategories = new List<ExpenseCategory>
        {
            new() { Name = "Online Advertising", Description = "Digital ads, social media", ParentCategoryId = marketingParent.Id, Type = ExpenseCategoryType.Marketing, Icon = "Globe", Color = "#F9A8D4", SortOrder = 1, IsActive = true },
            new() { Name = "Print Materials", Description = "Menus, flyers, business cards", ParentCategoryId = marketingParent.Id, Type = ExpenseCategoryType.Marketing, Icon = "Newspaper", Color = "#F9A8D4", SortOrder = 2, IsActive = true },
            new() { Name = "Promotions & Discounts", Description = "Special offers and discounts cost", ParentCategoryId = marketingParent.Id, Type = ExpenseCategoryType.Marketing, Icon = "Tag", Color = "#F9A8D4", SortOrder = 3, IsActive = true },
            new() { Name = "Photography & Media", Description = "Food photography, video production", ParentCategoryId = marketingParent.Id, Type = ExpenseCategoryType.Marketing, Icon = "Camera", Color = "#F9A8D4", SortOrder = 4, IsActive = true },
        };

        // Admin Subcategories
        var adminSubcategories = new List<ExpenseCategory>
        {
            new() { Name = "Accounting Services", Description = "Bookkeeping and accounting fees", ParentCategoryId = adminParent.Id, Type = ExpenseCategoryType.Administrative, Icon = "Calculator", Color = "#A5B4FC", SortOrder = 1, IsActive = true },
            new() { Name = "Legal Services", Description = "Legal fees and consultation", ParentCategoryId = adminParent.Id, Type = ExpenseCategoryType.Administrative, Icon = "Scale", Color = "#A5B4FC", SortOrder = 2, IsActive = true },
            new() { Name = "Security Services", Description = "Security guards and systems", ParentCategoryId = adminParent.Id, Type = ExpenseCategoryType.Administrative, Icon = "ShieldCheck", Color = "#A5B4FC", SortOrder = 3, IsActive = true },
            new() { Name = "Software & Subscriptions", Description = "POS, accounting software, etc.", ParentCategoryId = adminParent.Id, Type = ExpenseCategoryType.Administrative, Icon = "DesktopComputer", Color = "#A5B4FC", SortOrder = 4, IsActive = true },
            new() { Name = "Professional Development", Description = "Conferences, memberships", ParentCategoryId = adminParent.Id, Type = ExpenseCategoryType.Administrative, Icon = "UserGroup", Color = "#A5B4FC", SortOrder = 5, IsActive = true },
        };

        // Other Subcategories
        var otherSubcategories = new List<ExpenseCategory>
        {
            new() { Name = "Seasonal/Holiday", Description = "Seasonal decorations and expenses", ParentCategoryId = otherParent.Id, Type = ExpenseCategoryType.Other, Icon = "Gift", Color = "#9CA3AF", SortOrder = 1, IsActive = true },
            new() { Name = "Charitable Contributions", Description = "Donations and sponsorships", ParentCategoryId = otherParent.Id, Type = ExpenseCategoryType.Other, Icon = "HandRaised", Color = "#9CA3AF", SortOrder = 2, IsActive = true },
            new() { Name = "Miscellaneous", Description = "Other uncategorized expenses", ParentCategoryId = otherParent.Id, Type = ExpenseCategoryType.Other, Icon = "Puzzle", Color = "#9CA3AF", SortOrder = 3, IsActive = true },
        };

        // Add all subcategories
        await context.ExpenseCategories.AddRangeAsync(cogsSubcategories);
        await context.ExpenseCategories.AddRangeAsync(laborSubcategories);
        await context.ExpenseCategories.AddRangeAsync(occupancySubcategories);
        await context.ExpenseCategories.AddRangeAsync(operatingSubcategories);
        await context.ExpenseCategories.AddRangeAsync(marketingSubcategories);
        await context.ExpenseCategories.AddRangeAsync(adminSubcategories);
        await context.ExpenseCategories.AddRangeAsync(otherSubcategories);
    }
}
