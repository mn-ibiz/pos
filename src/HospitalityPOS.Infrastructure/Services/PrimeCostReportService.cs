using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Reports;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for generating prime cost and financial analysis reports.
/// </summary>
public class PrimeCostReportService : IPrimeCostReportService
{
    private readonly IDbContextFactory<POSDbContext> _contextFactory;
    private readonly ILogger<PrimeCostReportService> _logger;

    public PrimeCostReportService(
        IDbContextFactory<POSDbContext> contextFactory,
        ILogger<PrimeCostReportService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<PrimeCostReport> GeneratePrimeCostReportAsync(
        PrimeCostReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var report = new PrimeCostReport
        {
            StartDate = parameters.StartDate,
            EndDate = parameters.EndDate,
            GeneratedAt = DateTime.UtcNow
        };

        // Get revenue from orders
        var orders = await context.Orders
            .Where(o => o.CreatedAt >= parameters.StartDate && o.CreatedAt <= parameters.EndDate)
            .Where(o => o.Status == OrderStatus.Served)
            .ToListAsync(cancellationToken);

        report.GrossRevenue = orders.Sum(o => o.TotalAmount);
        report.Discounts = orders.Sum(o => o.DiscountAmount);

        // Calculate COGS from order items
        var orderIds = orders.Select(o => o.Id).ToList();
        var orderItems = await context.OrderItems
            .Where(oi => orderIds.Contains(oi.OrderId))
            .Include(oi => oi.Product)
            .ThenInclude(p => p!.Category)
            .ToListAsync(cancellationToken);

        // Calculate food and beverage costs
        decimal foodCost = 0;
        decimal beverageCost = 0;

        foreach (var item in orderItems)
        {
            if (item.Product == null) continue;
            var cost = (item.Product.CostPrice ?? 0) * item.Quantity;

            // Categorize by product category (food vs beverage)
            var categoryName = item.Product.Category?.Name ?? "";
            if (categoryName.Contains("Beverage", StringComparison.OrdinalIgnoreCase) ||
                categoryName.Contains("Drink", StringComparison.OrdinalIgnoreCase))
            {
                beverageCost += cost;
            }
            else
            {
                foodCost += cost;
            }
        }

        report.FoodCost = foodCost;
        report.BeverageCost = beverageCost;

        // Get labor costs from payslips
        var payrollPeriods = await context.Set<Core.Entities.PayrollPeriod>()
            .Where(p => p.StartDate >= parameters.StartDate && p.EndDate <= parameters.EndDate)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var payslips = await context.Set<Core.Entities.Payslip>()
            .Where(p => payrollPeriods.Contains(p.PayrollPeriodId))
            .ToListAsync(cancellationToken);

        report.WagesAndSalaries = payslips.Sum(p => p.BasicSalary);
        report.PayrollTaxes = payslips.Sum(p => p.TotalDeductions * 0.5m); // Estimate statutory as 50% of deductions
        report.EmployeeBenefits = payslips.Sum(p => p.TotalEarnings - p.BasicSalary);

        // Calculate breakdown if requested
        if (parameters.IncludeBreakdown)
        {
            report.COGSBreakdown = new List<CostBreakdownItem>
            {
                new() { Category = "Food", Amount = report.FoodCost, Percentage = report.NetRevenue > 0 ? Math.Round(report.FoodCost / report.NetRevenue * 100, 2) : 0 },
                new() { Category = "Beverage", Amount = report.BeverageCost, Percentage = report.NetRevenue > 0 ? Math.Round(report.BeverageCost / report.NetRevenue * 100, 2) : 0 },
                new() { Category = "Other", Amount = report.OtherCOGS, Percentage = report.NetRevenue > 0 ? Math.Round(report.OtherCOGS / report.NetRevenue * 100, 2) : 0 }
            };

            report.LaborBreakdown = new List<CostBreakdownItem>
            {
                new() { Category = "Wages & Salaries", Amount = report.WagesAndSalaries, Percentage = report.NetRevenue > 0 ? Math.Round(report.WagesAndSalaries / report.NetRevenue * 100, 2) : 0 },
                new() { Category = "Payroll Taxes", Amount = report.PayrollTaxes, Percentage = report.NetRevenue > 0 ? Math.Round(report.PayrollTaxes / report.NetRevenue * 100, 2) : 0 },
                new() { Category = "Benefits", Amount = report.EmployeeBenefits, Percentage = report.NetRevenue > 0 ? Math.Round(report.EmployeeBenefits / report.NetRevenue * 100, 2) : 0 }
            };
        }

        // Get previous period for comparison
        if (parameters.IncludePreviousPeriod)
        {
            var periodLength = parameters.EndDate - parameters.StartDate;
            var previousParams = new PrimeCostReportParameters
            {
                StartDate = parameters.StartDate - periodLength - TimeSpan.FromDays(1),
                EndDate = parameters.StartDate - TimeSpan.FromDays(1),
                StoreId = parameters.StoreId,
                IncludePreviousPeriod = false,
                IncludeBreakdown = false
            };

            var previousReport = await GeneratePrimeCostReportAsync(previousParams, cancellationToken);
            report.PreviousPeriodPrimeCostPercentage = previousReport.PrimeCostPercentage;
        }

        _logger.LogInformation("Generated Prime Cost Report for {StartDate} to {EndDate}: {PrimeCostPercentage}%",
            parameters.StartDate, parameters.EndDate, report.PrimeCostPercentage);

        return report;
    }

    public async Task<FoodCostReport> GenerateFoodCostReportAsync(
        FoodCostReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var report = new FoodCostReport
        {
            StartDate = parameters.StartDate,
            EndDate = parameters.EndDate,
            GeneratedAt = DateTime.UtcNow
        };

        // Calculate food sales
        var orders = await context.Orders
            .Where(o => o.CreatedAt >= parameters.StartDate && o.CreatedAt <= parameters.EndDate)
            .Where(o => o.Status == OrderStatus.Served)
            .ToListAsync(cancellationToken);

        report.FoodSales = orders.Sum(o => o.TotalAmount);

        // Get order items for theoretical cost calculation
        var orderIds = orders.Select(o => o.Id).ToList();
        var orderItems = await context.OrderItems
            .Where(oi => orderIds.Contains(oi.OrderId))
            .Include(oi => oi.Product)
            .ThenInclude(p => p!.Category)
            .ToListAsync(cancellationToken);

        // Calculate theoretical food cost from products
        report.TheoreticalFoodCost = orderItems.Sum(oi => (oi.Product?.CostPrice ?? 0) * oi.Quantity);

        // Get inventory data
        var inventory = await context.Inventories.Include(i => i.Product).ToListAsync(cancellationToken);
        report.EndingInventory = inventory.Sum(i => i.CurrentStock * (i.Product?.CostPrice ?? 0));

        // Calculate purchases from goods received notes
        var grns = await context.Set<Core.Entities.GoodsReceivedNote>()
            .Where(gr => gr.ReceivedDate >= parameters.StartDate && gr.ReceivedDate <= parameters.EndDate)
            .ToListAsync(cancellationToken);

        report.Purchases = grns.Sum(g => g.TotalAmount);

        // Beginning inventory estimate
        report.BeginningInventory = Math.Max(0, report.EndingInventory + report.TheoreticalFoodCost - report.Purchases);

        // Category breakdown
        if (parameters.IncludeVarianceItems)
        {
            var categories = await context.Categories.ToListAsync(cancellationToken);
            report.CategoryBreakdown = categories.Select(c => new FoodCostCategoryItem
            {
                CategoryId = c.Id,
                CategoryName = c.Name,
                Sales = orderItems.Where(oi => oi.Product?.CategoryId == c.Id).Sum(oi => oi.TotalAmount),
                Cost = orderItems.Where(oi => oi.Product?.CategoryId == c.Id).Sum(oi => (oi.Product?.CostPrice ?? 0) * oi.Quantity),
                TheoreticalCost = orderItems.Where(oi => oi.Product?.CategoryId == c.Id).Sum(oi => (oi.Product?.CostPrice ?? 0) * oi.Quantity)
            }).Where(c => c.Sales > 0).ToList();
        }

        return report;
    }

    public async Task<LaborCostReport> GenerateLaborCostReportAsync(
        LaborCostReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var report = new LaborCostReport
        {
            StartDate = parameters.StartDate,
            EndDate = parameters.EndDate,
            GeneratedAt = DateTime.UtcNow
        };

        // Get revenue
        var orders = await context.Orders
            .Where(o => o.CreatedAt >= parameters.StartDate && o.CreatedAt <= parameters.EndDate)
            .Where(o => o.Status == OrderStatus.Served)
            .ToListAsync(cancellationToken);

        report.GrossRevenue = orders.Sum(o => o.TotalAmount);

        // Get labor costs from payslips
        var payrollPeriods = await context.Set<Core.Entities.PayrollPeriod>()
            .Where(p => p.StartDate >= parameters.StartDate && p.EndDate <= parameters.EndDate)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var payslips = await context.Set<Core.Entities.Payslip>()
            .Where(p => payrollPeriods.Contains(p.PayrollPeriodId))
            .Include(p => p.Employee)
            .ToListAsync(cancellationToken);

        var hourlyPayslips = payslips.Where(p => p.Employee?.EmploymentType == EmploymentType.PartTime).ToList();
        var salariedPayslips = payslips.Where(p => p.Employee?.EmploymentType == EmploymentType.FullTime).ToList();

        report.HourlyWages = hourlyPayslips.Sum(p => p.BasicSalary);
        report.SalariedWages = salariedPayslips.Sum(p => p.BasicSalary);
        report.Overtime = payslips.Sum(p => p.TotalEarnings - p.BasicSalary) * 0.3m; // Estimate overtime
        report.PayrollTaxes = payslips.Sum(p => p.TotalDeductions * 0.5m);
        report.Benefits = payslips.Sum(p => p.TotalEarnings - p.BasicSalary) * 0.7m;

        // Get hours worked from attendance
        var attendance = await context.Set<Core.Entities.Attendance>()
            .Where(a => a.AttendanceDate >= parameters.StartDate && a.AttendanceDate <= parameters.EndDate)
            .ToListAsync(cancellationToken);

        report.TotalHoursWorked = attendance.Sum(a => a.HoursWorked);

        // Employee productivity breakdown
        if (parameters.IncludeEmployeeBreakdown)
        {
            var employeeGroups = attendance.GroupBy(a => a.EmployeeId);
            var employees = await context.Set<Core.Entities.Employee>().ToListAsync(cancellationToken);

            report.TopPerformers = employeeGroups
                .Select(g =>
                {
                    var employee = employees.FirstOrDefault(e => e.Id == g.Key);
                    var hoursWorked = g.Sum(a => a.HoursWorked);
                    var employeeOrders = orders.Where(o => o.UserId == employee?.UserId);
                    var totalSales = employeeOrders.Sum(o => o.TotalAmount);

                    return new EmployeeProductivityItem
                    {
                        EmployeeId = g.Key,
                        EmployeeName = employee?.FullName ?? "Unknown",
                        Role = employee?.Position ?? "Staff",
                        HoursWorked = hoursWorked,
                        TotalSales = totalSales,
                        TransactionCount = employeeOrders.Count()
                    };
                })
                .OrderByDescending(e => e.SalesPerHour)
                .Take(parameters.TopPerformersCount)
                .ToList();
        }

        return report;
    }

    public async Task<ProfitLossStatement> GenerateProfitLossStatementAsync(
        ProfitLossParameters parameters,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var statement = new ProfitLossStatement
        {
            StartDate = parameters.StartDate,
            EndDate = parameters.EndDate,
            GeneratedAt = DateTime.UtcNow
        };

        // Get sales data
        var orders = await context.Orders
            .Where(o => o.CreatedAt >= parameters.StartDate && o.CreatedAt <= parameters.EndDate)
            .Where(o => o.Status == OrderStatus.Served)
            .ToListAsync(cancellationToken);

        statement.GrossSales = orders.Sum(o => o.Subtotal);
        statement.Discounts = orders.Sum(o => o.DiscountAmount);
        statement.Returns = 0;

        // Get COGS
        var orderIds = orders.Select(o => o.Id).ToList();
        var orderItems = await context.OrderItems
            .Where(oi => orderIds.Contains(oi.OrderId))
            .Include(oi => oi.Product)
            .ToListAsync(cancellationToken);

        statement.FoodCost = orderItems.Sum(oi => (oi.Product?.CostPrice ?? 0) * oi.Quantity);

        // Get labor costs from payslips
        var payrollPeriods = await context.Set<Core.Entities.PayrollPeriod>()
            .Where(p => p.StartDate >= parameters.StartDate && p.EndDate <= parameters.EndDate)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var payslips = await context.Set<Core.Entities.Payslip>()
            .Where(p => payrollPeriods.Contains(p.PayrollPeriodId))
            .ToListAsync(cancellationToken);

        statement.LaborCost = payslips.Sum(p => p.NetPay);

        // Estimate other operating expenses
        statement.Rent = statement.TotalRevenue * 0.06m;
        statement.Utilities = statement.TotalRevenue * 0.03m;
        statement.Marketing = statement.TotalRevenue * 0.02m;

        // Build line items
        statement.LineItems = BuildPLLineItems(statement);

        // Prior period comparison
        if (parameters.IncludePriorPeriod)
        {
            var periodLength = parameters.EndDate - parameters.StartDate;
            var priorParams = new ProfitLossParameters
            {
                StartDate = parameters.StartDate - periodLength - TimeSpan.FromDays(1),
                EndDate = parameters.StartDate - TimeSpan.FromDays(1),
                StoreId = parameters.StoreId,
                IncludePriorPeriod = false
            };

            statement.PriorPeriod = await GenerateProfitLossStatementAsync(priorParams, cancellationToken);
        }

        return statement;
    }

    public async Task<DayPartAnalysisReport> GenerateDayPartAnalysisAsync(
        DayPartAnalysisParameters parameters,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var report = new DayPartAnalysisReport
        {
            StartDate = parameters.StartDate,
            EndDate = parameters.EndDate,
            GeneratedAt = DateTime.UtcNow
        };

        var orders = await context.Orders
            .Where(o => o.CreatedAt >= parameters.StartDate && o.CreatedAt <= parameters.EndDate)
            .Where(o => o.Status == OrderStatus.Served)
            .ToListAsync(cancellationToken);

        var totalSales = orders.Sum(o => o.TotalAmount);

        foreach (var dayPart in parameters.DayParts)
        {
            var dayPartOrders = orders.Where(o =>
            {
                var hour = o.CreatedAt.Hour;
                if (dayPart.StartHour < dayPart.EndHour)
                    return hour >= dayPart.StartHour && hour < dayPart.EndHour;
                else
                    return hour >= dayPart.StartHour || hour < dayPart.EndHour;
            }).ToList();

            var dayPartSales = dayPartOrders.Sum(o => o.TotalAmount);

            var summary = new DayPartSummary
            {
                DayPartName = dayPart.Name,
                StartTime = TimeSpan.FromHours(dayPart.StartHour),
                EndTime = TimeSpan.FromHours(dayPart.EndHour),
                Sales = dayPartSales,
                SalesPercentage = totalSales > 0 ? Math.Round(dayPartSales / totalSales * 100, 2) : 0,
                TransactionCount = dayPartOrders.Count,
                GuestCount = dayPartOrders.Count
            };

            report.DayParts.Add(summary);
        }

        return report;
    }

    public async Task<SalesPerLaborHourReport> GenerateSPLHReportAsync(
        SPLHReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var report = new SalesPerLaborHourReport
        {
            StartDate = parameters.StartDate,
            EndDate = parameters.EndDate,
            GeneratedAt = DateTime.UtcNow,
            TargetSPLH = parameters.TargetSPLH
        };

        var orders = await context.Orders
            .Where(o => o.CreatedAt >= parameters.StartDate && o.CreatedAt <= parameters.EndDate)
            .Where(o => o.Status == OrderStatus.Served)
            .ToListAsync(cancellationToken);

        report.TotalSales = orders.Sum(o => o.TotalAmount);

        // Get attendance for labor hours
        var attendance = await context.Set<Core.Entities.Attendance>()
            .Where(a => a.AttendanceDate >= parameters.StartDate && a.AttendanceDate <= parameters.EndDate)
            .ToListAsync(cancellationToken);

        report.TotalLaborHours = attendance.Sum(a => a.HoursWorked);

        // Daily breakdown
        var dailyGroups = orders.GroupBy(o => o.CreatedAt.DayOfWeek);
        foreach (var group in dailyGroups)
        {
            var dailySales = group.Sum(o => o.TotalAmount);
            var dailyHours = report.TotalLaborHours / 7;

            report.DailyBreakdown.Add(new SPLHDailySummary
            {
                DayOfWeek = group.Key,
                Sales = dailySales,
                LaborHours = dailyHours,
                LaborCost = dailyHours * 15m
            });
        }

        // Hourly breakdown
        var hourlyGroups = orders.GroupBy(o => o.CreatedAt.Hour);
        foreach (var group in hourlyGroups)
        {
            var hourlySales = group.Sum(o => o.TotalAmount);
            var hourlyLabor = report.TotalLaborHours / 24;

            var hourlyData = new SPLHHourlySummary
            {
                Hour = group.Key,
                Sales = hourlySales,
                LaborHours = hourlyLabor
            };

            hourlyData.IsUnderstaffed = hourlyData.SPLH > parameters.TargetSPLH * 1.5m;
            hourlyData.IsOverstaffed = hourlyData.SPLH < parameters.TargetSPLH * 0.5m;

            report.HourlyBreakdown.Add(hourlyData);
        }

        if (parameters.IncludeRecommendations)
        {
            report.Recommendations = GenerateSPLHRecommendations(report);
        }

        return report;
    }

    public async Task<List<PrimeCostTrendPoint>> GetPrimeCostTrendAsync(
        int? storeId,
        DateTime startDate,
        DateTime endDate,
        string interval = "day",
        CancellationToken cancellationToken = default)
    {
        var points = new List<PrimeCostTrendPoint>();
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            var nextDate = interval switch
            {
                "week" => currentDate.AddDays(7),
                "month" => currentDate.AddMonths(1),
                _ => currentDate.AddDays(1)
            };

            var report = await GeneratePrimeCostReportAsync(new PrimeCostReportParameters
            {
                StartDate = currentDate,
                EndDate = nextDate.AddDays(-1),
                StoreId = storeId,
                IncludePreviousPeriod = false,
                IncludeBreakdown = false
            }, cancellationToken);

            points.Add(new PrimeCostTrendPoint
            {
                Date = currentDate,
                Revenue = report.NetRevenue,
                COGS = report.TotalCOGS,
                Labor = report.TotalLaborCost
            });

            currentDate = nextDate;
        }

        return points;
    }

