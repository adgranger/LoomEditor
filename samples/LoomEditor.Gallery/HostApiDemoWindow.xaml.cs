// The host's point of view: this window and its partial files use public members only, no internal member of any LoomEditor assembly.
using LoomEditor.TextMate.Grammars;
using LoomEditor.Themes;
using Microsoft.UI.Xaml;

namespace LoomEditor.Gallery;

/// <summary>The host API demo (phase 13): blame column, host decorations, hidden areas, view zones, gutter clicks.</summary>
public sealed partial class HostApiDemoWindow : Window
{
    private const int MaxLogLines = 50;

    /// <summary>Opens the window on the sample file, with the theme of the main window.</summary>
    /// <param name="theme">The theme of the editor.</param>
    public HostApiDemoWindow(EditorTheme theme)
    {
        InitializeComponent();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1300, 900));
        Editor.Theme = theme;
        Editor.LanguageId = BuiltInLanguages.GetLanguageIdByFileName(HostApiSamples.FileName) ?? "plaintext";
        Editor.Text = HostApiSamples.Text;
        RegisterStyles();
        InitializeBlame();
        InitializeGutter();
    }

    private void Log(string line)
    {
        LogList.Items.Insert(0, line);
        while (LogList.Items.Count > MaxLogLines)
        {
            LogList.Items.RemoveAt(LogList.Items.Count - 1);
        }
    }

    private void OnApplyLanguage(object sender, RoutedEventArgs e)
    {
        string? id = BuiltInLanguages.GetLanguageIdByFileName(FileNameBox.Text);
        Editor.LanguageId = id ?? "plaintext";
        Log($"language {id ?? "null"}");
    }

    private void OnReloadSameText(object sender, RoutedEventArgs e)
    {
        Editor.Text = HostApiSamples.Text;
        Editor.ResetText(HostApiSamples.Text);
        Log("ResetText");
    }

    private void OnLineNumbersChanged(object sender, RoutedEventArgs e) => Editor.LineNumbersEnabled = LineNumbersBox.IsChecked == true;
}
