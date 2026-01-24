using HospitalityPOS.WPF.Models;
using HospitalityPOS.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace HospitalityPOS.WPF.Controls;

/// <summary>
/// Custom canvas control for the label template designer.
/// Provides drag-and-drop, selection, and resizing functionality.
/// </summary>
public class LabelDesignCanvas : Canvas
{
    #region Dependency Properties

    public static readonly DependencyProperty ShowGridProperty =
        DependencyProperty.Register(nameof(ShowGrid), typeof(bool), typeof(LabelDesignCanvas),
            new PropertyMetadata(true, OnShowGridChanged));

    public static readonly DependencyProperty GridSizeProperty =
        DependencyProperty.Register(nameof(GridSize), typeof(int), typeof(LabelDesignCanvas),
            new PropertyMetadata(8, OnGridSizeChanged));

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(LabelTemplateDesignerViewModel), typeof(LabelDesignCanvas),
            new PropertyMetadata(null));

    public bool ShowGrid
    {
        get => (bool)GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    public int GridSize
    {
        get => (int)GetValue(GridSizeProperty);
        set => SetValue(GridSizeProperty, value);
    }

    public LabelTemplateDesignerViewModel? ViewModel
    {
        get => (LabelTemplateDesignerViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    #endregion

    private DrawingVisual? _gridVisual;
    private bool _isDragging;
    private bool _isResizing;
    private Point _dragStart;
    private LabelDesignElement? _dragElement;
    private ResizeHandle _resizeHandle;

    private enum ResizeHandle
    {
        None,
        TopLeft, Top, TopRight,
        Left, Right,
        BottomLeft, Bottom, BottomRight
    }

    public LabelDesignCanvas()
    {
        Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)); // White label background
        ClipToBounds = true;

        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RedrawGrid();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        RedrawGrid();
    }

    private static void OnShowGridChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LabelDesignCanvas canvas)
        {
            canvas.RedrawGrid();
        }
    }

    private static void OnGridSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LabelDesignCanvas canvas)
        {
            canvas.RedrawGrid();
        }
    }

    private void RedrawGrid()
    {
        // Remove existing grid visual
        if (_gridVisual != null)
        {
            RemoveVisualChild(_gridVisual);
            _gridVisual = null;
        }

        if (!ShowGrid || ActualWidth <= 0 || ActualHeight <= 0) return;

        _gridVisual = new DrawingVisual();
        using (var dc = _gridVisual.RenderOpen())
        {
            var pen = new Pen(new SolidColorBrush(Color.FromArgb(50, 128, 128, 128)), 0.5);
            pen.Freeze();

            // Draw vertical lines
            for (double x = 0; x <= ActualWidth; x += GridSize)
            {
                dc.DrawLine(pen, new Point(x, 0), new Point(x, ActualHeight));
            }

            // Draw horizontal lines
            for (double y = 0; y <= ActualHeight; y += GridSize)
            {
                dc.DrawLine(pen, new Point(0, y), new Point(ActualWidth, y));
            }
        }

        AddVisualChild(_gridVisual);
    }

    protected override int VisualChildrenCount =>
        base.VisualChildrenCount + (_gridVisual != null ? 1 : 0);

    protected override Visual GetVisualChild(int index)
    {
        if (_gridVisual != null && index == base.VisualChildrenCount)
        {
            return _gridVisual;
        }
        return base.GetVisualChild(index);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        var position = e.GetPosition(this);
        var element = FindElementAt(position);

        if (element != null)
        {
            // Check if clicking on resize handle
            _resizeHandle = GetResizeHandle(element, position);

            if (_resizeHandle != ResizeHandle.None)
            {
                _isResizing = true;
                _dragStart = position;
                _dragElement = element;
            }
            else
            {
                _isDragging = true;
                _dragStart = position;
                _dragElement = element;
            }

            ViewModel?.SelectElement(element);
        }
        else
        {
            // Clicked on empty canvas - deselect
            ViewModel?.SelectElement(null);
        }

        CaptureMouse();
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var position = e.GetPosition(this);

        if (_isDragging && _dragElement != null)
        {
            var deltaX = position.X - _dragStart.X;
            var deltaY = position.Y - _dragStart.Y;

            ViewModel?.MoveElement(_dragElement, deltaX, deltaY);
            _dragStart = position;

            InvalidateVisual();
        }
        else if (_isResizing && _dragElement != null)
        {
            ResizeWithHandle(_dragElement, position);
            InvalidateVisual();
        }
        else
        {
            // Update cursor based on position
            var element = FindElementAt(position);
            if (element != null && element == ViewModel?.SelectedElement)
            {
                var handle = GetResizeHandle(element, position);
                Cursor = GetResizeCursor(handle);
            }
            else
            {
                Cursor = Cursors.Arrow;
            }
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        _isDragging = false;
        _isResizing = false;
        _dragElement = null;
        _resizeHandle = ResizeHandle.None;

        ReleaseMouseCapture();
    }

    private LabelDesignElement? FindElementAt(Point position)
    {
        if (ViewModel == null) return null;

        // Search in reverse order (top elements first)
        foreach (var element in ViewModel.Elements.OrderByDescending(e => e.ZIndex))
        {
            var bounds = new Rect(element.X, element.Y, element.Width, element.Height);
            if (bounds.Contains(position))
            {
                return element;
            }
        }

        return null;
    }

    private ResizeHandle GetResizeHandle(LabelDesignElement element, Point position)
    {
        const double handleSize = 8;
        var bounds = new Rect(element.X, element.Y, element.Width, element.Height);

        // Check corners
        if (IsNear(position, new Point(bounds.Left, bounds.Top), handleSize))
            return ResizeHandle.TopLeft;
        if (IsNear(position, new Point(bounds.Right, bounds.Top), handleSize))
            return ResizeHandle.TopRight;
        if (IsNear(position, new Point(bounds.Left, bounds.Bottom), handleSize))
            return ResizeHandle.BottomLeft;
        if (IsNear(position, new Point(bounds.Right, bounds.Bottom), handleSize))
            return ResizeHandle.BottomRight;

        // Check edges
        if (IsNear(position.Y, bounds.Top, handleSize) && position.X > bounds.Left && position.X < bounds.Right)
            return ResizeHandle.Top;
        if (IsNear(position.Y, bounds.Bottom, handleSize) && position.X > bounds.Left && position.X < bounds.Right)
            return ResizeHandle.Bottom;
        if (IsNear(position.X, bounds.Left, handleSize) && position.Y > bounds.Top && position.Y < bounds.Bottom)
            return ResizeHandle.Left;
        if (IsNear(position.X, bounds.Right, handleSize) && position.Y > bounds.Top && position.Y < bounds.Bottom)
            return ResizeHandle.Right;

        return ResizeHandle.None;
    }

    private bool IsNear(Point a, Point b, double threshold) =>
        Math.Abs(a.X - b.X) <= threshold && Math.Abs(a.Y - b.Y) <= threshold;

    private bool IsNear(double a, double b, double threshold) =>
        Math.Abs(a - b) <= threshold;

    private Cursor GetResizeCursor(ResizeHandle handle) => handle switch
    {
        ResizeHandle.TopLeft or ResizeHandle.BottomRight => Cursors.SizeNWSE,
        ResizeHandle.TopRight or ResizeHandle.BottomLeft => Cursors.SizeNESW,
        ResizeHandle.Top or ResizeHandle.Bottom => Cursors.SizeNS,
        ResizeHandle.Left or ResizeHandle.Right => Cursors.SizeWE,
        _ => Cursors.Arrow
    };

    private void ResizeWithHandle(LabelDesignElement element, Point position)
    {
        var deltaX = position.X - _dragStart.X;
        var deltaY = position.Y - _dragStart.Y;
        _dragStart = position;

        var newWidth = element.Width;
        var newHeight = element.Height;
        var newX = element.X;
        var newY = element.Y;

        switch (_resizeHandle)
        {
            case ResizeHandle.TopLeft:
                newX += deltaX;
                newY += deltaY;
                newWidth -= deltaX;
                newHeight -= deltaY;
                break;
            case ResizeHandle.Top:
                newY += deltaY;
                newHeight -= deltaY;
                break;
            case ResizeHandle.TopRight:
                newY += deltaY;
                newWidth += deltaX;
                newHeight -= deltaY;
                break;
            case ResizeHandle.Left:
                newX += deltaX;
                newWidth -= deltaX;
                break;
            case ResizeHandle.Right:
                newWidth += deltaX;
                break;
            case ResizeHandle.BottomLeft:
                newX += deltaX;
                newWidth -= deltaX;
                newHeight += deltaY;
                break;
            case ResizeHandle.Bottom:
                newHeight += deltaY;
                break;
            case ResizeHandle.BottomRight:
                newWidth += deltaX;
                newHeight += deltaY;
                break;
        }

        // Apply minimum size constraints
        if (newWidth >= 10 && newHeight >= 10)
        {
            element.X = newX;
            element.Y = newY;
            ViewModel?.ResizeElement(element, newWidth, newHeight);
        }
    }
}

