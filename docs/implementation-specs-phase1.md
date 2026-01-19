# Implementation Specifications - Phase 1: Quick Wins & POS Enhancements

**Document Version:** 1.0
**Target Sprint:** Phase 1 (Weeks 1-2)
**Priority:** Critical to High

---

## Feature 1: Real-Time Product Search in POS

**Priority:** Critical
**Estimated Effort:** 2-3 days
**Files to Modify:**
- `Views/POSView.xaml`
- `ViewModels/POSViewModel.cs`
- `Views/Dialogs/ProductSearchDialog.xaml` (optional enhancement)

### Current State Analysis

**Location:** `POSView.xaml:339-396`, `POSViewModel.cs:533-576`

Current search behavior:
1. User types in barcode input field
2. Presses Enter or clicks Search button
3. System searches for exact barcode match first
4. If no match, opens ProductSearchDialog modal
5. User must select product and click "Select"

**Problems:**
- Requires Enter key press - not real-time
- Modal dialog interrupts flow
- No autocomplete suggestions
- No visual feedback while typing

### Target State

Real-time dropdown search that:
1. Shows suggestions as user types (debounced 300ms)
2. Displays product name, code, price, and stock in dropdown
3. Allows arrow key navigation and Enter to select
4. Keeps focus in search field for continuous scanning
5. Falls back to barcode lookup on Enter if no dropdown selection

### Technical Implementation

#### 1.1 Add Search Popup to POSView.xaml

Replace the current Search Bar section (lines 326-396) with:

```xml
<!-- Search Bar with Autocomplete -->
<Border Grid.Row="0" Background="#252538" Padding="12,10">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>

        <!-- Barcode Icon -->
        <TextBlock Text="&#xE8B7;" FontFamily="Segoe MDL2 Assets" FontSize="20"
                   Foreground="#22C55E" VerticalAlignment="Center" Margin="0,0,10,0"/>

        <!-- Search Input with Popup -->
        <Grid Grid.Column="1">
            <TextBox x:Name="RetailBarcodeInput"
                     Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
                     Background="#2D2D44"
                     Foreground="White"
                     CaretBrush="White"
                     BorderBrush="#22C55E"
                     BorderThickness="2"
                     Padding="12,8"
                     FontSize="16"
                     FontWeight="Medium"
                     VerticalContentAlignment="Center"
                     PreviewKeyDown="SearchInput_PreviewKeyDown">
                <TextBox.InputBindings>
                    <KeyBinding Key="Enter" Command="{Binding ProcessSearchInputCommand}"/>
                    <KeyBinding Key="Escape" Command="{Binding CloseSearchDropdownCommand}"/>
                </TextBox.InputBindings>
            </TextBox>

            <!-- Placeholder -->
            <TextBlock Text="Scan barcode or type product name (F1)..."
                       Foreground="#6B7280" FontSize="13"
                       VerticalAlignment="Center" Margin="16,0,0,0"
                       IsHitTestVisible="False"
                       Visibility="{Binding SearchText, Converter={StaticResource StringToVisibilityConverter}}"/>

            <!-- Search Results Dropdown -->
            <Popup x:Name="SearchDropdown"
                   PlacementTarget="{Binding ElementName=RetailBarcodeInput}"
                   Placement="Bottom"
                   StaysOpen="False"
                   AllowsTransparency="True"
                   PopupAnimation="Fade"
                   IsOpen="{Binding IsSearchDropdownOpen}">
                <Border Background="#2D2D44"
                        BorderBrush="#22C55E"
                        BorderThickness="1"
                        CornerRadius="0,0,8,8"
                        Width="{Binding ElementName=RetailBarcodeInput, Path=ActualWidth}"
                        MaxHeight="300">
                    <Grid>
                        <!-- Loading Indicator -->
                        <StackPanel Orientation="Horizontal" Margin="12,8"
                                    Visibility="{Binding IsSearching, Converter={StaticResource BoolToVisibility}}">
                            <ProgressBar IsIndeterminate="True" Width="100" Height="3" Foreground="#22C55E"/>
                            <TextBlock Text="Searching..." Foreground="#9CA3AF" FontSize="12" Margin="8,0,0,0"/>
                        </StackPanel>

                        <!-- Results List -->
                        <ListBox ItemsSource="{Binding SearchResults}"
                                 SelectedItem="{Binding SelectedSearchResult}"
                                 Background="Transparent"
                                 BorderThickness="0"
                                 MaxHeight="280"
                                 ScrollViewer.HorizontalScrollBarVisibility="Disabled"
                                 Visibility="{Binding IsSearching, Converter={StaticResource InverseBoolToVisibility}}">
                            <ListBox.ItemContainerStyle>
                                <Style TargetType="ListBoxItem">
                                    <Setter Property="Background" Value="Transparent"/>
                                    <Setter Property="Padding" Value="12,8"/>
                                    <Setter Property="Cursor" Value="Hand"/>
                                    <Setter Property="Template">
                                        <Setter.Value>
                                            <ControlTemplate TargetType="ListBoxItem">
                                                <Border x:Name="Bd" Background="{TemplateBinding Background}"
                                                        Padding="{TemplateBinding Padding}">
                                                    <ContentPresenter/>
                                                </Border>
                                                <ControlTemplate.Triggers>
                                                    <Trigger Property="IsMouseOver" Value="True">
                                                        <Setter TargetName="Bd" Property="Background" Value="#3D3D5C"/>
                                                    </Trigger>
                                                    <Trigger Property="IsSelected" Value="True">
                                                        <Setter TargetName="Bd" Property="Background" Value="#22C55E"/>
                                                    </Trigger>
                                                </ControlTemplate.Triggers>
                                            </ControlTemplate>
                                        </Setter.Value>
                                    </Setter>
                                </Style>
                            </ListBox.ItemContainerStyle>
                            <ListBox.ItemTemplate>
                                <DataTemplate>
                                    <Grid>
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width="50"/>
                                            <ColumnDefinition Width="*"/>
                                            <ColumnDefinition Width="60"/>
                                            <ColumnDefinition Width="80"/>
                                        </Grid.ColumnDefinitions>

                                        <!-- Product Image Thumbnail -->
                                        <Border Width="40" Height="40" CornerRadius="4" Background="#3D3D5C">
                                            <Grid>
                                                <TextBlock Text="&#xE8B9;" FontFamily="Segoe MDL2 Assets"
                                                           FontSize="16" Foreground="#6B7280"
                                                           HorizontalAlignment="Center" VerticalAlignment="Center"/>
                                                <Image Source="{Binding ImagePath}" Stretch="UniformToFill"
                                                       Visibility="{Binding HasImage, Converter={StaticResource BoolToVisibility}}"/>
                                            </Grid>
                                        </Border>

                                        <!-- Product Info -->
                                        <StackPanel Grid.Column="1" Margin="8,0,0,0" VerticalAlignment="Center">
                                            <TextBlock Text="{Binding Name}" Foreground="White" FontSize="13"
                                                       FontWeight="Medium" TextTrimming="CharacterEllipsis"/>
                                            <TextBlock Text="{Binding Code}" Foreground="#9CA3AF" FontSize="11"/>
                                        </StackPanel>

                                        <!-- Stock -->
                                        <TextBlock Grid.Column="2" VerticalAlignment="Center" HorizontalAlignment="Center">
                                            <TextBlock.Style>
                                                <Style TargetType="TextBlock">
                                                    <Setter Property="Text" Value="{Binding CurrentStock}"/>
                                                    <Setter Property="Foreground" Value="#22C55E"/>
                                                    <Setter Property="FontSize" Value="12"/>
                                                    <Style.Triggers>
                                                        <DataTrigger Binding="{Binding IsOutOfStock}" Value="True">
                                                            <Setter Property="Text" Value="OUT"/>
                                                            <Setter Property="Foreground" Value="#EF4444"/>
                                                        </DataTrigger>
                                                    </Style.Triggers>
                                                </Style>
                                            </TextBlock.Style>
                                        </TextBlock>

                                        <!-- Price -->
                                        <TextBlock Grid.Column="3" Text="{Binding Price, StringFormat='KSh {0:N0}'}"
                                                   Foreground="#F59E0B" FontSize="13" FontWeight="SemiBold"
                                                   VerticalAlignment="Center" HorizontalAlignment="Right"/>
                                    </Grid>
                                </DataTemplate>
                            </ListBox.ItemTemplate>
                        </ListBox>

                        <!-- No Results -->
                        <TextBlock Text="No products found" Foreground="#6B7280" FontSize="13"
                                   HorizontalAlignment="Center" Margin="12"
                                   Visibility="{Binding HasNoResults, Converter={StaticResource BoolToVisibility}}"/>
                    </Grid>
                </Border>
            </Popup>
        </Grid>

        <!-- Search Button (still useful for explicit search) -->
        <Button Grid.Column="2" Content="&#xE721;" FontFamily="Segoe MDL2 Assets" FontSize="16"
                Background="#22C55E" Foreground="White"
                Width="44" Height="38" BorderThickness="0" Margin="8,0,0,0"
                Command="{Binding ProcessSearchInputCommand}" Cursor="Hand">
            <Button.Template>
                <ControlTemplate TargetType="Button">
                    <Border Background="{TemplateBinding Background}" CornerRadius="4">
                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                    </Border>
                </ControlTemplate>
            </Button.Template>
        </Button>
    </Grid>
</Border>
```

