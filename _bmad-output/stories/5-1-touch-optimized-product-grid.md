# Story 5.1: Touch-Optimized Product Grid

Status: done

## Story

As a waiter/cashier,
I want a touch-friendly product selection interface,
So that I can quickly add items to orders.

## Acceptance Criteria

1. **Given** a work period is open
   **When** the POS screen is displayed
   **Then** products should be shown as large, finger-friendly tiles (minimum 44x44 pixels)

2. **Given** products are displayed
   **When** viewing product tiles
   **Then** each tile should display: product image, name, price

3. **Given** products are organized
   **When** navigating products
   **Then** category tabs should allow quick navigation

4. **Given** a product tile is tapped
   **When** adding to order
   **Then** tapping a product should add it to the current order

5. **Given** a product is out of stock
   **When** viewing products
   **Then** out-of-stock items should be visually marked and not selectable

## Tasks / Subtasks

- [x] Task 1: Create Three-Panel Layout Structure (AC: #1, #3)
  - [x] Create POSView.xaml with Grid layout (3 columns)
  - [x] Left column: Order Ticket panel (300px width)
  - [x] Middle column: Category panel (140px width, vertical list)
  - [x] Right column: Product Grid (remaining width)
  - [x] Add header bar with user info, table info, back/logout buttons

- [x] Task 2: Create Product Grid Component (AC: #1, #2)
  - [x] Created integrated product grid in POSView.xaml
  - [x] Design touch-friendly tile layout (110x130 pixels with images)
  - [x] Display image, name, price on each tile
  - [x] Implement pagination tabs (numbered style with GoToPageCommand)
  - [x] Uses WrapPanel for responsive grid

- [x] Task 3: Implement Vertical Category Panel (AC: #3)
  - [x] Create vertical scrollable category list with momentum scroll
  - [x] Highlight selected category with green background
  - [x] Add left border indicator for selection (via DataTrigger)
  - [x] Include "All" option at top (Favorites deferred to later epic)

- [x] Task 4: Implement Add to Order (AC: #4)
  - [x] Handle tile tap event via AddToOrderCommand
  - [x] Add product to current order with quantity tracking
  - [x] Show visual feedback on tap (green border highlight)
  - [ ] Sound deferred - requires additional infrastructure

- [x] Task 5: Handle Out of Stock (AC: #5)
  - [x] Mark out-of-stock items visually with reduced opacity
  - [x] Disable tap on out-of-stock (IsEnabled binding)
  - [x] Show "OUT" overlay on tile with red text
  - [ ] Hide option deferred - not in current requirements

- [x] Task 6: Optimize Touch Experience
  - [x] Button press feedback with color change
  - [x] Implement momentum scrolling (PanningMode="VerticalOnly")
  - [ ] Haptic feedback deferred - platform dependent
  - [ ] Touch screen testing deferred - development on non-touch device

## Dev Notes

### Design Research Insights

Based on research from SambaPOS, Aronium, Floreant, and industry best practices:
- Reference: `_bmad-output/research/pos-design-research.md`

**Three-Panel Layout (SambaPOS Pattern - Actual Design):**

Based on actual SambaPOS screenshot analysis:
- **ORDER TICKET** on LEFT (order items, totals, payment buttons)
- **CATEGORIES** in MIDDLE (vertical list, selected = green highlight)
- **PRODUCTS GRID** on RIGHT (with pagination tabs 1-5)

```
+---------------------------+----------------+----------------------------------+
|       ORDER TICKET        |   CATEGORIES   |         PRODUCTS GRID            |
|         (Left)            |    (Middle)    |           (Right)                |
+---------------------------+----------------+----------------------------------+
| [SambaPOS Logo]           |                | [1] [2] [3] [4] [5]  <- Pages    |
| Change Table              |   [Pizza]      |                                  |
| Table: Inside 01          |   [Pide]       | +--------+ +--------+ +--------+ |
| Status: New Orders        |   [Burgers]    | |[IMAGE] | |[IMAGE] | |[IMAGE] | |
|                           |   [Sandwich]*  | | Spicy  | | Turkey | |Chicken | |
| [Select Customer]         |   [Snacks]     | | Italian| | Breast | |& Bacon | |
| [Ticket Note]             |   [Salads]     | | $9.90  | | $6.95  | | $8.95  | |
|                           |   [Cakes]      | +--------+ +--------+ +--------+ |
| Spicy Italian   $9.90     |   [Dessert]    |                                  |
| Turkey Breast   $6.95     |   [Coffee]     | +--------+ +--------+ +--------+ |
| Chicken & Bacon $8.95     |   [Drinks]     | |[IMAGE] | |[IMAGE] | |[IMAGE] | |
|                           |                | | Veggie | | Philly | | Meatball|
|                           |   * = selected | | Patty  | | Cheese | | Marinara|
| Balance:         $31.50   |   (green bar)  | | $7.95  | | $8.95  | | $7.95  | |
|                           |                | +--------+ +--------+ +--------+ |
| [Cash]     [Credit Card]  |                |                                  |
|  (orange)     (blue)      |                | +--------+ +--------+ +--------+ |
|                           |                | |[IMAGE] | |[IMAGE] | |[IMAGE] | |
| [Settle]     [Close]      |                | | Club   | | BLT    | | Grilled |
+---------------------------+----------------+----------------------------------+
```

**Key Layout Features:**
- Categories panel is narrow (~120px) in the middle
- Product tiles have image, name, and price
- Pagination tabs (1-5) at top of product grid
- Selected category highlighted with green background
- Payment buttons color-coded: Cash=Orange, Credit Card=Blue
- Order ticket shows running total and balance

### Main POS Screen Layout (Three-Panel Structure)

```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="50"/>  <!-- Header Bar -->
        <RowDefinition Height="*"/>   <!-- Main Content -->
    </Grid.RowDefinitions>

    <!-- Header Bar -->
    <Border Grid.Row="0" Background="#1F2937">
        <Grid>
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Left">
                <Image Source="/Assets/logo.png" Height="30" Margin="10,0"/>
                <TextBlock Text="Current User: John" Foreground="White"
                           VerticalAlignment="Center" Margin="20,0"/>
                <TextBlock Text="Table: Inside 01" Foreground="#9CA3AF"
                           VerticalAlignment="Center" Margin="20,0"/>
            </StackPanel>
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="10,0">
                <Button Content="Logout" Style="{StaticResource HeaderButton}"/>
                <Button Content="Settings" Style="{StaticResource HeaderButton}"/>
            </StackPanel>
        </Grid>
    </Border>

    <!-- Main Three-Panel Content -->
    <Grid Grid.Row="1">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="280"/>  <!-- Order Ticket (Left) -->
            <ColumnDefinition Width="120"/>  <!-- Categories (Middle) -->
            <ColumnDefinition Width="*"/>    <!-- Products (Right) -->
        </Grid.ColumnDefinitions>

        <!-- LEFT: Order Ticket Panel -->
        <Border Grid.Column="0" Background="#FFFFFF" BorderBrush="#E5E7EB"
                BorderThickness="0,0,1,0">
            <local:OrderTicketControl DataContext="{Binding CurrentOrder}"/>
        </Border>

        <!-- MIDDLE: Category Panel -->
        <Border Grid.Column="1" Background="#F3F4F6">
            <local:CategoryPanelControl DataContext="{Binding Categories}"/>
        </Border>

        <!-- RIGHT: Product Grid -->
        <Border Grid.Column="2" Background="#FFFFFF">
            <local:ProductGridControl DataContext="{Binding Products}"/>
        </Border>
    </Grid>
</Grid>
```

**Touch Target Requirements (Floreant/Industry Standard):**
| Element | Minimum Size | Recommended Size |
|---------|-------------|------------------|
| Product tiles | 44x44 px | 100x120 px |
| Category buttons | 44x60 px | Full width x 60 px |
| Action buttons | 44x44 px | 60x44 px |
| Spacing between buttons | 4px | 8px |

**Key Design Principles:**
- No tooltips (useless on touch screens)
- No hover effects (direct activation only)
- Clear visual affordances for interactive elements
- Thumb Zone optimization (primary actions at bottom center)

### Product Tile Design

```
+------------------+
|                  |
|    [PRODUCT      |
|     IMAGE]       |
|                  |
|------------------|
| Tusker Lager     |
| KSh 350.00       |
+------------------+
   (100x120 px)
```

### Product Grid XAML

```xml
<ScrollViewer VerticalScrollBarVisibility="Auto"
              HorizontalScrollBarVisibility="Disabled"
              PanningMode="VerticalOnly">
    <ItemsControl ItemsSource="{Binding Products}">
        <ItemsControl.ItemsPanel>
            <ItemsPanelTemplate>
                <UniformGrid Columns="{Binding GridColumns}"/>
            </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <Button Command="{Binding DataContext.AddToOrderCommand,
                                 RelativeSource={RelativeSource AncestorType=ItemsControl}}"
                        CommandParameter="{Binding}"
                        IsEnabled="{Binding IsInStock}"
                        Style="{StaticResource ProductTileButton}">
                    <Grid Width="100" Height="120">
                        <Grid.RowDefinitions>
                            <RowDefinition Height="80"/>
                            <RowDefinition Height="*"/>
                        </Grid.RowDefinitions>

                        <!-- Product Image -->
                        <Image Source="{Binding ImageSource}"
                               Stretch="UniformToFill"/>

                        <!-- Out of Stock Overlay -->
                        <Border Background="#CC000000"
                                Visibility="{Binding IsOutOfStock,
                                           Converter={StaticResource BoolToVisibility}}">
                            <TextBlock Text="OUT"
                                       Foreground="White"
                                       FontWeight="Bold"
                                       FontSize="16"
                                       HorizontalAlignment="Center"
                                       VerticalAlignment="Center"/>
                        </Border>

                        <!-- Product Info -->
                        <StackPanel Grid.Row="1"
                                    VerticalAlignment="Center"
                                    Margin="4,0">
                            <TextBlock Text="{Binding Name}"
                                       FontSize="12"
                                       FontWeight="SemiBold"
                                       TextTrimming="CharacterEllipsis"/>
                            <TextBlock Text="{Binding Price, StringFormat='KSh {0:N0}'}"
                                       FontSize="11"
                                       Foreground="#22C55E"/>
                        </StackPanel>
                    </Grid>
                </Button>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</ScrollViewer>
```

### Category Panel (Vertical - Middle Column)

The category panel is positioned in the middle of the three-panel layout as a vertical scrollable list:

```xml
<!-- Category Panel - Middle Column -->
<Border Width="120" Background="#F3F4F6">
    <ScrollViewer VerticalScrollBarVisibility="Auto"
                  HorizontalScrollBarVisibility="Disabled"
                  PanningMode="VerticalOnly">
        <ItemsControl ItemsSource="{Binding Categories}">
            <ItemsControl.ItemsPanel>
                <ItemsPanelTemplate>
                    <StackPanel Orientation="Vertical"/>
                </ItemsPanelTemplate>
            </ItemsControl.ItemsPanel>
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <Button Content="{Binding Name}"
                            Command="{Binding DataContext.SelectCategoryCommand,
                                     RelativeSource={RelativeSource AncestorType=ItemsControl}}"
                            CommandParameter="{Binding}"
                            Style="{StaticResource CategoryButtonStyle}"
                            Height="50"
                            HorizontalContentAlignment="Left"
                            Padding="12,0"/>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
    </ScrollViewer>
</Border>
```

### Category Button Style (SambaPOS Pattern)

```xml
<Style x:Key="CategoryButtonStyle" TargetType="Button">
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="Foreground" Value="#374151"/>
    <Setter Property="BorderThickness" Value="0"/>
    <Setter Property="FontSize" Value="14"/>
    <Setter Property="FontWeight" Value="Medium"/>
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="Button">
                <Border x:Name="border"
                        Background="{TemplateBinding Background}"
                        BorderThickness="3,0,0,0"
                        BorderBrush="Transparent">
                    <ContentPresenter VerticalAlignment="Center"
                                      HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}"
                                      Margin="{TemplateBinding Padding}"/>
                </Border>
                <ControlTemplate.Triggers>
                    <!-- Selected state - green highlight bar on left -->
                    <DataTrigger Binding="{Binding IsSelected}" Value="True">
                        <Setter TargetName="border" Property="Background" Value="#22C55E"/>
                        <Setter Property="Foreground" Value="White"/>
                        <Setter TargetName="border" Property="BorderBrush" Value="#16A34A"/>
                    </DataTrigger>
                    <Trigger Property="IsPressed" Value="True">
                        <Setter TargetName="border" Property="Background" Value="#E5E7EB"/>
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

### Product Grid Pagination Tabs

```xml
<!-- Pagination tabs at top of product grid (SambaPOS style) -->
<StackPanel Orientation="Horizontal" HorizontalAlignment="Left" Margin="0,0,0,8">
    <ItemsControl ItemsSource="{Binding PageNumbers}">
        <ItemsControl.ItemsPanel>
            <ItemsPanelTemplate>
                <StackPanel Orientation="Horizontal"/>
            </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <Button Content="{Binding}"
                        Command="{Binding DataContext.GoToPageCommand,
                                 RelativeSource={RelativeSource AncestorType=ItemsControl}}"
                        CommandParameter="{Binding}"
                        Width="40" Height="40"
                        Margin="2,0"
                        Style="{StaticResource PageTabButton}"/>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</StackPanel>
```

### Touch-Friendly Button Style

```xml
<Style x:Key="ProductTileButton" TargetType="Button">
    <Setter Property="Background" Value="White"/>
    <Setter Property="BorderBrush" Value="#E5E7EB"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="Margin" Value="4"/>
    <Setter Property="Cursor" Value="Hand"/>
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="Button">
                <Border Background="{TemplateBinding Background}"
                        BorderBrush="{TemplateBinding BorderBrush}"
                        BorderThickness="{TemplateBinding BorderThickness}"
                        CornerRadius="8">
                    <ContentPresenter/>
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property="IsPressed" Value="True">
                        <Setter Property="Background" Value="#E0F2FE"/>
                        <Setter Property="BorderBrush" Value="#0EA5E9"/>
                    </Trigger>
                    <Trigger Property="IsEnabled" Value="False">
                        <Setter Property="Opacity" Value="0.5"/>
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

### Grid Column Calculation

```csharp
private void CalculateGridColumns()
{
    const int tileWidth = 110; // tile width + margins
    const int minColumns = 3;
    const int maxColumns = 8;

    var availableWidth = ActualWidth - 20; // padding
    var columns = (int)(availableWidth / tileWidth);

    GridColumns = Math.Clamp(columns, minColumns, maxColumns);
}
```

### Performance Optimization
- Virtualize product grid for large catalogs
- Lazy load product images
- Cache category/product data
- Use compiled bindings where possible

### References
- [Source: docs/PRD_Hospitality_POS_System.md#8.2-Main-POS-Screen-Layout]
- [Source: docs/PRD_Hospitality_POS_System.md#8.3-Product-Display-Requirements]
- [Source: docs/PRD_Hospitality_POS_System.md#SO-001]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
1. Created complete three-panel POS layout following SambaPOS design pattern
2. Left panel (300px): Order Ticket with items, quantity controls, totals, payment buttons
3. Middle panel (140px): Vertical category list with green selection highlight
4. Right panel: Product grid with touch-friendly tiles (110x130px) and pagination tabs
5. Dark theme implementation (#1E1E2E, #2D2D44, #252538) for professional appearance
6. Touch-optimized: 44px+ button heights, momentum scrolling, clear visual feedback
7. Order item management: add, increase/decrease quantity, remove, clear order
8. Out-of-stock handling: visual overlay, disabled tiles, error message on tap attempt
9. Work period validation: redirects back if no work period is open
10. Category filtering: "All" shows all products, specific categories filter by categoryId
11. Order totals: automatic calculation with 16% VAT (Kenya)
12. Deferred items: sound feedback, haptic feedback, "Favorites" category, hide out-of-stock option

### Code Review Fixes Applied
- Fixed: Stock validation when increasing quantity - now checks AvailableStock before incrementing
- Fixed: Added try-catch to OnNavigatedTo async void method for proper exception handling
- Fixed: Added exception handling to fire-and-forget category filtering call
- Fixed: Replaced magic number 0.16m with named constant TaxRate
- Fixed: Added current page indicator to pagination tabs using MultiBinding converter
- Fixed: Removed unused GridColumns property from POSViewModel

### File List
- src/HospitalityPOS.WPF/Views/POSView.xaml (new)
- src/HospitalityPOS.WPF/Views/POSView.xaml.cs (new)
- src/HospitalityPOS.WPF/ViewModels/POSViewModel.cs (new)
- src/HospitalityPOS.WPF/ViewModels/MainViewModel.cs (modified - added NavigateToPOSAsync)
- src/HospitalityPOS.WPF/App.xaml.cs (modified - registered POSViewModel)
- src/HospitalityPOS.WPF/Converters/PageNumberToStyleConverter.cs (new - code review fix)
