using Avalonia;

namespace LoomEditor.Gallery;

/// <summary>Entry point of the Avalonia gallery app.</summary>
internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        // The UI Automation self-check of the WinUI Gallery is a Windows-only client: there is nothing to run here.
        if (args.Contains("--uia-selfcheck", StringComparer.Ordinal))
        {
            Console.WriteLine(UiaNotAvailable);
            return 0;
        }

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>The text of the "UIA self-check" button and flag.</summary>
    public const string UiaNotAvailable = "uia self-check: not available on Avalonia";

    /// <summary>Configures the app (also the entry point of design-time tools).</summary>
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect();
}