/// <summary>
/// Visual representation of a design element on the canvas.
/// </summary>
public class LabelElementControl : ContentControl
{
    public static readonly DependencyProperty ElementProperty =
        DependencyProperty.Register(nameof(Element), typeof(LabelDesignElement), typeof(LabelElementControl),
            new PropertyMetadata(null, OnElementChanged));

    public LabelDesignElement? Element
    {
        get => (LabelDesignElement?)GetValue(ElementProperty);
        set => SetValue(ElementProperty, value);
    }

    private static void OnElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LabelElementControl control)
        {
            control.UpdateVisual();
        }
    }

    public LabelElementControl()
    {
        SnapsToDevicePixels = true;
    }

    private void UpdateVisual()
    {
        if (Element == null) return;

        // Position on canvas
        Canvas.SetLeft(this, Element.X);
        Canvas.SetTop(this, Element.Y);
        Width = Element.Width;
        Height = Element.Height;
        Panel.SetZIndex(this, Element.ZIndex);

        // Create content based on element type
        Content = Element.ElementType switch
        {
            LabelElementType.Text => CreateTextVisual(),
            LabelElementType.Barcode => CreateBarcodeVisual(),
            LabelElementType.Price => CreatePriceVisual(),
            LabelElementType.QRCode => CreateQRCodeVisual(),
            LabelElementType.Image => CreateImageVisual(),
            LabelElementType.Box => CreateBoxVisual(),
            LabelElementType.Line => CreateLineVisual(),
            _ => null
        };
    }

    private UIElement CreateTextVisual()
    {
        var textBlock = new TextBlock
        {
            Text = Element!.Content,
            FontSize = Element.FontSize * 0.5, // Scale for display
            FontWeight = Element.IsBold ? FontWeights.Bold : FontWeights.Normal,
            Foreground = Brushes.Black,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        };

        textBlock.TextAlignment = Element.TextAlignment switch
        {
            Models.TextAlignment.Left => System.Windows.TextAlignment.Left,
            Models.TextAlignment.Center => System.Windows.TextAlignment.Center,
            Models.TextAlignment.Right => System.Windows.TextAlignment.Right,
            _ => System.Windows.TextAlignment.Left
        };

        return WrapWithBorder(textBlock);
    }

    private UIElement CreateBarcodeVisual()
    {
        // Simple barcode representation
        var grid = new Grid();

        // Barcode lines (simplified visual)
        var barcodePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Height = Element!.BarcodeHeight * 0.5
        };

        var random = new Random(Element.Content.GetHashCode());
        for (int i = 0; i < 30; i++)
        {
            var line = new Rectangle
            {
                Width = random.Next(1, 4),
                Fill = Brushes.Black,
                Margin = new Thickness(1, 0, 1, 0)
            };
            barcodePanel.Children.Add(line);
        }

        grid.Children.Add(barcodePanel);

        if (Element.ShowBarcodeText)
        {
            var textBlock = new TextBlock
            {
                Text = Element.Content,
                FontSize = 8,
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            grid.Children.Add(textBlock);
        }

        return WrapWithBorder(grid);
    }

    private UIElement CreatePriceVisual()
    {
        var textBlock = new TextBlock
        {
            Text = Element!.Content,
            FontSize = Element.FontSize * 0.6,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.Black,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        return WrapWithBorder(textBlock);
    }

    private UIElement CreateQRCodeVisual()
    {
        // Simple QR code representation
        var grid = new Grid { Background = Brushes.White };
        var size = Element!.QrSize * 10;

        var border = new Border
        {
            Width = size,
            Height = size,
            Background = Brushes.Black,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Add some white squares to represent QR pattern
        var innerGrid = new Grid();
        innerGrid.Children.Add(new Rectangle
        {
            Width = size * 0.3,
            Height = size * 0.3,
            Fill = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(2)
        });
        innerGrid.Children.Add(new Rectangle
        {
            Width = size * 0.3,
            Height = size * 0.3,
            Fill = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(2)
        });
        innerGrid.Children.Add(new Rectangle
        {
            Width = size * 0.3,
            Height = size * 0.3,
            Fill = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(2)
        });

        border.Child = innerGrid;
        grid.Children.Add(border);

        return WrapWithBorder(grid);
    }

    private UIElement CreateImageVisual()
    {
        var grid = new Grid { Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)) };
        var icon = new TextBlock
        {
            Text = "\uEB9F", // Image icon
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 24,
            Foreground = Brushes.Gray,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        grid.Children.Add(icon);

        return WrapWithBorder(grid);
    }

    private UIElement CreateBoxVisual()
    {
        return new Border
        {
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(Element!.LineThickness),
            Background = Brushes.Transparent
        };
    }

    private UIElement CreateLineVisual()
    {
        var line = new Rectangle
        {
            Fill = Brushes.Black
        };

        if (Element!.Width > Element.Height)
        {
            line.Height = Element.LineThickness;
        }
        else
        {
            line.Width = Element.LineThickness;
        }

        return line;
    }

    private UIElement WrapWithBorder(UIElement content)
    {
        var border = new Border
        {
            BorderBrush = Element!.IsSelected
                ? new SolidColorBrush(Color.FromRgb(59, 130, 246)) // Blue selection
                : new SolidColorBrush(Color.FromArgb(100, 128, 128, 128)), // Gray
            BorderThickness = new Thickness(Element.IsSelected ? 2 : 1),
            Background = Brushes.Transparent,
            Child = content
        };

        // Add resize handles if selected
        if (Element.IsSelected)
        {
            var grid = new Grid();
            grid.Children.Add(border);

            // Add corner handles
            AddResizeHandle(grid, HorizontalAlignment.Left, VerticalAlignment.Top);
            AddResizeHandle(grid, HorizontalAlignment.Right, VerticalAlignment.Top);
            AddResizeHandle(grid, HorizontalAlignment.Left, VerticalAlignment.Bottom);
            AddResizeHandle(grid, HorizontalAlignment.Right, VerticalAlignment.Bottom);

            return grid;
        }

        return border;
    }

    private void AddResizeHandle(Grid grid, HorizontalAlignment horizontal, VerticalAlignment vertical)
    {
        var handle = new Rectangle
        {
            Width = 8,
            Height = 8,
            Fill = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
            HorizontalAlignment = horizontal,
            VerticalAlignment = vertical,
            Margin = new Thickness(-4)
        };

        grid.Children.Add(handle);
    }
}
