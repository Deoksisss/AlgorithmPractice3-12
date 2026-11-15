using Avalonia;

namespace DataStructuresVisualizer;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
        // Linux desktops need a TrueType fallback with Cyrillic coverage.
        if (OperatingSystem.IsLinux())
            builder.With(new Avalonia.Media.FontManagerOptions { DefaultFamilyName = "DejaVu Sans" });
        return builder;
    }
}
