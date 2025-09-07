using COGLyricsScanner.ViewModels;

namespace COGLyricsScanner.Views;

public partial class HymnViewPage : ContentPage
{
    public HymnViewPage(HymnViewPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}