#### 1.2 ViewModel Changes (POSViewModel.cs)

Add these properties and methods:

```csharp
// === New Properties ===
private string _searchText = string.Empty;
public string SearchText
{
    get => _searchText;
    set
    {
        if (SetProperty(ref _searchText, value))
        {
            // Debounce search
            _searchDebounceTimer?.Stop();
            _searchDebounceTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _searchDebounceTimer.Tick += async (s, e) =>
            {
                _searchDebounceTimer.Stop();
                await PerformSearchAsync(value);
            };
            _searchDebounceTimer.Start();
        }
    }
}

private System.Windows.Threading.DispatcherTimer? _searchDebounceTimer;

private ObservableCollection<ProductSearchResult> _searchResults = new();
public ObservableCollection<ProductSearchResult> SearchResults
{
    get => _searchResults;
    set => SetProperty(ref _searchResults, value);
}

private ProductSearchResult? _selectedSearchResult;
public ProductSearchResult? SelectedSearchResult
{
    get => _selectedSearchResult;
    set
    {
        if (SetProperty(ref _selectedSearchResult, value) && value != null)
        {
            SelectSearchResult(value);
        }
    }
}

private bool _isSearchDropdownOpen;
public bool IsSearchDropdownOpen
{
    get => _isSearchDropdownOpen;
    set => SetProperty(ref _isSearchDropdownOpen, value);
}

private bool _isSearching;
public bool IsSearching
{
    get => _isSearching;
    set => SetProperty(ref _isSearching, value);
}

public bool HasNoResults => !IsSearching && SearchResults.Count == 0 && !string.IsNullOrEmpty(SearchText);

// === New Methods ===
private async Task PerformSearchAsync(string searchText)
{
    if (string.IsNullOrWhiteSpace(searchText) || searchText.Length < 2)
    {
        SearchResults.Clear();
        IsSearchDropdownOpen = false;
        return;
    }

    IsSearching = true;
    IsSearchDropdownOpen = true;

    try
    {
        var results = await Task.Run(() =>
        {
            var term = searchText.ToLower();
            return _allProducts
                .Where(p =>
                    (p.Name?.ToLower().Contains(term) ?? false) ||
                    (p.Code?.ToLower().Contains(term) ?? false) ||
                    (p.Barcode?.ToLower().Contains(term) ?? false))
                .Take(10)
                .Select(p => new ProductSearchResult
                {
                    Id = p.Id,
                    Name = p.Name,
                    Code = p.Code ?? "",
                    Price = p.SellingPrice,
                    CurrentStock = p.Inventory?.CurrentStock ?? 0,
                    IsOutOfStock = p.Inventory == null || p.Inventory.CurrentStock <= 0,
                    ImagePath = _imageService.GetDisplayImagePath(p.ImagePath),
                    HasImage = !string.IsNullOrEmpty(p.ImagePath)
                })
                .ToList();
        });

        SearchResults = new ObservableCollection<ProductSearchResult>(results);
        OnPropertyChanged(nameof(HasNoResults));
    }
    finally
    {
        IsSearching = false;
    }
}

[RelayCommand]
private async Task ProcessSearchInputAsync()
{
    if (SelectedSearchResult != null)
    {
        await SelectSearchResultAsync(SelectedSearchResult);
        return;
    }

    // Fall back to barcode lookup behavior
    await SearchBarcodeAsync();
}

private async Task SelectSearchResultAsync(ProductSearchResult result)
{
    var product = _allProducts.FirstOrDefault(p => p.Id == result.Id);
    if (product != null)
    {
        var tile = CreateProductTileFromEntity(product);
        await AddToOrderWithQuantityAsync(tile);
    }

    SearchText = string.Empty;
    SearchResults.Clear();
    IsSearchDropdownOpen = false;
}

[RelayCommand]
private void CloseSearchDropdown()
{
    IsSearchDropdownOpen = false;
    SearchResults.Clear();
}
```

