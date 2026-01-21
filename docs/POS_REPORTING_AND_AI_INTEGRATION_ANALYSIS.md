# POS Reporting & AI Integration Analysis

> **Document Version:** 1.0
> **Date:** January 21, 2026
> **Purpose:** Gap analysis of current reporting capabilities and AI integration roadmap

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Current Implementation Status](#current-implementation-status)
3. [Gap Analysis: Missing Reports](#gap-analysis-missing-reports)
4. [AI Integration Opportunities](#ai-integration-opportunities)
5. [Implementation Roadmap](#implementation-roadmap)
6. [Technical Architecture](#technical-architecture)
7. [Quick Implementation Steps](#quick-implementation-steps)

---

## Executive Summary

This document analyzes the current reporting capabilities of HospitalityPOS against industry best practices and outlines opportunities for AI integration to provide competitive differentiation. The analysis is based on research from leading POS providers including Toast, Square, Lightspeed, 7shifts, and Lineup.ai.

**Key Findings:**
- Current system has solid foundational reports (Sales, Inventory, Audit, Z/X Reports)
- Missing critical financial reports (P&L, Prime Cost, Food/Labor Cost %)
- No customer analytics (RFM, CLV, churn prediction)
- Significant opportunity for AI-powered insights and recommendations

---

## Current Implementation Status

### Implemented Reports

| Category | Reports | Status |
|----------|---------|--------|
| **Sales Reports** | Daily Sales Summary, Product Sales, Category Sales, Cashier Sales, Payment Method Sales, Hourly Sales | Implemented |
| **Shift Reports** | X-Report (mid-shift), Z-Report (end-of-day), Z-Report History | Implemented |
| **Inventory Reports** | Current Stock, Low Stock, Stock Movement, Stock Valuation, Dead Stock | Implemented |
| **Exception Reports** | Voids Report, Discounts Report | Implemented |
| **Audit Reports** | User Activity, Transaction Log, Void/Refund Log, Price Change Log, Permission Override Log | Implemented |
| **Financial Reports** | Margin Reports, Offer Reports | Implemented |
| **Analytics** | Comparative Analytics, Inventory Analytics | Implemented |
| **Enterprise** | Chain Reporting, Mobile Reporting, Email Reports, Waste Reports | Implemented |

### Existing Services

```
IReportService.cs
├── GenerateDailySummaryAsync()
├── GenerateProductSalesAsync()
├── GenerateCategorySalesAsync()
├── GenerateCashierSalesAsync()
├── GeneratePaymentMethodSalesAsync()
├── GenerateHourlySalesAsync()
├── GenerateVoidReportAsync()
├── GenerateDiscountReportAsync()
├── GenerateCurrentStockReportAsync()
├── GenerateLowStockReportAsync()
├── GenerateStockMovementReportAsync()
├── GenerateStockValuationReportAsync()
├── GenerateDeadStockReportAsync()
├── GenerateUserActivityReportAsync()
├── GenerateTransactionLogReportAsync()
├── GenerateVoidRefundLogReportAsync()
├── GeneratePriceChangeLogReportAsync()
├── GeneratePermissionOverrideLogReportAsync()
└── GenerateAuditTrailReportAsync()
```

---

## Gap Analysis: Missing Reports

### 1. Sales & Revenue Reports

| Report | Description | Business Value | Priority |
|--------|-------------|----------------|----------|
| **Day-Part Analysis** | Sales breakdown by breakfast/lunch/dinner/late-night | Optimize staffing and promotions by time slot | HIGH |
| **Week-over-Week Comparison** | Compare current week to previous week by day | Quick trend identification | HIGH |
| **Sales Snapshot Dashboard** | Real-time KPIs: guests, checks, averages, covers | At-a-glance performance | HIGH |
| **Seasonal Trend Report** | Month-over-month and year-over-year trends | Long-term planning | MEDIUM |
| **Table Turnover Report** | Average time per table/order | Space utilization efficiency | MEDIUM |
| **Revenue per Square Foot** | Sales divided by floor space | Location performance | LOW |

### 2. Product Performance Reports

| Report | Description | Business Value | Priority |
|--------|-------------|----------------|----------|
| **Menu Engineering Matrix** | Classify items as Stars/Plowhorses/Puzzles/Dogs | Menu optimization decisions | HIGH |
| **Product Mix (PMIX) with Subtotals** | Items grouped by category with contribution % | Category performance | HIGH |
| **Modifier Analysis** | Most used modifiers and add-ons | Upselling opportunities | MEDIUM |
| **Combo/Bundle Performance** | Bundle vs individual item sales | Promotion effectiveness | MEDIUM |
| **Price Elasticity Report** | Sales volume change vs price change | Pricing strategy | MEDIUM |

### 3. Financial & Cost Reports

| Report | Description | Business Value | Priority |
|--------|-------------|----------------|----------|
| **Profit & Loss (P&L) Statement** | Full income statement | Overall profitability view | HIGH |
| **Prime Cost Report** | COGS + Labor as % of revenue | Key profitability metric (target: ~60%) | HIGH |
| **Food Cost Percentage** | (Beginning Inv + Purchases - Ending Inv) / Food Sales | Cost control (target: 28-35%) | HIGH |
| **Labor Cost Percentage** | Total labor / Revenue | Staffing efficiency (target: 25-35%) | HIGH |
| **Theoretical vs Actual Variance** | Expected usage vs actual | Waste/theft detection | HIGH |
| **Gross Margin by Item** | Profit margin per menu item | Item-level profitability | MEDIUM |

**Formulas:**
```
Food Cost % = (Beginning Inventory + Purchases - Ending Inventory) / Food Sales × 100
Labor Cost % = Total Labor Cost / Gross Revenue × 100
Prime Cost = Cost of Goods Sold + Total Labor Cost
Prime Cost % = Prime Cost / Total Revenue × 100 (Target: ≤60%)
Gross Margin = (Selling Price - Cost Price) / Selling Price × 100
```

### 4. Customer Analytics Reports

| Report | Description | Business Value | Priority |
|--------|-------------|----------------|----------|
| **RFM Segmentation** | Score customers by Recency/Frequency/Monetary | Targeted marketing | HIGH |
| **Customer Lifetime Value (CLV)** | Predicted total revenue per customer | Customer prioritization | HIGH |
| **Purchase Frequency Analysis** | Average days between visits | Retention tracking | MEDIUM |
| **Basket Analysis** | Items frequently purchased together | Cross-selling opportunities | MEDIUM |
| **New vs Returning Customers** | Customer acquisition rate | Growth tracking | MEDIUM |
| **Loyalty Program Performance** | Points earned, redeemed, engagement | Program ROI | MEDIUM |

**RFM Scoring Model:**
```
Recency Score (1-5):   Days since last purchase (lower = better)
Frequency Score (1-5): Number of purchases in period (higher = better)
Monetary Score (1-5):  Total spend in period (higher = better)

Segments:
- Champions (R=5, F=5, M=5): Best customers, reward them
- Loyal (R=4-5, F=4-5): High potential, nurture relationship
- At Risk (R=1-2, F=3-5): Were good, need re-engagement
- Lost (R=1, F=1-2): May be gone, win-back campaign
```

### 5. Employee Performance Reports

| Report | Description | Business Value | Priority |
|--------|-------------|----------------|----------|
| **Sales Per Labor Hour (SPLH)** | Revenue ÷ labor hours worked | Labor efficiency | HIGH |
| **Employee Productivity Ranking** | Compare staff by sales, speed, accuracy | Performance management | MEDIUM |
| **Upsell Success Rate** | Modifier/add-on attach rate by employee | Training identification | MEDIUM |
| **Tips Report** | Tips by employee, shift, day | Compensation tracking | MEDIUM |
| **Training Impact Report** | Performance before/after training | Training ROI | LOW |

### 6. Operational Reports

| Report | Description | Business Value | Priority |
|--------|-------------|----------------|----------|
| **Speed of Service** | Order to payment duration | Customer experience | MEDIUM |
| **Queue Analysis** | Wait times, transaction duration | Bottleneck identification | MEDIUM |
| **Refund Analysis** | Reasons, patterns, by employee | Loss prevention | HIGH |
| **No-Sale Report** | Cash drawer opens without transaction | Theft prevention | HIGH |
| **Cash Variance Trend** | Historical over/short patterns | Cash handling issues | MEDIUM |

---

## AI Integration Opportunities

### Tier 1: Foundational AI Features (Proven ROI)

#### 1.1 Demand Forecasting Engine

**Purpose:** Predict sales volume by hour, day, week, and product

**Data Inputs:**
- Historical sales data (minimum 6 months)
- Day of week patterns
- Seasonality factors
- Weather data (API integration)
- Local events calendar
- Holiday calendar

**Outputs:**
- Daily sales forecast with confidence interval
- Hourly traffic predictions
- Product-level demand forecasts
- Recommended prep quantities

**Expected ROI:** 15-20% reduction in food waste, better staffing

**Implementation:**
```csharp
public interface IDemandForecastingService
{
    Task<SalesForecast> ForecastDailySalesAsync(DateTime date);
    Task<List<HourlyForecast>> ForecastHourlySalesAsync(DateTime date);
    Task<List<ProductForecast>> ForecastProductDemandAsync(DateTime date);
    Task<PrepRecommendation> GetPrepRecommendationsAsync(DateTime date);
}
```

#### 1.2 Smart Inventory Recommendations

**Purpose:** Automated reorder suggestions based on demand forecasting

**Features:**
- Automatic reorder point calculation
- Lead time consideration
- Safety stock optimization
- Seasonal adjustment
- Supplier delivery schedule integration

**Outputs:**
- "Reorder 50 units of Chicken Breast by Friday"
- "Stock level will reach critical on Sunday without reorder"
- "Suggested order quantity: 75 units (includes 15% safety buffer)"

**Expected ROI:** 5-10% sales lift from reduced stockouts, lower carrying costs

#### 1.3 Labor Scheduling Optimization

**Purpose:** Optimal staff scheduling based on forecasted demand

**Data Inputs:**
- Sales forecast by hour
- Historical labor data
- Employee availability
- Labor cost rates
- Minimum staffing requirements

**Outputs:**
- Recommended schedule by position
- Over/understaffing alerts
- Labor cost projections
- Break time optimization

**Expected ROI:** 10% reduction in labor costs

#### 1.4 Anomaly Detection & Fraud Prevention

**Purpose:** Real-time flagging of unusual patterns

**Detection Patterns:**
- Excessive voids by employee
- Unusual discount patterns
- Cash variance anomalies
- No-sale frequency spikes
- After-hours transactions
- High-value refunds

**Alert Examples:**
- "Alert: Employee John has 3x average void rate this week"
- "Warning: Cash drawer #2 showing consistent shortages"
- "Unusual: 5 transactions over $500 in last hour"

**Expected ROI:** 2-5% reduction in shrinkage/theft

---

### Tier 2: Advanced AI Features (Competitive Differentiation)

#### 2.1 Menu Engineering AI

**Analysis Framework:**
```
                    HIGH POPULARITY
                          │
         PLOWHORSES       │       STARS
         (High Vol,       │    (High Vol,
          Low Margin)     │     High Margin)
                          │
    ──────────────────────┼──────────────────── HIGH MARGIN
                          │
         DOGS             │       PUZZLES
         (Low Vol,        │    (Low Vol,
          Low Margin)     │     High Margin)
                          │
                    LOW POPULARITY
```

**Recommendations Engine:**
- Stars: Feature prominently, maintain quality
- Plowhorses: Increase price or reduce portion cost
- Puzzles: Promote more, reposition on menu
- Dogs: Remove from menu or reformulate

**Output Example:**
```
Menu Engineering Recommendation:
- Move "Grilled Salmon" from $24 to $26 (Puzzle → Star potential)
  Estimated impact: +$340/month profit, <5% volume decrease
- Remove "Veggie Wrap" (Dog): Only 3 sales/week, -12% margin
- Promote "Craft Burger" (Plow horse): Increase visibility
```

#### 2.2 AI-Powered Natural Language Insights

**Integration:** Claude/GPT API for conversational analytics

**Query Examples:**
```
User: "Why were sales down last Tuesday?"
AI: "Sales were 18% below average ($3,420 vs $4,175). Analysis:
     - Weather: Heavy rain reduced foot traffic by ~25%
     - Staffing: 2 servers called in sick (slower service)
     - Competition: Nearby restaurant ran 20% off promotion
     Recommendation: Consider rain-day promotions for delivery/takeout"

User: "What should I order more of?"
AI: "Based on trends and forecasts, increase orders for:
     1. IPA Beer: +23% sales growth, 2 stockouts last month
     2. Veggie Burger: 3 stockouts, strong weekend demand
     3. Sparkling Water: Seasonal uptrend starting
     Estimated additional revenue if in-stock: $890/month"

User: "Show me my worst-performing products"
AI: "Bottom 5 products by profit contribution:
     1. House Salad: 2.1% margin, declining trend
     2. Veggie Wrap: -12% margin (costs exceed price)
     3. Kids Pasta: Low volume, high waste
     4. Seasonal Soup: Inconsistent quality scores
     5. Decaf Coffee: 8 cups/week average
     Recommendation: Review pricing or remove items 2, 3, 5"
```

#### 2.3 Customer Churn Prediction

**Model Inputs:**
- Visit frequency history
- Recency of last visit
- Spending patterns
- Feedback/complaints
- Loyalty program engagement

**Risk Scoring:**
```
Churn Risk Score: 0-100
- Low Risk (0-30): Active, engaged customer
- Medium Risk (31-60): Declining engagement
- High Risk (61-80): Likely to churn without intervention
- Critical (81-100): Probably already churned
```

**Output:**
```
Churn Alert Report:
- 15 high-value customers haven't visited in 45+ days
- Combined historical monthly spend: $2,340
- Recommended action: Win-back campaign with 20% discount
- Estimated recovery rate: 35% (5-6 customers)
- Potential recovered revenue: $820/month
```

#### 2.4 Dynamic Pricing Suggestions

**Analysis Factors:**
- Demand patterns by time
- Competitor pricing
- Inventory levels
- Margin targets
- Price elasticity

**Output Example:**
```
Dynamic Pricing Recommendation:
Happy Hour Optimization (4-6pm):
- Current appetizer conversion: 12%
- Suggested: Reduce appetizer prices 15%
- Projected conversion: 22%
- Net revenue impact: +$145/day

Weekend Premium Suggestion:
- Friday/Saturday demand: 40% above average
- Suggested: 5% price increase on top sellers
- Projected impact: +$280/weekend, minimal volume loss
```

---

### Tier 3: Cutting-Edge AI (Future Differentiation)

#### 3.1 Auto-Generated Report Summaries

**Feature:** AI writes executive summaries for all reports

**Example Output:**
```
Weekly Business Summary (Auto-Generated)
Week of January 15-21, 2026

HIGHLIGHTS:
- Total Revenue: $28,450 (+8.2% vs last week)
- Best Day: Saturday ($5,890)
- Top Performer: New Craft Burger (+145 units)

KEY INSIGHTS:
- Sales increase driven by new burger launch (contributed $2,100)
- Labor efficiency improved 12% after Tuesday schedule adjustment
- Food cost % decreased from 32% to 29% (better portion control)

ALERTS:
- Chicken Breast inventory critical (reorder by Thursday)
- Employee turnover: 2 servers resigned (replacement needed)
- Cash variance on Register 2 needs investigation

RECOMMENDATIONS:
1. Extend burger promotion through next week
2. Schedule extra prep cook for weekend (forecast: +15% traffic)
3. Review Register 2 procedures with team
```

#### 3.2 Predictive Maintenance Alerts

**Monitored Patterns:**
- Receipt printer error frequency
- Card reader decline rates
- POS terminal response times
- Cash drawer open failures

**Alert Example:**
```
Equipment Alert: Receipt Printer #1
- Error rate increased 340% over 7 days
- Pattern suggests thermal head degradation
- Estimated time to failure: 3-5 days
- Recommendation: Schedule service or prepare replacement
```

#### 3.3 What-If Scenario Analysis

**User Query:** "What happens if I raise prices 5%?"

**AI Analysis:**
```
Price Increase Simulation: +5% Across Menu

PROJECTED IMPACT (based on historical elasticity):
- Revenue change: +2.8% ($795/week)
- Volume change: -2.1% (fewer transactions)
- Margin change: +4.2% (higher profit per sale)

BY CATEGORY:
- Beverages: Low elasticity, minimal volume impact
- Entrees: Medium elasticity, 3% volume decrease
- Appetizers: High elasticity, 5% volume decrease

RECOMMENDATION:
- Apply 5% increase to beverages and entrees
- Keep appetizer prices stable (price-sensitive)
- Expected net impact: +$680/week revenue, +$890/week profit
```

---

## Implementation Roadmap

### Phase 1: Critical Reports (Weeks 1-4)

| Week | Task | Deliverable |
|------|------|-------------|
| 1 | Prime Cost Report | `IPrimeCostReportService`, UI View |
| 1 | Food Cost % Report | Added to Financial Reports |
| 2 | Labor Cost % Report | Added to Financial Reports |
| 2 | P&L Statement | Basic income statement |
| 3 | Menu Engineering Matrix | Product classification report |
| 3 | Day-Part Analysis | Sales by time period |
| 4 | RFM Customer Segmentation | Customer scoring system |
| 4 | SPLH Report | Labor efficiency metrics |

### Phase 2: AI Foundation (Weeks 5-8)

| Week | Task | Deliverable |
|------|------|-------------|
| 5 | Data Pipeline Setup | Historical data aggregation |
| 5 | ML Model Training | Sales forecasting model |
| 6 | Demand Forecasting Service | `IDemandForecastingService` |
| 6 | Smart Inventory Alerts | Automated reorder suggestions |
| 7 | Anomaly Detection | Fraud/loss pattern detection |
| 7 | Alert System | Real-time notifications |
| 8 | AI Insights Dashboard | Centralized AI recommendations |
| 8 | Integration Testing | End-to-end validation |

### Phase 3: Advanced AI (Weeks 9-12)

| Week | Task | Deliverable |
|------|------|-------------|
| 9 | Menu Engineering AI | Automated recommendations |
| 9 | Price Optimization | Dynamic pricing engine |
| 10 | Natural Language Interface | Claude/GPT integration |
| 10 | Conversational Queries | "Ask your data" feature |
| 11 | Auto-Generated Summaries | AI report narratives |
| 11 | Customer Churn Model | Prediction and alerts |
| 12 | Labor Scheduling AI | Optimization recommendations |
| 12 | Polish & Documentation | Production ready |

---

## Technical Architecture

### AI Module Structure

```
src/HospitalityPOS.AI/
├── Interfaces/
│   ├── IDemandForecastingService.cs
│   ├── IAnomalyDetectionService.cs
│   ├── IMenuEngineeringService.cs
│   ├── ICustomerAnalyticsService.cs
│   ├── ILaborOptimizationService.cs
│   ├── INaturalLanguageService.cs
│   └── IInsightGeneratorService.cs
├── Models/
│   ├── Forecasting/
│   │   ├── SalesForecast.cs
│   │   ├── DemandPrediction.cs
│   │   └── ForecastConfidence.cs
│   ├── Analytics/
│   │   ├── CustomerRFMScore.cs
│   │   ├── MenuEngineeringItem.cs
│   │   └── AnomalyAlert.cs
│   └── Insights/
│       ├── BusinessInsight.cs
│       ├── Recommendation.cs
│       └── NaturalLanguageQuery.cs
├── Services/
│   ├── DemandForecastingService.cs
│   ├── AnomalyDetectionService.cs
│   ├── MenuEngineeringService.cs
│   ├── CustomerAnalyticsService.cs
│   ├── LaborOptimizationService.cs
│   ├── NaturalLanguageService.cs
│   └── InsightGeneratorService.cs
├── ML/
│   ├── Models/
│   │   ├── SalesForecaster.zip
│   │   └── AnomalyDetector.zip
│   └── Training/
│       ├── ModelTrainer.cs
│       └── DataPreprocessor.cs
└── Extensions/
    └── AIServiceCollectionExtensions.cs
```

### Database Schema Additions

```sql
-- Customer Analytics
CREATE TABLE CustomerScores (
    Id INT PRIMARY KEY IDENTITY,
    CustomerId INT NOT NULL,
    RecencyScore INT NOT NULL,
    FrequencyScore INT NOT NULL,
    MonetaryScore INT NOT NULL,
    RFMSegment NVARCHAR(50) NOT NULL,
    ChurnRiskScore DECIMAL(5,2),
    LifetimeValue DECIMAL(18,2),
    LastCalculated DATETIME2 NOT NULL,
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
);

-- AI Predictions
CREATE TABLE SalesForecasts (
    Id INT PRIMARY KEY IDENTITY,
    ForecastDate DATE NOT NULL,
    PredictedSales DECIMAL(18,2) NOT NULL,
    ConfidenceLower DECIMAL(18,2),
    ConfidenceUpper DECIMAL(18,2),
    ActualSales DECIMAL(18,2),
    ModelVersion NVARCHAR(50),
    CreatedAt DATETIME2 NOT NULL
);

-- Anomaly Alerts
CREATE TABLE AnomalyAlerts (
    Id INT PRIMARY KEY IDENTITY,
    AlertType NVARCHAR(50) NOT NULL,
    Severity NVARCHAR(20) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    EntityType NVARCHAR(50),
    EntityId INT,
    DetectedAt DATETIME2 NOT NULL,
    AcknowledgedAt DATETIME2,
    AcknowledgedBy INT,
    IsResolved BIT DEFAULT 0
);

-- AI Insights
CREATE TABLE BusinessInsights (
    Id INT PRIMARY KEY IDENTITY,
    InsightType NVARCHAR(50) NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    Recommendation NVARCHAR(MAX),
    Impact NVARCHAR(50),
    Priority INT,
    GeneratedAt DATETIME2 NOT NULL,
    ExpiresAt DATETIME2,
    IsActioned BIT DEFAULT 0
);
```

### External API Integrations

| Service | Purpose | Provider Options |
|---------|---------|------------------|
| Weather API | Demand forecasting input | OpenWeatherMap, WeatherAPI |
| Events API | Local events calendar | Eventbrite, PredictHQ |
| LLM API | Natural language insights | Anthropic Claude, OpenAI GPT |
| ML Platform | Model training/hosting | ML.NET, Azure ML, AWS SageMaker |

---

## Quick Implementation Steps

### Step 1: Add Missing Report Models

Create `src/HospitalityPOS.Core/Models/Reports/FinancialReports.cs`:

```csharp
namespace HospitalityPOS.Core.Models.Reports;

public class PrimeCostReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal TotalLaborCost { get; set; }
    public decimal PrimeCost => CostOfGoodsSold + TotalLaborCost;
    public decimal PrimeCostPercentage => TotalRevenue > 0
        ? Math.Round(PrimeCost / TotalRevenue * 100, 2) : 0;
    public bool IsHealthy => PrimeCostPercentage <= 60;
    public string Status => PrimeCostPercentage switch
    {
        <= 55 => "Excellent",
        <= 60 => "Good",
        <= 65 => "Warning",
        _ => "Critical"
    };
}

public class FoodCostReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal BeginningInventory { get; set; }
    public decimal Purchases { get; set; }
    public decimal EndingInventory { get; set; }
    public decimal FoodSales { get; set; }
    public decimal FoodCost => BeginningInventory + Purchases - EndingInventory;
    public decimal FoodCostPercentage => FoodSales > 0
        ? Math.Round(FoodCost / FoodSales * 100, 2) : 0;
    public string Status => FoodCostPercentage switch
    {
        <= 28 => "Excellent",
        <= 32 => "Good",
        <= 35 => "Acceptable",
        _ => "High"
    };
}

public class LaborCostReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal GrossRevenue { get; set; }
    public decimal TotalWages { get; set; }
    public decimal PayrollTaxes { get; set; }
    public decimal Benefits { get; set; }
    public decimal TotalLaborCost => TotalWages + PayrollTaxes + Benefits;
    public decimal LaborCostPercentage => GrossRevenue > 0
        ? Math.Round(TotalLaborCost / GrossRevenue * 100, 2) : 0;
    public decimal SalesPerLaborHour { get; set; }
    public string Status => LaborCostPercentage switch
    {
        <= 25 => "Excellent",
        <= 30 => "Good",
        <= 35 => "Acceptable",
        _ => "High"
    };
}
```

### Step 2: Add Menu Engineering Model

Create `src/HospitalityPOS.Core/Models/Reports/MenuEngineeringReport.cs`:

```csharp
namespace HospitalityPOS.Core.Models.Reports;

public enum MenuItemClassification
{
    Star,       // High profit, High popularity
    Plow,       // Low profit, High popularity
    Puzzle,     // High profit, Low popularity
    Dog         // Low profit, Low popularity
}

public class MenuEngineeringItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal FoodCost { get; set; }
    public decimal ContributionMargin => SellingPrice - FoodCost;
    public decimal MarginPercentage => SellingPrice > 0
        ? Math.Round(ContributionMargin / SellingPrice * 100, 2) : 0;
    public decimal PopularityIndex { get; set; }
    public decimal ProfitabilityIndex { get; set; }
    public MenuItemClassification Classification { get; set; }
    public string Recommendation { get; set; } = string.Empty;
}

public class MenuEngineeringReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<MenuEngineeringItem> Items { get; set; } = [];
    public int StarCount => Items.Count(i => i.Classification == MenuItemClassification.Star);
    public int PlowCount => Items.Count(i => i.Classification == MenuItemClassification.Plow);
    public int PuzzleCount => Items.Count(i => i.Classification == MenuItemClassification.Puzzle);
    public int DogCount => Items.Count(i => i.Classification == MenuItemClassification.Dog);
    public decimal AverageMargin { get; set; }
    public decimal AveragePopularity { get; set; }
}
```

### Step 3: Add Customer Analytics Models

Create `src/HospitalityPOS.Core/Models/Analytics/CustomerAnalytics.cs`:

```csharp
namespace HospitalityPOS.Core.Models.Analytics;

public enum RFMSegment
{
    Champion,
    Loyal,
    Potential,
    NewCustomer,
    Promising,
    NeedAttention,
    AboutToSleep,
    AtRisk,
    CantLoseThem,
    Hibernating,
    Lost
}

public class CustomerRFMScore
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime LastPurchaseDate { get; set; }
    public int DaysSinceLastPurchase { get; set; }
    public int PurchaseCount { get; set; }
    public decimal TotalSpent { get; set; }
    public int RecencyScore { get; set; }  // 1-5
    public int FrequencyScore { get; set; } // 1-5
    public int MonetaryScore { get; set; }  // 1-5
    public int RFMScore => RecencyScore * 100 + FrequencyScore * 10 + MonetaryScore;
    public RFMSegment Segment { get; set; }
    public decimal ChurnRiskScore { get; set; }
    public decimal PredictedLifetimeValue { get; set; }
}

public class CustomerAnalyticsReport
{
    public DateTime GeneratedAt { get; set; }
    public int TotalCustomers { get; set; }
    public List<CustomerRFMScore> Customers { get; set; } = [];
    public Dictionary<RFMSegment, int> SegmentCounts { get; set; } = [];
    public int AtRiskCount { get; set; }
    public decimal TotalAtRiskValue { get; set; }
}
```

### Step 4: Add AI Service Interface

Create `src/HospitalityPOS.Core/Interfaces/IAIInsightsService.cs`:

```csharp
namespace HospitalityPOS.Core.Interfaces;

public interface IAIInsightsService
{
    // Forecasting
    Task<SalesForecast> ForecastSalesAsync(DateTime date);
    Task<List<ProductDemandForecast>> ForecastProductDemandAsync(DateTime date);

    // Recommendations
    Task<List<InventoryRecommendation>> GetInventoryRecommendationsAsync();
    Task<List<MenuRecommendation>> GetMenuRecommendationsAsync();
    Task<List<StaffingRecommendation>> GetStaffingRecommendationsAsync(DateTime date);

    // Anomaly Detection
    Task<List<AnomalyAlert>> DetectAnomaliesAsync(DateTime startDate, DateTime endDate);
    Task<List<FraudAlert>> DetectFraudPatternsAsync(int? employeeId = null);

    // Natural Language
    Task<string> AnswerBusinessQuestionAsync(string question);
    Task<string> GenerateReportSummaryAsync(string reportType, object reportData);

    // Customer Analytics
    Task<List<CustomerRFMScore>> CalculateRFMScoresAsync();
    Task<List<ChurnRiskAlert>> PredictChurnAsync();
}
```

### Step 5: Create AI Insights Dashboard ViewModel

Create `src/HospitalityPOS.WPF/ViewModels/AIInsightsDashboardViewModel.cs`:

```csharp
namespace HospitalityPOS.WPF.ViewModels;

public partial class AIInsightsDashboardViewModel : ObservableObject
{
    private readonly IAIInsightsService _aiService;

    [ObservableProperty] private SalesForecast? _todaysForecast;
    [ObservableProperty] private List<AnomalyAlert> _activeAlerts = [];
    [ObservableProperty] private List<BusinessInsight> _topInsights = [];
    [ObservableProperty] private List<InventoryRecommendation> _inventoryAlerts = [];
    [ObservableProperty] private string _aiChatResponse = string.Empty;
    [ObservableProperty] private string _userQuestion = string.Empty;
    [ObservableProperty] private bool _isLoading;

    [RelayCommand]
    private async Task LoadDashboardAsync()
    {
        IsLoading = true;
        try
        {
            TodaysForecast = await _aiService.ForecastSalesAsync(DateTime.Today);
            ActiveAlerts = await _aiService.DetectAnomaliesAsync(
                DateTime.Today.AddDays(-7), DateTime.Today);
            InventoryAlerts = await _aiService.GetInventoryRecommendationsAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AskQuestionAsync()
    {
        if (string.IsNullOrWhiteSpace(UserQuestion)) return;

        IsLoading = true;
        try
        {
            AiChatResponse = await _aiService.AnswerBusinessQuestionAsync(UserQuestion);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

---

## Success Metrics

| Metric | Current | Target | Measurement |
|--------|---------|--------|-------------|
| Report coverage | 70% | 95% | Reports implemented vs industry standard |
| Time to insight | Manual | <5 sec | Query to answer time |
| Forecast accuracy | N/A | 85%+ | Predicted vs actual sales |
| Food waste | Baseline | -15% | Waste tracking reduction |
| Labor efficiency | Baseline | +10% | SPLH improvement |
| Customer retention | Baseline | +5% | Repeat visit rate |

---

## Appendix: Competitive Analysis

| Feature | HospitalityPOS | Toast | Square | Lightspeed |
|---------|---------------|-------|--------|------------|
| Basic Reports | Yes | Yes | Yes | Yes |
| Z/X Reports | Yes | Yes | Yes | Yes |
| Menu Engineering | Planned | Yes | No | Yes |
| AI Forecasting | Planned | Beta | Yes | Yes |
| Natural Language | Planned | No | No | Beta |
| Auto Insights | Planned | No | Limited | Limited |
| Labor Optimization | Planned | Partner | Partner | Partner |

---

*Document prepared for HospitalityPOS development team*
