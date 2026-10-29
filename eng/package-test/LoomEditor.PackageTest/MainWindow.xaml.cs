using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.PackageTest;

/// <summary>The only window of the package test app: one editor with a short C# text.</summary>
/// <remarks>Threading: UI thread only.</remarks>
public sealed partial class MainWindow : Window
{
    /// <summary>Creates the window.</summary>
    public MainWindow()
    {
        InitializeComponent();
        Editor.Text = "// LoomEditor package test\nclass Program { static void Main() { } }\n";
        // A missing style does not crash by itself: turn it into a crash log the smoke test sees.
        Editor.Loaded += (_, _) =>
        {
            if (VisualTreeHelper.GetChildrenCount(Editor) == 0)
            {
                throw new InvalidOperationException(
                    "The LoomEditor control template was not applied: Generic.xaml is missing from the package.");
            }
        };
    }
}