#### 1.3 Add ProductSearchResult Model

Create new file `ViewModels/Models/ProductSearchResult.cs`:

```csharp
namespace HospitalityPOS.WPF.ViewModels.Models;

public class ProductSearchResult
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal CurrentStock { get; set; }
    public bool IsOutOfStock { get; set; }
    public string? ImagePath { get; set; }
    public bool HasImage { get; set; }
}
```

#### 1.4 Add Keyboard Navigation (Code-Behind)

In `POSView.xaml.cs`:

```csharp
private void SearchInput_PreviewKeyDown(object sender, KeyEventArgs e)
{
    var vm = DataContext as POSViewModel;
    if (vm == null) return;

    if (e.Key == Key.Down && vm.SearchResults.Count > 0)
    {
        // Move focus to dropdown
        var index = vm.SearchResults.IndexOf(vm.SelectedSearchResult);
        if (index < vm.SearchResults.Count - 1)
        {
            vm.SelectedSearchResult = vm.SearchResults[index + 1];
        }
        else if (index == -1)
        {
            vm.SelectedSearchResult = vm.SearchResults[0];
        }
        e.Handled = true;
    }
    else if (e.Key == Key.Up && vm.SearchResults.Count > 0)
    {
        var index = vm.SearchResults.IndexOf(vm.SelectedSearchResult);
        if (index > 0)
        {
            vm.SelectedSearchResult = vm.SearchResults[index - 1];
        }
        e.Handled = true;
    }
}
```

### Testing Criteria

| Test Case | Expected Result |
|-----------|-----------------|
| Type "cok" | Shows products containing "cok" within 300ms |
| Type barcode and Enter | Adds product directly, clears field |
| Arrow Down in results | Highlights next item |
| Enter with selection | Adds selected product to order |
| Escape key | Closes dropdown, keeps text |
| Click outside | Closes dropdown |
| No results | Shows "No products found" message |
| Scan barcode quickly | Auto-adds on Enter without dropdown |

---

## Feature 2: Product Images in POS Grid

**Priority:** High
**Estimated Effort:** 1 day
**Files to Modify:**
- `Views/POSView.xaml` (lines 450-500)
- `Services/ImageService.cs` (may need enhancement)

### Current State

Location: `POSView.xaml:461-480`

Current display:
- 100x115 product tiles with placeholder icon
- Image element exists but likely not loading properly
- No caching or fallback handling

### Target State

1. Display product images with proper aspect ratio
2. Show placeholder for missing images
3. Add loading state for images
4. Cache images for performance

### Technical Implementation

#### 2.1 Enhanced Product Tile Template

Replace product tile content (lines 455-497):

