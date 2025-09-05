using COGLyricsScanner.ViewModels;
using COGLyricsScanner.Models;

namespace COGLyricsScanner.Views;

public partial class CollectionDetailPage : ContentPage
{
    private CollectionDetailPageViewModel _viewModel;

    public CollectionDetailPage(Collection collection)
    {
        InitializeComponent();
        _viewModel = new CollectionDetailPageViewModel(collection);
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