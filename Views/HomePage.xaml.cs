using COGLyricsScanner.ViewModels;
using Microsoft.Maui.Controls;

namespace COGLyricsScanner.Views;

public partial class HomePage : ContentPage
{
    private HomePageViewModel ViewModel => (HomePageViewModel)BindingContext;

    public HomePage(HomePageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.OnAppearingAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await ViewModel.OnDisappearingAsync();
    }
}