```xml
<Grid Width="100" Height="115">
    <Grid.RowDefinitions>
        <RowDefinition Height="70"/>
        <RowDefinition Height="*"/>
    </Grid.RowDefinitions>

    <!-- Product Image with Loading State -->
    <Border Grid.Row="0" Background="#3D3D5C" CornerRadius="6,6,0,0" ClipToBounds="True">
        <Grid>
            <!-- Placeholder Icon (shows when no image) -->
            <TextBlock Text="&#xE8B9;" FontFamily="Segoe MDL2 Assets" FontSize="24"
                       Foreground="#6B7280" HorizontalAlignment="Center" VerticalAlignment="Center"
                       x:Name="PlaceholderIcon"/>

            <!-- Product Image -->
            <Image x:Name="ProductImage"
                   Stretch="UniformToFill"
                   RenderOptions.BitmapScalingMode="HighQuality">
                <Image.Style>
                    <Style TargetType="Image">
                        <Setter Property="Source" Value="{Binding ImagePath, TargetNullValue={x:Null}}"/>
                        <Style.Triggers>
                            <Trigger Property="Source" Value="{x:Null}">
                                <Setter Property="Visibility" Value="Collapsed"/>
                            </Trigger>
                            <DataTrigger Binding="{Binding ImagePath}" Value="">
                                <Setter Property="Visibility" Value="Collapsed"/>
                            </DataTrigger>
                            <DataTrigger Binding="{Binding ImagePath}" Value="{x:Null}">
                                <Setter Property="Visibility" Value="Collapsed"/>
                            </DataTrigger>
                        </Style.Triggers>
                    </Style>
                </Image.Style>
            </Image>

            <!-- Offer Badge -->
            <Border Background="#EF4444" CornerRadius="0,6,0,4" Padding="3,1"
                    HorizontalAlignment="Right" VerticalAlignment="Top"
                    Visibility="{Binding HasActiveOffer, Converter={StaticResource BoolToVisibility}}">
                <TextBlock Text="OFFER" Foreground="White" FontWeight="Bold" FontSize="8"/>
            </Border>

            <!-- Low Stock Warning Badge -->
            <Border Background="#F59E0B" CornerRadius="4,0,4,0" Padding="3,1"
                    HorizontalAlignment="Left" VerticalAlignment="Top"
                    Visibility="{Binding IsLowStock, Converter={StaticResource BoolToVisibility}}">
                <TextBlock Text="LOW" Foreground="White" FontWeight="Bold" FontSize="7"/>
            </Border>

            <!-- Out of Stock Overlay -->
            <Border Background="#CC000000" CornerRadius="6,6,0,0"
                    Visibility="{Binding IsOutOfStock, Converter={StaticResource BoolToVisibility}}">
                <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center">
                    <TextBlock Text="&#xE711;" FontFamily="Segoe MDL2 Assets"
                               FontSize="16" Foreground="#EF4444"/>
                    <TextBlock Text="OUT" Foreground="#EF4444" FontWeight="Bold" FontSize="10"/>
                </StackPanel>
            </Border>
        </Grid>
    </Border>

    <!-- Product Info -->
    <StackPanel Grid.Row="1" VerticalAlignment="Center" Margin="5,3">
        <TextBlock Text="{Binding Name}" Foreground="White" FontSize="11" FontWeight="Medium"
                   TextTrimming="CharacterEllipsis" MaxHeight="28" TextWrapping="Wrap"
                   LineHeight="14" MaxLines="2"/>

        <!-- Regular Price -->
        <TextBlock Text="{Binding Price, StringFormat='KSh {0:N0}'}"
                   Foreground="#22C55E" FontSize="11" FontWeight="SemiBold" Margin="0,2,0,0"
                   Visibility="{Binding HasActiveOffer, Converter={StaticResource InverseBoolToVisibility}}"/>

        <!-- Offer Price -->
        <StackPanel Visibility="{Binding HasActiveOffer, Converter={StaticResource BoolToVisibility}}"
                    Orientation="Horizontal" Margin="0,2,0,0">
            <TextBlock Text="{Binding OfferPrice, StringFormat='KSh {0:N0}'}"
                       Foreground="#22C55E" FontSize="11" FontWeight="Bold"/>
            <TextBlock Text="{Binding SavingsPercent, StringFormat=' -{0}%'}"
                       Foreground="#EF4444" FontSize="9" FontWeight="SemiBold" VerticalAlignment="Center"/>
        </StackPanel>
    </StackPanel>
</Grid>
```

#### 2.2 ImageService Enhancement

Add caching to `Services/ImageService.cs`:

```csharp
private readonly ConcurrentDictionary<string, BitmapImage> _imageCache = new();

public BitmapImage? GetCachedImage(string? imagePath)
{
    if (string.IsNullOrEmpty(imagePath))
        return null;

    var fullPath = GetDisplayImagePath(imagePath);
    if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
        return null;

    if (_imageCache.TryGetValue(fullPath, out var cached))
        return cached;

    try
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
        bitmap.DecodePixelWidth = 100; // Optimize for tile size
        bitmap.EndInit();
        bitmap.Freeze(); // Enable cross-thread access

        _imageCache.TryAdd(fullPath, bitmap);
        return bitmap;
    }
    catch
    {
        return null;
    }
}
```

#### 2.3 Update ProductTileViewModel

Add `IsLowStock` property:

```csharp
public bool IsLowStock { get; set; }
// Set during CreateProductTileFromEntity:
// IsLowStock = p.IsLowStock
```

### Testing Criteria

| Test Case | Expected Result |
|-----------|-----------------|
| Product with image | Shows image, no placeholder |
| Product without image | Shows package icon placeholder |
| Out of stock product | Shows dark overlay with "OUT" |
| Low stock product | Shows orange "LOW" badge |
| Product with offer | Shows red "OFFER" badge |
| Scrolling product grid | Images load smoothly (cached) |

---

## Feature 3: Split Payment

**Priority:** High
**Estimated Effort:** 3-4 days
**Files to Create/Modify:**
- `Views/Dialogs/SplitPaymentDialog.xaml` (NEW)
- `ViewModels/Dialogs/SplitPaymentDialogViewModel.cs` (NEW)
- `Views/POSView.xaml` (add split payment button)
- `ViewModels/POSViewModel.cs` (add split payment command)
- `Services/OrderService.cs` (modify to handle multiple payments)

### Current State

- Single payment method per transaction
- No ability to split between Cash/M-Pesa/Card

### Target State

1. "Split Payment" button in payment section
2. Dialog showing order total with payment allocation
3. Allow adding multiple payment methods
4. Track remaining balance
5. Complete only when fully paid

### Technical Implementation

#### 3.1 SplitPaymentDialog.xaml

