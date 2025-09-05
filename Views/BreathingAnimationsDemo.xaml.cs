using Microsoft.Maui.Controls;

namespace COGLyricsScanner.Views;

public partial class BreathingAnimationsDemo : ContentPage
{
    public BreathingAnimationsDemo()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Optional: Auto-enable breathing animations after a short delay
        Dispatcher.StartTimer(TimeSpan.FromSeconds(2), () =>
        {
            if (BreathingToggle != null)
            {
                BreathingToggle.IsToggled = true;
            }
            return false; // Don't repeat
        });
    }
}