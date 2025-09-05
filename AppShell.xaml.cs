using COGLyricsScanner.Views;

namespace COGLyricsScanner;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        // Register routes for navigation
        RegisterRoutes();
        
        // Set up shell events
        SetupShellEvents();
    }

    private void RegisterRoutes()
    {
        // Register additional routes that are not part of the main tab bar
        Routing.RegisterRoute("settings", typeof(SettingsPage));
        Routing.RegisterRoute("collections", typeof(CollectionsPage));
        Routing.RegisterRoute("statistics", typeof(StatisticsPage));
        Routing.RegisterRoute("about", typeof(AboutPage));
        
        // Register detail pages
        Routing.RegisterRoute("hymn-detail", typeof(HymnDetailPage));
        Routing.RegisterRoute("collection-detail", typeof(CollectionDetailPage));
        Routing.RegisterRoute("export", typeof(ExportPage));
        Routing.RegisterRoute("backup", typeof(BackupPage));
    }

    private void SetupShellEvents()
    {
        // Handle navigation events
        Navigating += OnShellNavigating;
        Navigated += OnShellNavigated;
    }

    private async void OnShellNavigating(object? sender, ShellNavigatingEventArgs e)
    {
        // Handle any pre-navigation logic here
        // For example, you could show loading indicators or validate navigation
        
        // Example: Show loading for certain routes
        if (e.Target.Location.OriginalString.Contains("scan") ||
            e.Target.Location.OriginalString.Contains("edit"))
        {
            // Could show a loading indicator here
        }
    }

    private async void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        // Handle post-navigation logic here
        // For example, you could hide loading indicators or update analytics
        
        // Example: Track navigation for analytics
        var route = e.Current.Location.OriginalString;
        System.Diagnostics.Debug.WriteLine($"Navigated to: {route}");
        
        // Update the current page context if needed
        await UpdateCurrentPageContext(route);
    }

    private async Task UpdateCurrentPageContext(string route)
    {
        // This method can be used to update context or perform actions
        // based on the current page
        
        try
        {
            switch (route.ToLower())
            {
                case var r when r.Contains("scan"):
                    // Handle scan page context
                    break;
                case var r when r.Contains("edit"):
                    // Handle edit page context
                    break;
                case var r when r.Contains("home"):
                    // Handle home page context
                    break;
                default:
                    // Handle other pages
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating page context: {ex.Message}");
        }
        
        await Task.CompletedTask;
    }

    // Method to programmatically navigate to specific routes
    public static async Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null)
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
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
        }
    }

    // Method to go back in navigation stack
    public static async Task GoBackAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Go back error: {ex.Message}");
        }
    }

    // Method to navigate to root
    public static async Task NavigateToRootAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//home");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigate to root error: {ex.Message}");
        }
    }

    // Method to show flyout programmatically
    public static void ShowFlyout()
    {
        Shell.Current.FlyoutIsPresented = true;
    }

    // Method to hide flyout programmatically
    public static void HideFlyout()
    {
        Shell.Current.FlyoutIsPresented = false;
    }
}