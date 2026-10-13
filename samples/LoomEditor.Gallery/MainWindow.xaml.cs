using Microsoft.UI.Xaml;

namespace LoomEditor.Gallery;

/// <summary>The main window: one editor showing a sample file.</summary>
public sealed partial class MainWindow : Window
{
    /// <summary>Creates the window and loads the sample text.</summary>
    public MainWindow()
    {
        InitializeComponent();
        Editor.Text = SampleText.HelloWorld;
    }
}
