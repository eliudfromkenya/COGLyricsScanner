using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace COGLyricsScanner.ViewModels;

public partial class BaseViewModel : ObservableObject, IDisposable
{
    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string loadingMessage = "Loading...";

    [ObservableProperty]
    private bool isEmpty;

    [ObservableProperty]
    private string emptyMessage = "No data available";

    // Navigation helper
    protected async Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        try
        {
            if (parameters != null && parameters.Any())
            {
                await Shell.Current.GoToAsync(route, parameters);
            }
            else
            {
                await Shell.Current.GoToAsync(route);
            }
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Navigation failed");
        }
    }

    // Go back helper
    protected async Task GoBackAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Unable to go back");
        }
    }

    // Error handling
    protected async Task HandleErrorAsync(Exception ex, string userMessage = "An error occurred")
    {
        System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
        
        ErrorMessage = userMessage;
        HasError = true;
        
        // Show error to user
        await ShowErrorAsync(userMessage);
    }

    // Synchronous error handling
    protected void HandleError(Exception ex, string userMessage = "An error occurred")
    {
        System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
        
        ErrorMessage = userMessage;
        HasError = true;
        
        // Note: Cannot show alert synchronously, only log
    }

    // Show error message to user
    protected virtual async Task ShowErrorAsync(string message)
    {
        try
        {
            await Application.Current?.MainPage?.DisplayAlert("Error", message, "OK")!;
        }
        catch
        {
            // Fallback if DisplayAlert fails
            System.Diagnostics.Debug.WriteLine($"Failed to show error: {message}");
        }
    }

    // Show success message to user
    protected virtual async Task ShowSuccessAsync(string message)
    {
        try
        {
            await Application.Current?.MainPage?.DisplayAlert("Success", message, "OK")!;
        }
        catch
        {
            // Fallback if DisplayAlert fails
            System.Diagnostics.Debug.WriteLine($"Success: {message}");
        }
    }

    // Show generic message to user
    protected virtual async Task ShowMessageAsync(string title, string message)
    {
        try
        {
            await Application.Current?.MainPage?.DisplayAlert(title, message, "OK")!;
        }
        catch
        {
            // Fallback if DisplayAlert fails
            System.Diagnostics.Debug.WriteLine($"{title}: {message}");
        }
    }

    // Show confirmation dialog
    protected virtual async Task<bool> ShowConfirmationAsync(string title, string message, string accept = "Yes", string cancel = "No")
    {
        try
        {
            return await Application.Current?.MainPage?.DisplayAlert(title, message, accept, cancel)!;
        }
        catch
        {
            // Fallback if DisplayAlert fails
            System.Diagnostics.Debug.WriteLine($"Confirmation dialog failed: {message}");
            return false;
        }
    }

    // Show action sheet
    protected virtual async Task<string?> ShowActionSheetAsync(string title, string cancel, string? destruction = null, params string[] buttons)
    {
        try
        {
            return await Application.Current?.MainPage?.DisplayActionSheet(title, cancel, destruction, buttons)!;
        }
        catch
        {
            // Fallback if DisplayActionSheet fails
            System.Diagnostics.Debug.WriteLine($"Action sheet failed: {title}");
            return null;
        }
    }

    // Execute with busy indicator
    protected async Task ExecuteWithBusyAsync(Func<Task> operation, string? loadingMessage = null)
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            IsLoading = true;
            HasError = false;
            
            if (!string.IsNullOrEmpty(loadingMessage))
                LoadingMessage = loadingMessage;

            await operation();
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex);
        }
        finally
        {
            IsBusy = false;
            IsLoading = false;
        }
    }

    // Execute with busy indicator and return result
    protected async Task<T?> ExecuteWithBusyAsync<T>(Func<Task<T>> operation, string? loadingMessage = null)
    {
        if (IsBusy)
            return default;

        try
        {
            IsBusy = true;
            IsLoading = true;
            HasError = false;
            
            if (!string.IsNullOrEmpty(loadingMessage))
                LoadingMessage = loadingMessage;

            return await operation();
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex);
            return default;
        }
        finally
        {
            IsBusy = false;
            IsLoading = false;
        }
    }

    // Execute async operation with error handling
    protected async Task ExecuteAsync(Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex);
        }
    }

    // Clear error state
    [RelayCommand]
    protected virtual void ClearError()
    {
        HasError = false;
        ErrorMessage = string.Empty;
    }

    // Refresh command - to be overridden by derived classes
    [RelayCommand]
    protected virtual async Task RefreshAsync()
    {
        if (IsRefreshing)
            return;

        try
        {
            IsRefreshing = true;
            await OnRefreshAsync();
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Failed to refresh");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    // Override this method in derived classes to implement refresh logic
    protected virtual async Task OnRefreshAsync()
    {
        await Task.CompletedTask;
    }

    // Lifecycle methods - can be overridden by derived classes
    public virtual async Task OnAppearingAsync()
    {
        await Task.CompletedTask;
    }

    public virtual async Task OnDisappearingAsync()
    {
        await Task.CompletedTask;
    }

    // Validation helper
    protected bool ValidateNotEmpty(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            ErrorMessage = $"{fieldName} is required";
            HasError = true;
            return false;
        }
        return true;
    }

    // Validation helper for collections
    protected bool ValidateNotEmpty<T>(IEnumerable<T>? collection, string fieldName)
    {
        if (collection == null || !collection.Any())
        {
            ErrorMessage = $"{fieldName} cannot be empty";
            HasError = true;
            return false;
        }
        return true;
    }

    // IDisposable implementation
    private bool _disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources here
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}