    public async Task<List<FoodCostTrendPoint>> GetFoodCostTrendAsync(
        int? storeId,
        DateTime startDate,
        DateTime endDate,
        string interval = "day",
        CancellationToken cancellationToken = default)
    {
        var points = new List<FoodCostTrendPoint>();
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            var nextDate = interval switch
            {
                "week" => currentDate.AddDays(7),
                "month" => currentDate.AddMonths(1),
                _ => currentDate.AddDays(1)
            };

            var report = await GenerateFoodCostReportAsync(new FoodCostReportParameters
            {
                StartDate = currentDate,
                EndDate = nextDate.AddDays(-1),
                StoreId = storeId
            }, cancellationToken);

            points.Add(new FoodCostTrendPoint
            {
                Date = currentDate,
                FoodSales = report.FoodSales,
                FoodCost = report.ActualFoodCost,
                TheoreticalCost = report.TheoreticalFoodCost
            });

            currentDate = nextDate;
        }

        return points;
    }

    public async Task<List<LaborCostTrendPoint>> GetLaborCostTrendAsync(
        int? storeId,
        DateTime startDate,
        DateTime endDate,
        string interval = "day",
        CancellationToken cancellationToken = default)
    {
        var points = new List<LaborCostTrendPoint>();
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            var nextDate = interval switch
            {
                "week" => currentDate.AddDays(7),
                "month" => currentDate.AddMonths(1),
                _ => currentDate.AddDays(1)
            };

            var report = await GenerateLaborCostReportAsync(new LaborCostReportParameters
            {
                StartDate = currentDate,
                EndDate = nextDate.AddDays(-1),
                StoreId = storeId,
                IncludeEmployeeBreakdown = false
            }, cancellationToken);

            points.Add(new LaborCostTrendPoint
            {
                Date = currentDate,
                Revenue = report.GrossRevenue,
                LaborCost = report.TotalLaborCost,
                LaborHours = report.TotalHoursWorked
            });

            currentDate = nextDate;
        }

        return points;
    }

    private static List<PLLineItem> BuildPLLineItems(ProfitLossStatement statement)
    {
        return new List<PLLineItem>
        {
            new() { Section = "Revenue", Category = "Sales", AccountName = "Gross Sales", Amount = statement.GrossSales, PercentageOfRevenue = 100, DisplayOrder = 1 },
            new() { Section = "Revenue", Category = "Sales", AccountName = "Less: Discounts", Amount = -statement.Discounts, DisplayOrder = 2 },
            new() { Section = "Revenue", Category = "Sales", AccountName = "Net Sales", Amount = statement.NetSales, DisplayOrder = 3, IsSubtotal = true },
            new() { Section = "COGS", Category = "Cost of Goods", AccountName = "Food Cost", Amount = statement.FoodCost, DisplayOrder = 10 },
            new() { Section = "COGS", Category = "Cost of Goods", AccountName = "Total COGS", Amount = statement.TotalCOGS, DisplayOrder = 15, IsSubtotal = true },
            new() { Section = "Profit", Category = "Gross Profit", AccountName = "Gross Profit", Amount = statement.GrossProfit, PercentageOfRevenue = statement.GrossProfitMargin, DisplayOrder = 20, IsTotal = true },
            new() { Section = "Operating", Category = "Expenses", AccountName = "Labor Cost", Amount = statement.LaborCost, DisplayOrder = 30 },
            new() { Section = "Operating", Category = "Expenses", AccountName = "Operating Income", Amount = statement.OperatingIncome, PercentageOfRevenue = statement.OperatingMargin, DisplayOrder = 50, IsTotal = true },
            new() { Section = "Net", Category = "Income", AccountName = "Net Income", Amount = statement.NetIncome, PercentageOfRevenue = statement.NetProfitMargin, DisplayOrder = 100, IsTotal = true }
        };
    }

    private static List<SPLHRecommendation> GenerateSPLHRecommendations(SalesPerLaborHourReport report)
    {
        var recommendations = new List<SPLHRecommendation>();

        if (report.OverallSPLH < report.TargetSPLH * 0.8m)
        {
            recommendations.Add(new SPLHRecommendation
            {
                Category = "Staffing",
                Title = "Reduce Labor Hours",
                Description = $"Current SPLH ({report.OverallSPLH:F2}) is below target ({report.TargetSPLH:F2}). Consider reducing scheduled hours.",
                Impact = "Could improve SPLH by 15-20%",
                Priority = 1
            });
        }

        return recommendations;
    }
}
