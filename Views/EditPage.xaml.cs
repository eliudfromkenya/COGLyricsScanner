using COGLyricsScanner.ViewModels;
using Microsoft.Maui.Controls;

namespace COGLyricsScanner.Views
{

public partial class EditPage : ContentPage, IQueryAttributable
{
    private EditPageViewModel ViewModel => (EditPageViewModel)BindingContext;
    private bool _isUpdatingLineNumbers = false;

    public EditPage()
    {
        InitializeComponent();
        BindingContext = new EditPageViewModel();
    }

    public EditPage(EditPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("hymnId", out var hymnIdObj) && 
            int.TryParse(hymnIdObj.ToString(), out var hymnId))
        {
            if (BindingContext is EditPageViewModel viewModel)
            {
                _ = Task.Run(async () => await viewModel.LoadHymnAsync(hymnId));
            }
        }
        
        if (query.TryGetValue("collectionId", out var collectionIdObj) && 
            int.TryParse(collectionIdObj.ToString(), out var collectionId))
        {
            if (BindingContext is EditPageViewModel viewModel)
            {
                viewModel.SetDefaultCollection(collectionId);
            }
        }
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