```xml
<Window x:Class="HospitalityPOS.WPF.Views.Dialogs.SplitPaymentDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Split Payment"
        Width="500" Height="600"
        WindowStartupLocation="CenterOwner"
        WindowStyle="None"
        ResizeMode="NoResize"
        Background="#1E1E2E">

    <Border BorderBrush="#3D3D5C" BorderThickness="1" CornerRadius="8">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="50"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>

            <!-- Header -->
            <Border Grid.Row="0" Background="#252538" CornerRadius="8,8,0,0" Padding="16,0">
                <Grid>
                    <TextBlock Text="Split Payment" FontSize="18" FontWeight="Bold"
                               Foreground="White" VerticalAlignment="Center"/>
                    <Button Content="X" HorizontalAlignment="Right"
                            Background="Transparent" Foreground="#9CA3AF"
                            BorderThickness="0" Width="32" Height="32"
                            Command="{Binding CancelCommand}"/>
                </Grid>
            </Border>

            <!-- Order Total -->
            <Border Grid.Row="1" Background="#2D2D44" Margin="16,16,16,0" CornerRadius="8" Padding="16">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock Text="Order Total" Foreground="#9CA3AF" FontSize="14"/>
                    <TextBlock Grid.Column="1" Text="{Binding OrderTotal, StringFormat='KSh {0:N2}'}"
                               Foreground="White" FontSize="24" FontWeight="Bold"/>
                </Grid>
            </Border>

            <!-- Payments List -->
            <Border Grid.Row="2" Background="#2D2D44" Margin="16" CornerRadius="8">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                    </Grid.RowDefinitions>

                    <TextBlock Text="PAYMENTS" Foreground="#9CA3AF" FontSize="11"
                               FontWeight="Bold" Margin="16,12"/>

                    <ItemsControl Grid.Row="1" ItemsSource="{Binding Payments}" Margin="8,0,8,8">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <Border Background="#3D3D5C" CornerRadius="6" Margin="0,4" Padding="12">
                                    <Grid>
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width="Auto"/>
                                            <ColumnDefinition Width="*"/>
                                            <ColumnDefinition Width="Auto"/>
                                            <ColumnDefinition Width="Auto"/>
                                        </Grid.ColumnDefinitions>

                                        <!-- Payment Method Icon -->
                                        <Border Width="36" Height="36" CornerRadius="6"
                                                Background="{Binding MethodColor}">
                                            <TextBlock Text="{Binding MethodIcon}"
                                                       FontFamily="Segoe MDL2 Assets"
                                                       FontSize="16" Foreground="White"
                                                       HorizontalAlignment="Center"
                                                       VerticalAlignment="Center"/>
                                        </Border>

                                        <!-- Method Name -->
                                        <TextBlock Grid.Column="1" Text="{Binding MethodName}"
                                                   Foreground="White" FontSize="14"
                                                   VerticalAlignment="Center" Margin="12,0"/>

                                        <!-- Amount -->
                                        <TextBlock Grid.Column="2"
                                                   Text="{Binding Amount, StringFormat='KSh {0:N2}'}"
                                                   Foreground="#22C55E" FontSize="16" FontWeight="Bold"
                                                   VerticalAlignment="Center"/>

                                        <!-- Remove Button -->
                                        <Button Grid.Column="3" Content="X"
                                                Background="#EF4444" Foreground="White"
                                                Width="28" Height="28" Margin="12,0,0,0"
                                                Command="{Binding DataContext.RemovePaymentCommand,
                                                         RelativeSource={RelativeSource AncestorType=ItemsControl}}"
                                                CommandParameter="{Binding}"/>
                                    </Grid>
                                </Border>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </Grid>
            </Border>

            <!-- Add Payment Section -->
            <Border Grid.Row="3" Background="#252538" Margin="16,0,16,16" CornerRadius="8" Padding="16">
                <StackPanel>
                    <TextBlock Text="ADD PAYMENT" Foreground="#9CA3AF" FontSize="11"
                               FontWeight="Bold" Margin="0,0,0,12"/>

                    <!-- Amount Input -->
                    <Grid Margin="0,0,0,12">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Text="KSh" Foreground="White" FontSize="18" FontWeight="Bold"
                                   VerticalAlignment="Center" Margin="0,0,8,0"/>
                        <TextBox Grid.Column="1" Text="{Binding PaymentAmount, UpdateSourceTrigger=PropertyChanged}"
                                 Background="#2D2D44" Foreground="White" FontSize="24" FontWeight="Bold"
                                 BorderBrush="#3D3D5C" Padding="12,8" HorizontalContentAlignment="Right"/>
                    </Grid>

                    <!-- Quick Amount Buttons -->
                    <StackPanel Orientation="Horizontal" Margin="0,0,0,12">
                        <Button Content="Remaining" Command="{Binding SetRemainingCommand}"
                                Background="#22C55E" Foreground="White" Padding="16,8" Margin="0,0,8,0"/>
                        <Button Content="50%" Command="{Binding SetHalfCommand}"
                                Background="#3D3D5C" Foreground="White" Padding="16,8" Margin="0,0,8,0"/>
                        <Button Content="Custom" Command="{Binding ClearAmountCommand}"
                                Background="#3D3D5C" Foreground="White" Padding="16,8"/>
                    </StackPanel>

                    <!-- Payment Method Buttons -->
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <Button Grid.Column="0" Background="#22C55E" Foreground="White"
                                Height="50" Margin="0,0,4,0"
                                Command="{Binding AddCashPaymentCommand}">
                            <StackPanel Orientation="Horizontal">
                                <TextBlock Text="&#xE8C7;" FontFamily="Segoe MDL2 Assets" Margin="0,0,8,0"/>
                                <TextBlock Text="CASH"/>
                            </StackPanel>
                        </Button>
                        <Button Grid.Column="1" Background="#16A34A" Foreground="White"
                                Height="50" Margin="4,0"
                                Command="{Binding AddMpesaPaymentCommand}">
                            <TextBlock Text="M-PESA"/>
                        </Button>
                        <Button Grid.Column="2" Background="#3B82F6" Foreground="White"
                                Height="50" Margin="4,0,0,0"
                                Command="{Binding AddCardPaymentCommand}">
                            <StackPanel Orientation="Horizontal">
                                <TextBlock Text="&#xE8C7;" FontFamily="Segoe MDL2 Assets" Margin="0,0,8,0"/>
                                <TextBlock Text="CARD"/>
                            </StackPanel>
                        </Button>
                    </Grid>
                </StackPanel>
            </Border>

            <!-- Footer with Balance -->
            <Border Grid.Row="4" Background="#1A1A2E" CornerRadius="0,0,8,8" Padding="16">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>

                    <!-- Remaining Balance -->
                    <StackPanel>
                        <TextBlock Text="REMAINING BALANCE" Foreground="#9CA3AF" FontSize="11"/>
                        <TextBlock Text="{Binding RemainingBalance, StringFormat='KSh {0:N2}'}"
                                   FontSize="20" FontWeight="Bold">
                            <TextBlock.Style>
                                <Style TargetType="TextBlock">
                                    <Setter Property="Foreground" Value="#EF4444"/>
                                    <Style.Triggers>
                                        <DataTrigger Binding="{Binding IsFullyPaid}" Value="True">
                                            <Setter Property="Foreground" Value="#22C55E"/>
                                        </DataTrigger>
                                    </Style.Triggers>
                                </Style>
                            </TextBlock.Style>
                        </TextBlock>
                    </StackPanel>

                    <!-- Complete Button -->
                    <Button Grid.Column="1" Content="COMPLETE SALE" Width="160" Height="50"
                            Background="#22C55E" Foreground="White" FontSize="14" FontWeight="Bold"
                            IsEnabled="{Binding IsFullyPaid}"
                            Command="{Binding CompleteCommand}"/>
                </Grid>
            </Border>
        </Grid>
    </Border>
</Window>
```

