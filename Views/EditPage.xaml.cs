using COGLyricsScanner.ViewModels;
using Microsoft.Maui.Controls;

namespace COGLyricsScanner.Views
{

public partial class EditPage : ContentPage
{
    private EditPageViewModel ViewModel => (EditPageViewModel)BindingContext;
    private bool _isUpdatingLineNumbers = false;

    public EditPage(EditPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.OnAppearingAsync();
        UpdateLineNumbers();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await ViewModel.OnDisappearingAsync();
    }

    private void OnLyricsTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isUpdatingLineNumbers)
        {
            UpdateLineNumbers();
        }
    }

    private void OnLyricsScrolled(object sender, ScrolledEventArgs e)
    {
        // Synchronize line numbers scroll with editor scroll
        if (ViewModel.ShowLineNumbers && !_isUpdatingLineNumbers)
        {
            _isUpdatingLineNumbers = true;
            LineNumbersScrollView.ScrollToAsync(0, e.ScrollY, false);
            _isUpdatingLineNumbers = false;
        }
    }

    private void UpdateLineNumbers()
    {
        if (!ViewModel.ShowLineNumbers)
            return;

        var text = LyricsEditor.Text ?? string.Empty;
        var lines = text.Split('\n');
        var lineCount = lines.Length;

        // Clear existing line numbers
        LineNumbersStack.Children.Clear();

        // Add line numbers
        for (int i = 1; i <= lineCount; i++)
        {
            var lineLabel = new Label
            {
                Text = i.ToString(),
                FontSize = ViewModel.FontSize * 0.8, // Slightly smaller than editor font
                TextColor = Color.FromArgb("#666666"),
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 0, 4, 0),
                LineHeight = 1.2
            };

            LineNumbersStack.Children.Add(lineLabel);
        }
    }

    // Handle property changes that affect line numbers
    protected override void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        
        if (propertyName == nameof(ViewModel.ShowLineNumbers))
        {
            UpdateLineNumbers();
        }
        else if (propertyName == nameof(ViewModel.FontSize))
        {
            UpdateLineNumbers();
        }
    }
}
}