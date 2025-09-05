namespace COGLyricsScanner.Helpers;

public static class ServiceHelper
{
    public static IServiceProvider Services { get; private set; } = null!;

    public static void Initialize(IServiceProvider serviceProvider)
    {
        Services = serviceProvider;
    }

    public static T GetService<T>() where T : class
    {
        return Services.GetService(typeof(T)) as T
            ?? throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within MauiProgram.cs.");
    }

    public static T GetRequiredService<T>() where T : class
    {
        return Services.GetRequiredService<T>();
    }
}