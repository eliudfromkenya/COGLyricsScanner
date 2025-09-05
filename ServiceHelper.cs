namespace COGLyricsScanner;

public static class ServiceHelper
{
    public static IServiceProvider? Services { get; private set; }

    public static void Initialize(IServiceProvider serviceProvider)
    {
        Services = serviceProvider;
    }

    public static T? GetService<T>() where T : class
    {
        return Services?.GetService(typeof(T)) as T;
    }

    public static T GetRequiredService<T>() where T : class
    {
        if (Services == null)
            throw new InvalidOperationException("Services not initialized. Call Initialize first.");
            
        return (T)Services.GetRequiredService(typeof(T));
    }
}