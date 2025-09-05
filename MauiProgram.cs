using COGLyricsScanner.Services;
using COGLyricsScanner.ViewModels;
using COGLyricsScanner.Views;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Plugin.Maui.OCR;

namespace COGLyricsScanner;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseOcr()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register Services
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
        builder.Services.AddSingleton<Services.IOcrService, OcrService>();
        builder.Services.AddSingleton<IThemeService, ThemeService>();
        builder.Services.AddSingleton<IExportService, ExportService>();
        builder.Services.AddSingleton<ISettingsService, SettingsService>();

        // Register ViewModels
        builder.Services.AddTransient<ScanPageViewModel>();
        builder.Services.AddTransient<EditPageViewModel>();
        builder.Services.AddTransient<HomePageViewModel>();
        builder.Services.AddTransient<CollectionsPageViewModel>();
        builder.Services.AddTransient<StatisticsPageViewModel>();

        // Register Views
        builder.Services.AddTransient<ScanPage>();
        builder.Services.AddTransient<EditPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<CollectionsPage>();
        builder.Services.AddTransient<CollectionDetailPage>();
        builder.Services.AddTransient<StatisticsPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<AboutPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        
        // Initialize ServiceHelper
        ServiceHelper.Initialize(app.Services);
        
        return app;
    }
}