#### 3.2 SplitPaymentDialogViewModel

```csharp
public partial class SplitPaymentDialogViewModel : ObservableObject
{
    public decimal OrderTotal { get; }

    [ObservableProperty]
    private ObservableCollection<PaymentEntry> _payments = new();

    [ObservableProperty]
    private string _paymentAmount = "";

    public decimal TotalPaid => Payments.Sum(p => p.Amount);
    public decimal RemainingBalance => Math.Max(0, OrderTotal - TotalPaid);
    public bool IsFullyPaid => RemainingBalance <= 0;

    public SplitPaymentDialogViewModel(decimal orderTotal)
    {
        OrderTotal = orderTotal;
        PaymentAmount = orderTotal.ToString("F2");
    }

    [RelayCommand]
    private void AddCashPayment()
    {
        if (decimal.TryParse(PaymentAmount, out var amount) && amount > 0)
        {
            Payments.Add(new PaymentEntry
            {
                Method = PaymentMethod.Cash,
                MethodName = "Cash",
                MethodIcon = "\uE8C7",
                MethodColor = "#22C55E",
                Amount = Math.Min(amount, RemainingBalance)
            });
            RefreshBalances();
        }
    }

    [RelayCommand]
    private void AddMpesaPayment()
    {
        if (decimal.TryParse(PaymentAmount, out var amount) && amount > 0)
        {
            Payments.Add(new PaymentEntry
            {
                Method = PaymentMethod.Mpesa,
                MethodName = "M-Pesa",
                MethodIcon = "\uE8EA",
                MethodColor = "#16A34A",
                Amount = Math.Min(amount, RemainingBalance)
            });
            RefreshBalances();
        }
    }

    [RelayCommand]
    private void AddCardPayment()
    {
        if (decimal.TryParse(PaymentAmount, out var amount) && amount > 0)
        {
            Payments.Add(new PaymentEntry
            {
                Method = PaymentMethod.Card,
                MethodName = "Card",
                MethodIcon = "\uE8C7",
                MethodColor = "#3B82F6",
                Amount = Math.Min(amount, RemainingBalance)
            });
            RefreshBalances();
        }
    }

    [RelayCommand]
    private void RemovePayment(PaymentEntry payment)
    {
        Payments.Remove(payment);
        RefreshBalances();
    }

    [RelayCommand]
    private void SetRemaining()
    {
        PaymentAmount = RemainingBalance.ToString("F2");
    }

    [RelayCommand]
    private void SetHalf()
    {
        PaymentAmount = (OrderTotal / 2).ToString("F2");
    }

    [RelayCommand]
    private void ClearAmount()
    {
        PaymentAmount = "";
    }

    private void RefreshBalances()
    {
        OnPropertyChanged(nameof(TotalPaid));
        OnPropertyChanged(nameof(RemainingBalance));
        OnPropertyChanged(nameof(IsFullyPaid));
        PaymentAmount = RemainingBalance.ToString("F2");
    }
}

public class PaymentEntry
{
    public PaymentMethod Method { get; set; }
    public string MethodName { get; set; } = "";
    public string MethodIcon { get; set; } = "";
    public string MethodColor { get; set; } = "";
    public decimal Amount { get; set; }
}
```

#### 3.3 Add Split Payment Button to POS

In `POSView.xaml`, payment section (after line 800):

```xml
<!-- Split Payment Button -->
<Button Background="#6366F1" Foreground="White" Height="44" Margin="0,8,0,0"
        FontSize="13" FontWeight="SemiBold" BorderThickness="0" Cursor="Hand"
        Command="{Binding OpenSplitPaymentCommand}" ToolTip="Split Payment">
    <Button.Template>
        <ControlTemplate TargetType="Button">
            <Border Background="{TemplateBinding Background}" CornerRadius="6">
                <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
            </Border>
        </ControlTemplate>
    </Button.Template>
    <StackPanel Orientation="Horizontal">
        <TextBlock Text="&#xE8AB;" FontFamily="Segoe MDL2 Assets" FontSize="14" Margin="0,0,8,0"/>
        <TextBlock Text="SPLIT PAYMENT" VerticalAlignment="Center"/>
    </StackPanel>
</Button>
```

### Testing Criteria

| Test Case | Expected Result |
|-----------|-----------------|
| Open split dialog | Shows order total, remaining = total |
| Add cash payment | Adds to list, updates remaining |
| Add full remaining | Button enables "Complete Sale" |
| Remove payment | Updates remaining balance |
| Overpay attempt | Caps at remaining amount |
| Complete with balance | Button disabled |
| Complete fully paid | Closes dialog, processes order |

