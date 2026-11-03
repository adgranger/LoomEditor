using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;

namespace LoomEditor.Avalonia.Tests.TestHelpers;

/// <summary>The application of the headless tests: Fluent and the LoomEditor styles, drawn by Skia.</summary>
public sealed class TestApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add(new LoomEditorStyles());
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
