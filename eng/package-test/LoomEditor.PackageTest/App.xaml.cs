using Microsoft.UI.Xaml;

namespace LoomEditor.PackageTest;

/// <summary>Entry point of the package test app.</summary>
public partial class App : Application
{
    private Window? _window;

    /// <summary>Creates the app.</summary>
    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
    }

    /// <summary>Path of the crash log: %TEMP%\LoomEditor.PackageTest.log.</summary>
    public static string CrashLogPath { get; } = Path.Combine(Path.GetTempPath(), "LoomEditor.PackageTest.log");

    /// <inheritdoc />
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }

    // A Native AOT app has no debugger attached by default: write crashes to a file so they can be diagnosed.
    private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        File.AppendAllText(CrashLogPath, $"{DateTime.Now:O} {e.Message}{Environment.NewLine}{e.Exception}{Environment.NewLine}");
    }
}
