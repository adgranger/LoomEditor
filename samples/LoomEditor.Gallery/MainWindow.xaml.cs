using System.Diagnostics;
using System.Globalization;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.TextMate.Grammars;
using LoomEditor.Themes;
using LoomEditor.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.Gallery;

/// <summary>The main window: one editor showing a sample file, and buttons that drive its text model.</summary>
public sealed partial class MainWindow : Window
{
    private bool _isBusy;

    /// <summary>The ids of the built-in languages (the items of the language picker).</summary>
    public string[] LanguageIds { get; } = [.. BuiltInLanguages.Ids];

    /// <summary>The names of the built-in themes (the items of the theme picker).</summary>
    public string[] ThemeNames { get; } = [.. EditorTheme.BuiltIn.Select(theme => theme.Name)];

    // Items are added in code (strings as ComboBoxItem content): the same as the static items of the font picker, which work under Native AOT.
    private void PopulatePickers()
    {
        foreach (string id in LanguageIds)
        {
            LanguageBox.Items.Add(new ComboBoxItem { Content = id });
        }

        foreach (string name in ThemeNames)
        {
            ThemeBox.Items.Add(new ComboBoxItem { Content = name });
        }

        foreach (FeatureSample sample in FeatureSamples.All)
        {
            FeatureSampleBox.Items.Add(new ComboBoxItem { Content = sample.Name });
        }

        LanguageBox.SelectedIndex = 0;
        ThemeBox.SelectedIndex = 0;
    }

    /// <summary>Creates the window and loads the sample text.</summary>
    public MainWindow()
    {
        InitializeComponent();
        PopulatePickers();
        Editor.LanguageId = "csharp";
        LanguageBox.SelectedIndex = Array.IndexOf(LanguageIds, "csharp");
        Editor.Text = SampleText.HelloWorld;
        Editor.Model.ContentChanged += (_, _) => RefreshStatus();
        // A load detects the indentation: show the tab size it chose.
        Editor.Model.OptionsChanged += (_, _) => TabSizeBox.Value = Editor.Model.Options.TabSize;
        Editor.SelectionChanged += (_, _) => RefreshStatus();
        RefreshStatus();
    }

    private void OnInsertHeader(object sender, RoutedEventArgs e) => ModelDemoActions.InsertHeader(Editor.Model);

    private void OnAppendLine(object sender, RoutedEventArgs e) => ModelDemoActions.AppendLine(Editor.Model);

    private void OnDeleteFirstLine(object sender, RoutedEventArgs e) => ModelDemoActions.DeleteFirstLine(Editor.Model);

    private void OnReplaceHello(object sender, RoutedEventArgs e) => ModelDemoActions.ReplaceHello(Editor.Model);

    private void OnUndo(object sender, RoutedEventArgs e) => Editor.Model.Undo();

    private void OnRedo(object sender, RoutedEventArgs e) => Editor.Model.Redo();

    private void OnLf(object sender, RoutedEventArgs e) => ModelDemoActions.SetEol(Editor.Model, EndOfLineSequence.LF);

    private void OnCrlf(object sender, RoutedEventArgs e) => ModelDemoActions.SetEol(Editor.Model, EndOfLineSequence.CRLF);

    private void OnReset(object sender, RoutedEventArgs e)
    {
        // The Text property does not change when the model was edited, so setting it to the same value again would be
        // ignored: go through an empty text first.
        Editor.Text = string.Empty;
        Editor.Text = SampleText.HelloWorld;
    }

    private void OnSmoothScrollingToggled(object sender, RoutedEventArgs e) => Editor.SmoothScrolling = SmoothScrollingSwitch.IsOn;

    private void OnWordWrapToggled(object sender, RoutedEventArgs e) => Editor.WordWrap = WordWrapSwitch.IsOn ? EditorWordWrap.On : EditorWordWrap.Off;

    private void OnGuidesChanged(object sender, SelectionChangedEventArgs e)
    {
        Editor.RenderIndentGuides = GuidesBox.SelectedIndex != 2;
        Editor.BracketPairGuides = GuidesBox.SelectedIndex == 1 ? EditorBracketPairGuides.On : EditorBracketPairGuides.Off;
    }

    private void OnReadOnlyToggled(object sender, RoutedEventArgs e) => Editor.IsReadOnly = ReadOnlySwitch.IsOn;

    // Three cursors on three lines: type, delete, indent and paste show the multi-cursor behavior.
    private void OnMultiCursorSample(object sender, RoutedEventArgs e)
    {
        Editor.Text = string.Empty;
        Editor.Text = SampleText.MultiCursor;
        Editor.SetSelections(
            new[]
            {
                Selection.Collapsed(new Position(1, 6)),
                Selection.Collapsed(new Position(2, 6)),
                new Selection(new Position(3, 1), new Position(3, 6)),
            });
        Editor.Focus(FocusState.Programmatic);
    }

    private async void OnLargeFile(object sender, RoutedEventArgs e) => await LoadDocumentAsync(LargeDocuments.Create100MB);

    private async void OnLongLine(object sender, RoutedEventArgs e) => await LoadDocumentAsync(LargeDocuments.CreateLongLine);

    private async void OnUnicode(object sender, RoutedEventArgs e) => await LoadDocumentAsync(() => LargeDocuments.Unicode);

    private async void OnScrollTest(object sender, RoutedEventArgs e)
    {
        if (!BeginBusy("Scrolling for 10 seconds…"))
        {
            return;
        }

        try
        {
            FrameStatistics statistics = await ScrollTest.RunAsync(Editor, TimeSpan.FromSeconds(10));
            StatusText.Text = statistics.Format();
        }
        finally
        {
            EndBusy();
        }
    }

