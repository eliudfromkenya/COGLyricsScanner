using COGLyricsScanner.ViewModels;
using COGLyricsScanner.Models;

namespace COGLyricsScanner.Views
{

public partial class CollectionDetailPage : ContentPage, IQueryAttributable
{
    private CollectionDetailPageViewModel _viewModel = null!;

    public CollectionDetailPage()
    {
        InitializeComponent();
    }

    public CollectionDetailPage(Collection collection)
    {
        InitializeComponent();
        _viewModel = new CollectionDetailPageViewModel(collection);
        BindingContext = _viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("collectionId", out var collectionIdObj) && 
            int.TryParse(collectionIdObj.ToString(), out var collectionId))
        {
            _viewModel = new CollectionDetailPageViewModel(collectionId);
            BindingContext = _viewModel;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel != null)
        {
            await _viewModel.OnAppearingAsync();
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        if (_viewModel != null)
        {
            await _viewModel.OnDisappearingAsync();
        }
    }

    private async void OnItemTapped(object sender, TappedEventArgs e)
    {
        if (sender is Border border)
        {
            // Trigger pressed animation
            await border.ScaleTo(0.95, 100, Easing.CubicOut);
            await border.FadeTo(0.8, 50);
            
            // Return to normal state
            await Task.WhenAll(
                border.ScaleTo(1.0, 150, Easing.CubicOut),
                border.FadeTo(1.0, 100)
            );
            
            // Execute the ViewHymnCommand
            if (_viewModel?.ViewHymnCommand?.CanExecute(border.BindingContext) == true)
            {
                _viewModel.ViewHymnCommand.Execute(border.BindingContext);
            }
        }
    }
}
}