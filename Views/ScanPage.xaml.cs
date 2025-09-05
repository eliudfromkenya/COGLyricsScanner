using COGLyricsScanner.ViewModels;

namespace COGLyricsScanner.Views
{

public partial class ScanPage : ContentPage
{
    public ScanPage(ScanPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is ScanPageViewModel viewModel)
        {
            await viewModel.OnAppearingAsync();
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        
        if (BindingContext is ScanPageViewModel viewModel)
        {
            await viewModel.OnDisappearingAsync();
        }
    }
}
}