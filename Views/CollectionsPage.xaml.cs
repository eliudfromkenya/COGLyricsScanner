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
            var name = await DisplayPromptAsync("New Collection", "Enter collection name:", "Create", "Cancel", "My Collection");
            if (!string.IsNullOrWhiteSpace(name))
            {
                var description = await DisplayPromptAsync("Collection Description", "Enter description (optional):", "Create", "Cancel", "");
                await _viewModel.CreateCollectionAsync(name.Trim(), description?.Trim());
            }
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
                await _viewModel.NavigateToCollectionAsync(collection);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to open collection: {ex.Message}", "OK");
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
            var name = await DisplayPromptAsync("Edit Collection", "Collection name:", "Save", "Cancel", collection.Name);
            if (!string.IsNullOrWhiteSpace(name) && name != collection.Name)
            {
                var description = await DisplayPromptAsync("Edit Description", "Description (optional):", "Save", "Cancel", collection.Description ?? "");
                await _viewModel.UpdateCollectionAsync(collection, name.Trim(), description?.Trim());
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to edit collection: {ex.Message}", "OK");
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
}
}