    // Generates on a background thread; only the assignment of the text happens on the UI thread.
    private async Task LoadDocumentAsync(Func<string> create)
    {
        if (!BeginBusy("Generating…"))
        {
            return;
        }

        try
        {
            var stopwatch = Stopwatch.StartNew();
            string text = await Task.Run(create);
            Editor.ScrollTop = 0;
            Editor.ScrollLeft = 0;
            Editor.Text = text;
            await NextRenderingAsync();
            stopwatch.Stop();
            StatusText.Text = string.Create(
                CultureInfo.InvariantCulture, $"Loaded {Editor.Model.LineCount:N0} lines in {stopwatch.ElapsedMilliseconds:N0} ms");
        }
        finally
        {
            EndBusy();
        }
    }

    private static Task NextRenderingAsync()
    {
        var completion = new TaskCompletionSource();
        void OnRendering(object? sender, object e)
        {
            CompositionTarget.Rendering -= OnRendering;
            completion.SetResult();
        }

        CompositionTarget.Rendering += OnRendering;
        return completion.Task;
    }

    private bool BeginBusy(string message)
    {
        if (_isBusy)
        {
            return false;
        }

        _isBusy = true;
        SetSampleButtonsEnabled(false);
        StatusText.Text = message;
        return true;
    }

    private void EndBusy()
    {
        _isBusy = false;
        SetSampleButtonsEnabled(true);
    }

    private void SetSampleButtonsEnabled(bool isEnabled)
    {
        LargeFileButton.IsEnabled = isEnabled;
        LongLineButton.IsEnabled = isEnabled;
        UnicodeButton.IsEnabled = isEnabled;
        ScrollTestButton.IsEnabled = isEnabled;
        MultiCursorButton.IsEnabled = isEnabled;
        LargeCSharpButton.IsEnabled = isEnabled;
        FeatureSampleBox.IsEnabled = isEnabled;
    }

    private void OnFontSizeChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (Editor is not null && !double.IsNaN(args.NewValue))
        {
            Editor.FontSize = args.NewValue;
        }
    }

    // Same order as the items of FontFamilyBox. SelectedItem is not read: under Native AOT it comes back as a plain
    // ContentControl, not a ComboBoxItem, so a type test on it (or on its Content) never matches.
    private static readonly string[] FontFamilies = ["Cascadia Mono", "Consolas", "Segoe UI"];

    private void OnFontFamilyChanged(object sender, SelectionChangedEventArgs e)
    {
        int index = FontFamilyBox.SelectedIndex;
        if (Editor is not null && index >= 0 && index < FontFamilies.Length)
        {
            Editor.FontFamily = new FontFamily(FontFamilies[index]);
        }
    }

    // SelectedIndex, not SelectedItem: see FontFamilies.
    private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        int index = LanguageBox.SelectedIndex;
        if (Editor is not null && index >= 0 && index < LanguageIds.Length)
        {
            Editor.LanguageId = LanguageIds[index];
            Editor.Text = string.Empty;
            Editor.Text = HighlightingSamples.For(LanguageIds[index]);
        }
    }

    // SelectedIndex, not SelectedItem: see FontFamilies.
    private void OnFeatureSampleChanged(object sender, SelectionChangedEventArgs e)
    {
        int index = FeatureSampleBox.SelectedIndex;
        if (Editor is null || index < 0 || index >= FeatureSamples.All.Count)
        {
            return;
        }

        FeatureSample sample = FeatureSamples.All[index];
        Editor.LanguageId = sample.LanguageId;
        LanguageBox.SelectedIndex = Array.IndexOf(LanguageIds, sample.LanguageId);
        Editor.Text = string.Empty;
        Editor.Text = sample.Text;
        Editor.SetSelections(new[] { Selection.Collapsed(new Position(sample.Line, sample.Column)) });
        FeatureHintText.Text = sample.Hint;
        Editor.Focus(FocusState.Programmatic);
    }

    private void OnThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        int index = ThemeBox.SelectedIndex;
        if (Editor is not null && index >= 0 && index < EditorTheme.BuiltIn.Count)
        {
            Editor.Theme = EditorTheme.BuiltIn[index];
        }
    }

    private async void OnLargeCSharp(object sender, RoutedEventArgs e)
    {
        Editor.LanguageId = "csharp";
        LanguageBox.SelectedIndex = Array.IndexOf(LanguageIds, "csharp");
        await LoadDocumentAsync(HighlightingSamples.CreateLargeCSharp);
    }

    private void OnTabSizeChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (Editor is not null && !double.IsNaN(args.NewValue))
        {
            Editor.TabSize = (int)args.NewValue;
        }
    }

    private string FormatCursors()
    {
        IReadOnlyList<Selection> selections = Editor.Selections;
        Selection primary = selections[0];
        return string.Create(
            CultureInfo.InvariantCulture,
            $" · Cursors: {selections.Count} · Primary: ({primary.SelectionStartLineNumber},{primary.SelectionStartColumn})-({primary.PositionLineNumber},{primary.PositionColumn})");
    }

    private void RefreshStatus()
    {
        StatusText.Text = ModelDemoActions.FormatStatus(Editor.Model)
            + FormatCursors();
        UndoButton.IsEnabled = Editor.Model.CanUndo;
        RedoButton.IsEnabled = Editor.Model.CanRedo;
    }
}
