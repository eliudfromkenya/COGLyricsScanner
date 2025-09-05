using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace COGLyricsScanner.Views;

public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
        InitializeComponent();
        
        // Start the splash sequence
        _ = StartSplashSequence();
    }
    
    private async Task StartSplashSequence()
    {
        // Wait for animations to complete (3.5 seconds total)
        await Task.Delay(3500);
        
        // Navigate to the main app
        await NavigateToMainApp();
    }
    
    private async Task NavigateToMainApp()
    {
        try
        {
            // Fade out animation before navigation
            await MainContent.FadeTo(0, 500, Easing.CubicIn);
            
            // Navigate to AppShell (main app)
            Application.Current.MainPage = new AppShell();
        }
        catch (Exception ex)
        {
            // Fallback navigation in case of error
            System.Diagnostics.Debug.WriteLine($"Splash navigation error: {ex.Message}");
            Application.Current.MainPage = new AppShell();
        }
    }
    
    protected override bool OnBackButtonPressed()
    {
        // Prevent back button during splash
        return true;
    }
}