---

## Feature 4: Dashboard Sparklines

**Priority:** Medium
**Estimated Effort:** 1-2 days
**Dependencies:** OxyPlot or LiveCharts NuGet package
**Files to Modify:**
- `Views/DashboardView.xaml`
- `ViewModels/DashboardViewModel.cs`
- `HospitalityPOS.WPF.csproj` (add NuGet)

### Implementation Overview

1. Add `OxyPlot.Wpf` NuGet package
2. Create reusable SparklineControl
3. Add trend data to DashboardViewModel
4. Integrate into KPI cards

### Quick NuGet Command

```bash
dotnet add src/HospitalityPOS.WPF/HospitalityPOS.WPF.csproj package OxyPlot.Wpf
```

### Sparkline Control Example

```xml
<oxy:PlotView Height="30" Width="100" Background="Transparent">
    <oxy:PlotView.Model>
        <oxy:PlotModel PlotAreaBorderColor="Transparent">
            <oxy:PlotModel.Axes>
                <oxy:LinearAxis Position="Bottom" IsAxisVisible="False"/>
                <oxy:LinearAxis Position="Left" IsAxisVisible="False"/>
            </oxy:PlotModel.Axes>
            <oxy:PlotModel.Series>
                <oxy:AreaSeries ItemsSource="{Binding HourlySalesData}"
                                Color="#22C55E" Fill="#3322C55E"/>
            </oxy:PlotModel.Series>
        </oxy:PlotModel>
    </oxy:PlotView.Model>
</oxy:PlotView>
```

---

## Feature 5: Denomination Counting (Close Day)

**Priority:** High
**Estimated Effort:** 1 day
**Files to Modify:**
- `Views/Dialogs/CloseWorkPeriodDialog.xaml`
- `Views/Dialogs/CloseWorkPeriodDialog.xaml.cs`

### Implementation

Add denomination grid to CloseWorkPeriodDialog:

```xml
<!-- Denomination Counting Grid -->
<Border Background="#1E1E2E" CornerRadius="6" Padding="12" Margin="0,8,0,0">
    <StackPanel>
        <Grid Margin="0,0,0,8">
            <TextBlock Text="CASH DENOMINATION COUNT" Style="{StaticResource LabelStyle}" FontWeight="SemiBold"/>
            <Button Content="Hide" HorizontalAlignment="Right" Background="Transparent"
                    Foreground="#6B7280" BorderThickness="0" Padding="8,2"
                    Click="ToggleDenominations_Click"/>
        </Grid>

        <Grid x:Name="DenominationGrid">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="80"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="80"/>
            </Grid.ColumnDefinitions>
            <Grid.RowDefinitions>
                <RowDefinition Height="36"/>
                <RowDefinition Height="36"/>
                <RowDefinition Height="36"/>
                <RowDefinition Height="36"/>
                <RowDefinition Height="36"/>
                <RowDefinition Height="36"/>
                <RowDefinition Height="36"/>
                <RowDefinition Height="36"/>
                <RowDefinition Height="36"/>
            </Grid.RowDefinitions>

            <!-- Headers -->
            <TextBlock Text="NOTE/COIN" Foreground="#6B7280" FontSize="10" FontWeight="Bold"/>
            <TextBlock Grid.Column="1" Text="COUNT" Foreground="#6B7280" FontSize="10" FontWeight="Bold" HorizontalAlignment="Center"/>
            <TextBlock Grid.Column="2" Text="TOTAL" Foreground="#6B7280" FontSize="10" FontWeight="Bold" HorizontalAlignment="Right"/>

            <!-- KSh 1000 -->
            <TextBlock Grid.Row="1" Text="KSh 1,000" Foreground="White" VerticalAlignment="Center"/>
            <TextBox Grid.Row="1" Grid.Column="1" x:Name="D1000Count" Text="0" TextChanged="DenominationChanged"
                     Background="#2D2D44" Foreground="White" BorderThickness="0" Width="60"
                     HorizontalAlignment="Center" TextAlignment="Center" FontSize="16"/>
            <TextBlock Grid.Row="1" Grid.Column="2" x:Name="D1000Total" Text="0" Foreground="#22C55E"
                       HorizontalAlignment="Right" VerticalAlignment="Center"/>

            <!-- KSh 500 -->
            <TextBlock Grid.Row="2" Text="KSh 500" Foreground="White" VerticalAlignment="Center"/>
            <TextBox Grid.Row="2" Grid.Column="1" x:Name="D500Count" Text="0" TextChanged="DenominationChanged"
                     Background="#2D2D44" Foreground="White" BorderThickness="0" Width="60"
                     HorizontalAlignment="Center" TextAlignment="Center" FontSize="16"/>
            <TextBlock Grid.Row="2" Grid.Column="2" x:Name="D500Total" Text="0" Foreground="#22C55E"
                       HorizontalAlignment="Right" VerticalAlignment="Center"/>

            <!-- KSh 200 -->
            <TextBlock Grid.Row="3" Text="KSh 200" Foreground="White" VerticalAlignment="Center"/>
            <TextBox Grid.Row="3" Grid.Column="1" x:Name="D200Count" Text="0" TextChanged="DenominationChanged"
                     Background="#2D2D44" Foreground="White" BorderThickness="0" Width="60"
                     HorizontalAlignment="Center" TextAlignment="Center" FontSize="16"/>
            <TextBlock Grid.Row="3" Grid.Column="2" x:Name="D200Total" Text="0" Foreground="#22C55E"
                       HorizontalAlignment="Right" VerticalAlignment="Center"/>

            <!-- KSh 100 -->
            <TextBlock Grid.Row="4" Text="KSh 100" Foreground="White" VerticalAlignment="Center"/>
            <TextBox Grid.Row="4" Grid.Column="1" x:Name="D100Count" Text="0" TextChanged="DenominationChanged"
                     Background="#2D2D44" Foreground="White" BorderThickness="0" Width="60"
                     HorizontalAlignment="Center" TextAlignment="Center" FontSize="16"/>
            <TextBlock Grid.Row="4" Grid.Column="2" x:Name="D100Total" Text="0" Foreground="#22C55E"
                       HorizontalAlignment="Right" VerticalAlignment="Center"/>

            <!-- KSh 50 -->
            <TextBlock Grid.Row="5" Text="KSh 50" Foreground="White" VerticalAlignment="Center"/>
            <TextBox Grid.Row="5" Grid.Column="1" x:Name="D50Count" Text="0" TextChanged="DenominationChanged"
                     Background="#2D2D44" Foreground="White" BorderThickness="0" Width="60"
                     HorizontalAlignment="Center" TextAlignment="Center" FontSize="16"/>
            <TextBlock Grid.Row="5" Grid.Column="2" x:Name="D50Total" Text="0" Foreground="#22C55E"
                       HorizontalAlignment="Right" VerticalAlignment="Center"/>

            <!-- KSh 20 -->
            <TextBlock Grid.Row="6" Text="KSh 20" Foreground="White" VerticalAlignment="Center"/>
            <TextBox Grid.Row="6" Grid.Column="1" x:Name="D20Count" Text="0" TextChanged="DenominationChanged"
                     Background="#2D2D44" Foreground="White" BorderThickness="0" Width="60"
                     HorizontalAlignment="Center" TextAlignment="Center" FontSize="16"/>
            <TextBlock Grid.Row="6" Grid.Column="2" x:Name="D20Total" Text="0" Foreground="#22C55E"
                       HorizontalAlignment="Right" VerticalAlignment="Center"/>

            <!-- KSh 10 -->
            <TextBlock Grid.Row="7" Text="KSh 10" Foreground="White" VerticalAlignment="Center"/>
            <TextBox Grid.Row="7" Grid.Column="1" x:Name="D10Count" Text="0" TextChanged="DenominationChanged"
                     Background="#2D2D44" Foreground="White" BorderThickness="0" Width="60"
                     HorizontalAlignment="Center" TextAlignment="Center" FontSize="16"/>
            <TextBlock Grid.Row="7" Grid.Column="2" x:Name="D10Total" Text="0" Foreground="#22C55E"
                       HorizontalAlignment="Right" VerticalAlignment="Center"/>

            <!-- Coins (5, 1) -->
            <TextBlock Grid.Row="8" Text="Coins" Foreground="White" VerticalAlignment="Center"/>
            <TextBox Grid.Row="8" Grid.Column="1" x:Name="CoinsCount" Text="0" TextChanged="DenominationChanged"
                     Background="#2D2D44" Foreground="White" BorderThickness="0" Width="60"
                     HorizontalAlignment="Center" TextAlignment="Center" FontSize="16"/>
            <TextBlock Grid.Row="8" Grid.Column="2" x:Name="CoinsTotal" Text="0" Foreground="#22C55E"
                       HorizontalAlignment="Right" VerticalAlignment="Center"/>
        </Grid>

        <!-- Grand Total -->
        <Border Background="#252538" CornerRadius="4" Padding="8" Margin="0,12,0,0">
            <Grid>
                <TextBlock Text="COUNTED TOTAL" Foreground="White" FontWeight="Bold"/>
                <TextBlock x:Name="DenominationGrandTotal" Text="KSh 0" Foreground="#22C55E"
                           FontSize="18" FontWeight="Bold" HorizontalAlignment="Right"/>
            </Grid>
        </Border>
    </StackPanel>
</Border>
```

