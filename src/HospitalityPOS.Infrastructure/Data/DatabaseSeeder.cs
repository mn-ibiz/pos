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
                await SeedAdminUserAsync(context);
                await SeedPaymentMethodsAsync(context);
                await SeedSystemSettingsAsync(context);
                await SeedChartOfAccountsAsync(context);
                await SeedSalaryComponentsAsync(context);
                await SeedPointsConfigurationAsync(context);
                await SeedTierConfigurationsAsync(context);

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

        // Default admin password: Admin@123 (meets complexity requirements)
        // User must change password on first login
        const string defaultPassword = "Admin@123";
        const int bcryptWorkFactor = 12;

        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Administrator");
        if (adminRole is null) return;

        var adminUser = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword, bcryptWorkFactor),
            FullName = "System Administrator",
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
}
