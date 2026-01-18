using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.WPF.Controls;

/// <summary>
/// Control for displaying and managing the floor grid with tables.
/// </summary>
public partial class FloorGridControl : UserControl
{
    private const double CellSize = 60;
    private Point _dragStartPoint;
    private Table? _draggedTable;
    private bool _isDragging;

    #region Dependency Properties

    /// <summary>
    /// Identifies the Tables dependency property.
    /// </summary>
    public static readonly DependencyProperty TablesProperty =
        DependencyProperty.Register(nameof(Tables), typeof(ObservableCollection<Table>), typeof(FloorGridControl),
            new PropertyMetadata(null, OnTablesChanged));

    /// <summary>
    /// Gets or sets the tables collection.
    /// </summary>
    public ObservableCollection<Table> Tables
    {
        get => (ObservableCollection<Table>)GetValue(TablesProperty);
        set => SetValue(TablesProperty, value);
    }

    /// <summary>
    /// Identifies the Sections dependency property.
    /// </summary>
    public static readonly DependencyProperty SectionsProperty =
        DependencyProperty.Register(nameof(Sections), typeof(ObservableCollection<Section>), typeof(FloorGridControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the sections collection.
    /// </summary>
    public ObservableCollection<Section> Sections
    {
        get => (ObservableCollection<Section>)GetValue(SectionsProperty);
        set => SetValue(SectionsProperty, value);
    }

    /// <summary>
    /// Identifies the SelectedTable dependency property.
    /// </summary>
    public static readonly DependencyProperty SelectedTableProperty =
        DependencyProperty.Register(nameof(SelectedTable), typeof(Table), typeof(FloorGridControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>
    /// Gets or sets the selected table.
    /// </summary>
    public Table? SelectedTable
    {
        get => (Table?)GetValue(SelectedTableProperty);
        set => SetValue(SelectedTableProperty, value);
    }

    /// <summary>
    /// Identifies the GridWidth dependency property.
    /// </summary>
    public static readonly DependencyProperty GridWidthProperty =
        DependencyProperty.Register(nameof(GridWidth), typeof(int), typeof(FloorGridControl),
            new PropertyMetadata(10, OnGridSizeChanged));

    /// <summary>
    /// Gets or sets the grid width (columns).
    /// </summary>
    public int GridWidth
    {
        get => (int)GetValue(GridWidthProperty);
        set => SetValue(GridWidthProperty, value);
    }

    /// <summary>
    /// Identifies the GridHeight dependency property.
    /// </summary>
    public static readonly DependencyProperty GridHeightProperty =
        DependencyProperty.Register(nameof(GridHeight), typeof(int), typeof(FloorGridControl),
            new PropertyMetadata(10, OnGridSizeChanged));

    /// <summary>
    /// Gets or sets the grid height (rows).
    /// </summary>
    public int GridHeight
    {
        get => (int)GetValue(GridHeightProperty);
        set => SetValue(GridHeightProperty, value);
    }

    /// <summary>
    /// Gets whether there are no tables.
    /// </summary>
    public bool HasNoTables => Tables == null || Tables.Count == 0;

    #endregion

    #region Events

    /// <summary>
    /// Event raised when a table position changes.
    /// </summary>
    public event EventHandler<TablePositionChangedEventArgs>? TablePositionChanged;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="FloorGridControl"/> class.
    /// </summary>
    public FloorGridControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        DrawGrid();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        DrawGrid();
    }

    private static void OnTablesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FloorGridControl control)
        {
            control.OnPropertyChanged(e);
        }
    }

    private static void OnGridSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FloorGridControl control)
        {
            control.DrawGrid();
        }
    }

    private void DrawGrid()
    {
        // Remove old grid lines
        var linesToRemove = GridCanvas.Children.OfType<Line>().ToList();
        foreach (var line in linesToRemove)
        {
            GridCanvas.Children.Remove(line);
        }

        var width = ActualWidth > 0 ? ActualWidth : 600;
        var height = ActualHeight > 0 ? ActualHeight : 400;

        var cellWidth = width / GridWidth;
        var cellHeight = height / GridHeight;
        var cellSizeActual = Math.Min(cellWidth, cellHeight);

        // Draw vertical lines
        for (int i = 0; i <= GridWidth; i++)
        {
            var line = new Line
            {
                X1 = i * cellSizeActual,
                Y1 = 0,
                X2 = i * cellSizeActual,
                Y2 = GridHeight * cellSizeActual,
                Stroke = new SolidColorBrush(Color.FromRgb(60, 60, 90)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 4 }
            };
            GridCanvas.Children.Insert(0, line);
        }

        // Draw horizontal lines
        for (int i = 0; i <= GridHeight; i++)
        {
            var line = new Line
            {
                X1 = 0,
                Y1 = i * cellSizeActual,
                X2 = GridWidth * cellSizeActual,
                Y2 = i * cellSizeActual,
                Stroke = new SolidColorBrush(Color.FromRgb(60, 60, 90)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 4 }
            };
            GridCanvas.Children.Insert(0, line);
        }
    }

    private void Table_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is Table table)
        {
            SelectedTable = table;
            _dragStartPoint = e.GetPosition(GridCanvas);
            _draggedTable = table;
            _isDragging = false;
            border.CaptureMouse();
        }
    }

    private void Table_MouseMove(object sender, MouseEventArgs e)
    {
        if (_draggedTable == null || e.LeftButton != MouseButtonState.Pressed)
            return;

        var currentPoint = e.GetPosition(GridCanvas);
        var diff = currentPoint - _dragStartPoint;

        if (Math.Abs(diff.X) > 5 || Math.Abs(diff.Y) > 5)
        {
            _isDragging = true;
        }

        if (_isDragging && sender is Border border)
        {
            // Calculate cell size
            var cellWidth = ActualWidth / GridWidth;
            var cellHeight = ActualHeight / GridHeight;
            var cellSizeActual = Math.Min(cellWidth, cellHeight);

            // Calculate new grid position
            var newX = (int)(currentPoint.X / cellSizeActual);
            var newY = (int)(currentPoint.Y / cellSizeActual);

            // Clamp to grid bounds
            newX = Math.Max(0, Math.Min(newX, GridWidth - _draggedTable.Width));
            newY = Math.Max(0, Math.Min(newY, GridHeight - _draggedTable.Height));

            // Update visual position
            Canvas.SetLeft(border.Parent as UIElement ?? border, newX * cellSizeActual);
            Canvas.SetTop(border.Parent as UIElement ?? border, newY * cellSizeActual);
        }
    }

    private void Table_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_draggedTable != null && _isDragging)
        {
            var currentPoint = e.GetPosition(GridCanvas);

            // Calculate cell size
            var cellWidth = ActualWidth / GridWidth;
            var cellHeight = ActualHeight / GridHeight;
            var cellSizeActual = Math.Min(cellWidth, cellHeight);

            // Calculate new grid position
            var newX = (int)(currentPoint.X / cellSizeActual);
            var newY = (int)(currentPoint.Y / cellSizeActual);

            // Clamp to grid bounds
            newX = Math.Max(0, Math.Min(newX, GridWidth - _draggedTable.Width));
            newY = Math.Max(0, Math.Min(newY, GridHeight - _draggedTable.Height));

            // Only raise event if position changed
            if (newX != _draggedTable.GridX || newY != _draggedTable.GridY)
            {
                TablePositionChanged?.Invoke(this, new TablePositionChangedEventArgs
                {
                    TableId = _draggedTable.Id,
                    NewX = newX,
                    NewY = newY,
                    Width = _draggedTable.Width,
                    Height = _draggedTable.Height
                });

                // Update the table position
                _draggedTable.GridX = newX;
                _draggedTable.GridY = newY;
            }
        }

        if (sender is Border border)
        {
            border.ReleaseMouseCapture();
        }

        _draggedTable = null;
        _isDragging = false;
    }
}

/// <summary>
/// Event args for table position changes.
/// </summary>
public class TablePositionChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the table ID.
    /// </summary>
    public int TableId { get; set; }

    /// <summary>
    /// Gets or sets the new X position.
    /// </summary>
    public int NewX { get; set; }

    /// <summary>
    /// Gets or sets the new Y position.
    /// </summary>
    public int NewY { get; set; }

    /// <summary>
    /// Gets or sets the width.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    public int Height { get; set; }
}
