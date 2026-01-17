using System.Windows;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Interaction logic for OfferEditorDialog.xaml
/// </summary>
public partial class OfferEditorDialog : Window
{
    private readonly OfferEditorViewModel _viewModel;

    /// <summary>
    /// Gets the result offer from the dialog.
    /// </summary>
    public ProductOffer? Result { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OfferEditorDialog"/> class.
    /// </summary>
    public OfferEditorDialog(OfferEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = viewModel;

        viewModel.CloseRequested += OnCloseRequested;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
    }

    private void OnCloseRequested(object? sender, ProductOffer? result)
    {
        Result = result;
        DialogResult = result != null;
        Close();
    }
}