### Code-Behind for Denomination Calculation

```csharp
private void DenominationChanged(object sender, TextChangedEventArgs e)
{
    RecalculateDenominations();
}

private void RecalculateDenominations()
{
    decimal total = 0;

    total += ParseAndUpdate(D1000Count, D1000Total, 1000);
    total += ParseAndUpdate(D500Count, D500Total, 500);
    total += ParseAndUpdate(D200Count, D200Total, 200);
    total += ParseAndUpdate(D100Count, D100Total, 100);
    total += ParseAndUpdate(D50Count, D50Total, 50);
    total += ParseAndUpdate(D20Count, D20Total, 20);
    total += ParseAndUpdate(D10Count, D10Total, 10);

    if (int.TryParse(CoinsCount.Text, out int coins))
    {
        CoinsTotal.Text = coins.ToString("N0");
        total += coins;
    }

    DenominationGrandTotal.Text = $"KSh {total:N0}";
    CashCountInput.Text = total.ToString("F2");
}

private decimal ParseAndUpdate(TextBox countBox, TextBlock totalBlock, decimal denomination)
{
    if (int.TryParse(countBox.Text, out int count) && count >= 0)
    {
        var subtotal = count * denomination;
        totalBlock.Text = subtotal.ToString("N0");
        return subtotal;
    }
    totalBlock.Text = "0";
    return 0;
}
```

---

## Summary Checklist

| Feature | Priority | Effort | Status |
|---------|----------|--------|--------|
| Real-Time Product Search | Critical | 2-3 days | Ready for implementation |
| Product Images in POS | High | 1 day | Ready for implementation |
| Split Payment | High | 3-4 days | Ready for implementation |
| Dashboard Sparklines | Medium | 1-2 days | Ready for implementation |
| Denomination Counting | High | 1 day | Ready for implementation |

---

**Next Steps:**
1. Start with Feature 1 (Real-Time Search) - highest impact
2. Follow with Feature 2 (Product Images) - quick win
3. Then Feature 3 (Split Payment) - customer-facing improvement
4. Features 4 & 5 can be done in parallel

**Document End**
