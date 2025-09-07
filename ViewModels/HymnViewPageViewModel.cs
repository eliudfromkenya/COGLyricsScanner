using COGLyricsScanner.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using COGLyricsScanner.Helpers;
using COGLyricsScanner.Services;
using System.IO;
using System.Linq;

namespace COGLyricsScanner.ViewModels;

public partial class HymnViewPageViewModel : BaseViewModel, IQueryAttributable
{
    [ObservableProperty]
    private Hymn hymn;

    [ObservableProperty]
    private bool hasMetadata;

    public HymnViewPageViewModel()
    {
        Title = "Hymn View";
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.ContainsKey("hymn") && query["hymn"] is Hymn selectedHymn)
        {
            Hymn = selectedHymn;
            Title = Hymn.Title;
            UpdateMetadataVisibility();
        }
    }

    partial void OnHymnChanged(Hymn value)
    {
        UpdateMetadataVisibility();
    }

    private void UpdateMetadataVisibility()
    {
        if (Hymn != null)
        {
            HasMetadata = !string.IsNullOrEmpty(Hymn.Tags) ||
                         !string.IsNullOrEmpty(Hymn.Notes) ||
                         Hymn.CreatedDate != default ||
                         Hymn.ModifiedDate != default ||
                         Hymn.ViewCount > 0;
        }
        else
        {
            HasMetadata = false;
        }
    }

    [RelayCommand]
    private async Task EditHymn()
    {
        if (Hymn == null) return;

        try
        {
            var navigationParameter = new Dictionary<string, object>
            {
                { "hymnId", Hymn.Id },
                { "hymnBookId", Hymn.HymnBookId },
                { "collectionId", 0 } // Default to 0 if no specific collection context
            };

            await Shell.Current.GoToAsync("//edit", navigationParameter);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to navigate to edit page: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task NavigateToCollection()
    {
       try
        {
             if (Hymn == null)
                {
                    // If no hymn, navigate to collections page
                    await Shell.Current.GoToAsync("//Collections");
                    return;
                }

        
            // Get collections for this hymn through the junction table
            var databaseService = ServiceHelper.GetService<Services.IDatabaseService>();
            var collections = await databaseService.GetCollectionsByHymnIdAsync(Hymn.Id);
            if (collections?.Any() == true)
            {
                // Navigate to the first collection this hymn belongs to
                var firstCollection = collections.First();
                await Shell.Current.GoToAsync($"//collections/collection-detail?collectionId={firstCollection.Id}");
                return;
            }
            
            // Fallback to collections page if no collection found
            await Shell.Current.GoToAsync("//collections");
        }
        catch (Exception ex)
        {
            // Fallback to collections page on error
            await Shell.Current.GoToAsync("//collections");
        }
    }

    [RelayCommand]
    private async Task ShowMoreOptions()
    {
        if (Hymn == null) return;

        try
        {
            var action = await Shell.Current.DisplayActionSheet(
                "Options",
                "Cancel",
                null,
                "Share",
                "Add to Favorites",
                "Export",
                "Delete");

            switch (action)
            {
                case "Share":
                    await ShareHymn();
                    break;
                case "Add to Favorites":
                    await ToggleFavorite();
                    break;
                case "Export":
                    await ExportHymn();
                    break;
                case "Delete":
                    await DeleteHymn();
                    break;
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to show options: {ex.Message}", "OK");
        }
    }

    private async Task ShareHymn()
    {
        try
        {
            var shareText = $"{Hymn.Title}\n\n{Hymn.Lyrics}";

            await Share.RequestAsync(new ShareTextRequest
            {
                Text = shareText,
                Title = Hymn.Title
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to share hymn: {ex.Message}", "OK");
        }
    }

    private async Task ToggleFavorite()
    {
        try
        {
            Hymn.IsFavorite = !Hymn.IsFavorite;
            
            var databaseService = ServiceHelper.GetService<Services.IDatabaseService>();
            await databaseService.UpdateHymnAsync(Hymn);

            var message = Hymn.IsFavorite ? "Added to favorites" : "Removed from favorites";
            await Shell.Current.DisplayAlert("Success", message, "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to update favorite status: {ex.Message}", "OK");
        }
    }

    private async Task ExportHymn()
    {
        try
        {
            var exportService = ServiceHelper.GetService<Services.IExportService>();
            var hymns = new List<Hymn> { Hymn };
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{Hymn.Title}.txt");
            await exportService.ExportHymnsAsync(hymns, ExportFormat.TXT, filePath, true);
            
            await Shell.Current.DisplayAlert("Success", "Hymn exported successfully", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to export hymn: {ex.Message}", "OK");
        }
    }

    private async Task DeleteHymn()
    {
        try
        {
            var confirm = await Shell.Current.DisplayAlert(
                "Delete Hymn",
                $"Are you sure you want to delete '{Hymn.Title}'? This action cannot be undone.",
                "Delete",
                "Cancel");

            if (confirm)
            {
                var databaseService = ServiceHelper.GetService<Services.IDatabaseService>();
                await databaseService.DeleteHymnAsync(Hymn.Id);
                
                await Shell.Current.DisplayAlert("Success", "Hymn deleted successfully", "OK");
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to delete hymn: {ex.Message}", "OK");
        }
    }
}