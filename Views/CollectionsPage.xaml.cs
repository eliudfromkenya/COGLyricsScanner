using COGLyricsScanner.ViewModels;
using COGLyricsScanner.Models;

namespace COGLyricsScanner.Views
{

public partial class CollectionsPage : ContentPage
{
    private CollectionsPageViewModel _viewModel;

    public CollectionsPage()
    {
        InitializeComponent();
        _viewModel = new CollectionsPageViewModel();
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

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel.SearchText = e.NewTextValue;
    }

    private async void OnAddCollectionClicked(object sender, EventArgs e)
    {
            try
            {
                var pg = new CollectionModalPage();
                await Shell.Current.Navigation.PushModalAsync(pg);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to create collection: {ex.Message}", "OK");
            }
            
    }

    private async void OnCollectionTapped(object sender, TappedEventArgs e)
    {
        try
        {
            if (e.Parameter is Collection collection)
            {
                // The sender should be the Border element from the TapGestureRecognizer
                if (sender is Border border)
                {
                    // Trigger selection visual state
                    VisualStateManager.GoToState(border, "Selected");
                    
                    // Add a subtle scale animation
                    await border.ScaleTo(0.95, 100, Easing.CubicOut);
                    await Task.Delay(50); // Brief pause to show selection
                    await border.ScaleTo(1.0, 100, Easing.CubicOut);
                    
                    // Reset to normal state
                    VisualStateManager.GoToState(border, "Normal");
                }
                    var pg = new CollectionDetailPage(collection);
                   await Shell.Current.Navigation.PushModalAsync(pg);

               // await _viewModel.NavigateToCollectionAsync(collection);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to navigate to collection: {ex.Message}", "OK");
        }
    }

    private async void OnCollectionMenuClicked(object sender, EventArgs e)
    {
        try
        {
            if (sender is Button button && button.CommandParameter is Collection collection)
            {
                var action = await DisplayActionSheet(
                    collection.Name,
                    "Cancel",
                    null,
                    "View Details",
                    "Edit",
                    "Export",
                    "Delete");

                switch (action)
                {
                    case "View Details":
                        await _viewModel.NavigateToCollectionAsync(collection);
                        break;
                    case "Edit":
                        await EditCollection(collection);
                        break;
                    case "Export":
                        await _viewModel.ExportCollectionAsync(collection);
                        break;
                    case "Delete":
                        await DeleteCollection(collection);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Action failed: {ex.Message}", "OK");
        }
    }

    private async Task EditCollection(Collection collection)
    {
        try
        {
            await Shell.Current.GoToAsync($"collection-modal?collectionId={collection.Id}");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to open edit modal: {ex.Message}", "OK");
        }
    }

    private async Task DeleteCollection(Collection collection)
    {
        try
        {
            var confirm = await DisplayAlert(
                "Delete Collection",
                $"Are you sure you want to delete '{collection.Name}'? This will not delete the hymns, only remove them from this collection.",
                "Delete",
                "Cancel");

            if (confirm)
            {
                await _viewModel.DeleteCollectionAsync(collection);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to delete collection: {ex.Message}", "OK");
        }
    }

    private async void OnEditCollectionClicked(object sender, EventArgs e)
    {
        try
        {
            if (sender is Button button && button.CommandParameter is Collection collection)
            {
                await EditCollection(collection);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to edit collection: {ex.Message}", "OK");
        }
    }

    private async void OnDeleteCollectionClicked(object sender, EventArgs e)
    {
        try
        {
            if (sender is Button button && button.CommandParameter is Collection collection)
            {
                await DeleteCollection(collection);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to delete collection: {ex.Message}", "OK");
        }
    }
}
}