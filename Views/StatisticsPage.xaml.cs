using COGLyricsScanner.ViewModels;

namespace COGLyricsScanner.Views
{

public partial class StatisticsPage : ContentPage
{
    private StatisticsPageViewModel _viewModel;

    public StatisticsPage()
    {
        InitializeComponent();
        _viewModel = new StatisticsPageViewModel();
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await _viewModel.OnDisappearingAsync();
    }
}
}