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
}
}