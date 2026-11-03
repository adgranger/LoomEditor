// The host's point of view: this window and its partial files use public members only, no internal member of any LoomEditor assembly.
using LoomEditor.TextMate.Grammars;
using LoomEditor.Themes;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace LoomEditor.Gallery;

/// <summary>The host API demo (phase 13): blame column, host decorations, hidden areas, view zones, gutter clicks.</summary>
public sealed partial class HostApiDemoWindow : Window
{
    private const int MaxLogLines = 50;
    private readonly ObservableCollection<string> _log = [];

    /// <summary>Opens the window with the dark theme (the constructor the XAML runtime loader and design tools look for).</summary>
    public HostApiDemoWindow()
        : this(EditorTheme.Dark)
    {
    }

    /// <summary>Opens the window on the sample file, with the theme of the main window.</summary>
    /// <param name="theme">The theme of the editor.</param>
    public HostApiDemoWindow(EditorTheme theme)
    {
        InitializeComponent();
        LogList.ItemsSource = _log;
        Editor.Theme = theme;
        Editor.LanguageId = BuiltInLanguages.GetLanguageIdByFileName(HostApiSamples.FileName) ?? "plaintext";
        Editor.Text = HostApiSamples.Text;
        RegisterStyles();
        InitializeBlame();
        InitializeGutter();
    }

    private void Log(string line)
    {
        _log.Insert(0, line);
        while (_log.Count > MaxLogLines)
        {
            _log.RemoveAt(_log.Count - 1);
        }
    }

    private void OnApplyLanguage(object? sender, RoutedEventArgs e)
    {
        string? id = BuiltInLanguages.GetLanguageIdByFileName(FileNameBox.Text ?? string.Empty);
        Editor.LanguageId = id ?? "plaintext";
        Log($"language {id ?? "null"}");
    }

    private void OnReloadSameText(object? sender, RoutedEventArgs e)
    {
        Editor.Text = HostApiSamples.Text;
        Editor.ResetText(HostApiSamples.Text);
        Log("ResetText");
    }

    private void OnLineNumbersChanged(object? sender, RoutedEventArgs e)
    {
        // IsChecked="True" in XAML raises this handler during InitializeComponent, before Editor exists: the control's default matches.
        if (Editor is not null)
        {
            Editor.LineNumbersEnabled = LineNumbersBox.IsChecked == true;
        }
    }
}
