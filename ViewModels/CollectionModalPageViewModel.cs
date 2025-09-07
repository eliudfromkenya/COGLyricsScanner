using System.ComponentModel;
using System.Windows.Input;
using COGLyricsScanner.Models;
using COGLyricsScanner.Services;
using COGLyricsScanner.Helpers;

namespace COGLyricsScanner.ViewModels;

public class CollectionModalPageViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService = null!;
    private Collection? _originalCollection;
    private string _collectionName;
    private string _collectionDescription;
    private string _nameError;
    private bool _isEditMode;
    private int _hymnCount;
    private DateTime _createdAt;
    private DateTime? _updatedAt;
    private bool _canSave;

    public CollectionModalPageViewModel()
    {
        _databaseService = ServiceHelper.GetService<IDatabaseService>() ?? throw new InvalidOperationException("DatabaseService not found");
        InitializeCommands();
        InitializeForCreate();
    }

    public CollectionModalPageViewModel(Collection collection)
    {
        _databaseService = ServiceHelper.GetService<IDatabaseService>() ?? throw new InvalidOperationException("DatabaseService not found");
        _originalCollection = collection;
        InitializeCommands();
        InitializeForEdit(collection);
    }

    #region Properties

    public string CollectionName
    {
        get => _collectionName;
        set
        {
            if (SetProperty(ref _collectionName, value))
            {
                ValidateName();
                UpdateCanSave();
            }
        }
    }

    public string CollectionDescription
    {
        get => _collectionDescription;
        set => SetProperty(ref _collectionDescription, value);
    }

    public string NameError
    {
        get => _nameError;
        set => SetProperty(ref _nameError, value);
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    public int HymnCount
    {
        get => _hymnCount;
        set => SetProperty(ref _hymnCount, value);
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        set => SetProperty(ref _createdAt, value);
    }

    public DateTime? UpdatedAt
    {
        get => _updatedAt;
        set => SetProperty(ref _updatedAt, value);
    }

    public bool CanSave
    {
        get => _canSave;
        set => SetProperty(ref _canSave, value);
    }

    public string Subtitle => IsEditMode ? "Edit collection details" : "Create a new collection to organize your hymns";

    public string SaveButtonText => IsEditMode ? "Update" : "Create";

    #endregion

    #region Commands

    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;
    public ICommand DeleteCommand { get; private set; } = null!;
    public ICommand NavigateToCollectionsCommand { get; private set; } = null!;

    #endregion

    #region Initialization

    private void InitializeCommands()
    {
        SaveCommand = new Command(async () => await SaveCollectionAsync(), () => CanSave);
        CancelCommand = new Command(async () => await CancelAsync());
        DeleteCommand = new Command(async () => await DeleteCollectionAsync());
        NavigateToCollectionsCommand = new Command(async () => await NavigateToCollectionsAsync());
    }

    private void InitializeForCreate()
    {
        IsEditMode = false;
        Title = "New Collection";
        CollectionName = string.Empty;
        CollectionDescription = string.Empty;
        UpdateCanSave();
    }

    private async void InitializeForEdit(Collection collection)
    {
        IsEditMode = true;
        Title = "Edit Collection";
        CollectionName = collection.Name;
        CollectionDescription = collection.Description;
        CreatedAt = collection.CreatedDate;
        UpdatedAt = collection.ModifiedDate;
        
        // Load hymn count
        await LoadHymnCountAsync();
        UpdateCanSave();
    }

    #endregion

    #region Methods

    private async Task LoadHymnCountAsync()
    {
        if (_originalCollection != null)
        {
            try
            {
                var hymnCollections = await _databaseService.GetHymnCollectionsByCollectionIdAsync(_originalCollection.Id);
                HymnCount = hymnCollections?.Count ?? 0;
            }
            catch (Exception ex)
            {
                HandleError(ex, "Failed to load hymn count");
            }
        }
    }

    private void ValidateName()
    {
        NameError = string.Empty;
        
        if (string.IsNullOrWhiteSpace(CollectionName))
        {
            NameError = "Collection name is required";
        }
        else if (CollectionName.Length < 2)
        {
            NameError = "Collection name must be at least 2 characters";
        }
        else if (CollectionName.Length > 100)
        {
            NameError = "Collection name must be less than 100 characters";
        }
    }

    private void UpdateCanSave()
    {
        CanSave = !string.IsNullOrWhiteSpace(CollectionName) && 
                  string.IsNullOrEmpty(NameError) &&
                  (!IsEditMode || HasChanges());
        
        ((Command)SaveCommand).ChangeCanExecute();
    }

    private bool HasChanges()
    {
        if (_originalCollection == null) return true;
        
        return CollectionName?.Trim() != _originalCollection.Name ||
               CollectionDescription?.Trim() != _originalCollection.Description;
    }

    private async Task SaveCollectionAsync()
    {
        await ExecuteAsync(async () =>
        {
            ValidateName();
            if (!string.IsNullOrEmpty(NameError))
            {
                return;
            }

            if (IsEditMode)
            {
                await UpdateCollectionAsync();
            }
            else
            {
                await CreateCollectionAsync();
            }

            await CloseModalAsync();
        });
    }

    private async Task CreateCollectionAsync()
    {
        var collection = new Collection
        {
            Name = CollectionName.Trim(),
            Description = string.IsNullOrWhiteSpace(CollectionDescription) ? null : CollectionDescription.Trim(),
            CreatedDate = DateTime.Now,
            ModifiedDate = DateTime.Now
        };

        await _databaseService.SaveCollectionAsync(collection);
        await ShowSuccessAsync($"Collection '{collection.Name}' created successfully.");
    }

    private async Task UpdateCollectionAsync()
    {
        _originalCollection.Name = CollectionName.Trim();
        _originalCollection.Description = string.IsNullOrWhiteSpace(CollectionDescription) ? null : CollectionDescription.Trim();
        _originalCollection.ModifiedDate = DateTime.Now;

        await _databaseService.SaveCollectionAsync(_originalCollection);
        await ShowSuccessAsync($"Collection '{_originalCollection.Name}' updated successfully.");
    }

    private async Task DeleteCollectionAsync()
    {
        if (_originalCollection == null) return;

        var confirm = await Application.Current.MainPage.DisplayAlert(
            "Delete Collection",
            $"Are you sure you want to delete '{_originalCollection.Name}'? This will not delete the hymns, only remove them from this collection.",
            "Delete",
            "Cancel");

        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            // First remove all hymn-collection relationships
            var hymnCollections = await _databaseService.GetHymnCollectionsByCollectionIdAsync(_originalCollection.Id);
            foreach (var hymnCollection in hymnCollections)
            {
                await _databaseService.DeleteHymnCollectionAsync(hymnCollection.Id);
            }

            // Then delete the collection
            await _databaseService.DeleteCollectionAsync(_originalCollection.Id);
            
            await ShowSuccessAsync($"Collection '{_originalCollection.Name}' deleted successfully.");
            await CloseModalAsync();
        });
    }

    private async Task CancelAsync()
    {
        if (HasChanges())
        {
            var confirm = await Application.Current.MainPage.DisplayAlert(
                "Discard Changes",
                "You have unsaved changes. Are you sure you want to discard them?",
                "Discard",
                "Continue Editing");

            if (!confirm) return;
        }

        await CloseModalAsync();
    }

    private async Task CloseModalAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            HandleError(ex, "Failed to close modal");
        }
    }

    private async Task NavigateToCollectionsAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//collections");
        }
        catch (Exception ex)
        {
            HandleError(ex, "Failed to navigate to collections");
        }
    }

    #endregion

    #region BaseViewModel Overrides

    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync();
        
        if (IsEditMode)
        {
            await LoadHymnCountAsync();
        }
    }

    #endregion
}