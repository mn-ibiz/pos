namespace HospitalityPOS.Core.Constants;

/// <summary>
/// Contains all permission name constants used for authorization.
/// These constants match the permission names seeded in the database.
/// </summary>
public static class PermissionNames
{
    /// <summary>
    /// Work Period permissions.
    /// </summary>
    public static class WorkPeriod
    {
        public const string Open = "WorkPeriod.Open";
        public const string Close = "WorkPeriod.Close";
        public const string ViewHistory = "WorkPeriod.ViewHistory";
    }

    /// <summary>
    /// Sales permissions.
    /// </summary>
    public static class Sales
    {
        public const string Create = "Sales.Create";
        public const string ViewOwn = "Sales.ViewOwn";
        public const string ViewAll = "Sales.ViewAll";
        public const string Modify = "Sales.Modify";
        public const string Void = "Sales.Void";
    }

    /// <summary>
    /// Receipts permissions.
    /// </summary>
    public static class Receipts
    {
        public const string View = "Receipts.View";
        public const string Split = "Receipts.Split";
        public const string Merge = "Receipts.Merge";
        public const string Reprint = "Receipts.Reprint";
        public const string Void = "Receipts.Void";
        public const string ModifyOwn = "Receipts.ModifyOwn";
        public const string ModifyOther = "Receipts.ModifyOther";
        public const string ModifyAny = "Receipts.ModifyAny";
    }

    /// <summary>
    /// Products permissions.
    /// </summary>
    public static class Products
    {
        public const string View = "Products.View";
        public const string Create = "Products.Create";
        public const string Edit = "Products.Edit";
        public const string Delete = "Products.Delete";
        public const string SetPrices = "Products.SetPrices";
        public const string Manage = "Products.Manage";
    }

    /// <summary>
    /// Inventory permissions.
    /// </summary>
    public static class Inventory
    {
        public const string View = "Inventory.View";
        public const string Adjust = "Inventory.Adjust";
        public const string ReceivePurchase = "Inventory.ReceivePurchase";
        public const string FullAccess = "Inventory.FullAccess";
    }

    /// <summary>
    /// Users permissions.
    /// </summary>
    public static class Users
    {
        public const string View = "Users.View";
        public const string Create = "Users.Create";
        public const string Edit = "Users.Edit";
        public const string Delete = "Users.Delete";
        public const string AssignRoles = "Users.AssignRoles";
    }

    /// <summary>
    /// Reports permissions.
    /// </summary>
    public static class Reports
    {
        public const string XReport = "Reports.XReport";
        public const string ZReport = "Reports.ZReport";
        public const string Sales = "Reports.Sales";
        public const string Inventory = "Reports.Inventory";
        public const string Audit = "Reports.Audit";
    }

    /// <summary>
    /// Discounts permissions.
    /// </summary>
    public static class Discounts
    {
        public const string Apply10 = "Discounts.Apply10";
        public const string Apply20 = "Discounts.Apply20";
        public const string Apply50 = "Discounts.Apply50";
        public const string ApplyAny = "Discounts.ApplyAny";
    }

    /// <summary>
    /// Settings permissions.
    /// </summary>
    public static class Settings
    {
        public const string View = "Settings.View";
        public const string Modify = "Settings.Modify";
    }
}
