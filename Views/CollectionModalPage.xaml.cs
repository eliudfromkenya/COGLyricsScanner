using COGLyricsScanner.ViewModels;
using COGLyricsScanner.Models;
using COGLyricsScanner.Services;
using COGLyricsScanner.Helpers;

namespace COGLyricsScanner.Views;

public partial class CollectionModalPage : ContentPage, IQueryAttributable
{
    private CollectionModalPageViewModel _viewModel;

    public CollectionModalPage()
    {
        InitializeComponent();
        // ViewModel will be set in ApplyQueryAttributes or OnAppearing
    }

    public CollectionModalPage(Collection collection)
    {
        InitializeComponent();
        _viewModel = new CollectionModalPageViewModel(collection);
        BindingContext = _viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("collectionId", out var collectionIdObj) && 
            int.TryParse(collectionIdObj.ToString(), out var collectionId))
        {
            // Load collection for editing
            LoadCollectionForEdit(collectionId);
        }
        else
        {
            // Create new collection
            _viewModel = new CollectionModalPageViewModel();
            BindingContext = _viewModel;
        }
    }

    private async void LoadCollectionForEdit(int collectionId)
    {
        try
        {
            var databaseService = ServiceHelper.GetService<IDatabaseService>();
            var collection = await databaseService.GetCollectionAsync(collectionId);
            
            if (collection != null)
            {
                _viewModel = new CollectionModalPageViewModel(collection);
                BindingContext = _viewModel;
            }
            else
            {
                await DisplayAlert("Error", "Collection not found", "OK");
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load collection: {ex.Message}", "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Initialize ViewModel if not already set (fallback for direct navigation)
        if (_viewModel == null)
        {
            _viewModel = new CollectionModalPageViewModel();
            BindingContext = _viewModel;
        }
        
        await _viewModel.OnAppearingAsync();
        
        // Focus on name entry for better UX
        if (!_viewModel.IsEditMode)
        {
            NameEntry.Focus();
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