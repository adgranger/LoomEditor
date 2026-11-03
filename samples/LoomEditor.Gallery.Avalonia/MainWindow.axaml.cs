using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.TextMate.Grammars;
using LoomEditor.Themes;
using LoomEditor.View.Features.FontZoom;
using LoomEditor.View.Layout;

namespace LoomEditor.Gallery;

/// <summary>The main window: one editor showing a sample file, and buttons that drive its text model.</summary>
public sealed partial class MainWindow : Window
{
    private bool _isBusy;
    private readonly List<string> _injectedTextIds = [];

    /// <summary>The ids of the built-in languages (the items of the language picker).</summary>
    public string[] LanguageIds { get; } = [.. BuiltInLanguages.Ids];

    /// <summary>The names of the built-in themes (the items of the theme picker).</summary>
    public string[] ThemeNames { get; } = [.. EditorTheme.BuiltIn.Select(theme => theme.Name)];

    // Items are added in code (strings as ComboBoxItem content): the pickers are filled from the library's lists.
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
        InitializeComments();
        Editor.LanguageId = "csharp";
        LanguageBox.SelectedIndex = Array.IndexOf(LanguageIds, "csharp");
        Editor.Text = SampleText.HelloWorld;
        Editor.Model.ContentChanged += (_, _) => RefreshStatus();
        // A load detects the indentation: show the tab size it chose.
        Editor.Model.OptionsChanged += (_, _) => TabSizeBox.Value = Editor.Model.Options.TabSize;
        Editor.SelectionChanged += (_, _) => RefreshStatus();
        Editor.PropertyChanged += (_, e) =>
        {
            if (e.Property == LoomEditorControl.InputModeProperty)
            {
                OvertypeSwitch.IsChecked = Editor.InputMode == LoomEditor.View.Cursors.EditorInputMode.Overtype;
            }
            else if (e.Property == LoomEditorControl.TabMovesFocusProperty)
            {
                // Ctrl+M toggles the property from the keyboard: keep the switch in sync.
                TabFocusSwitch.IsChecked = Editor.TabMovesFocus;
            }
        };
        LetterSpacingSlider.PropertyChanged += (_, e) =>
        {
            if (e.Property == RangeBase.ValueProperty)
            {
                Editor.LetterSpacing = LetterSpacingSlider.Value;
            }
        };
        RefreshStatus();
        InitializeLsp();
        InitializeDiffSelfCheck();
        InitializeTextSelfCheck();
        InitializeCommentsSelfCheck();
        InitializeHostApiSelfCheck();
    }

    private void OnInsertHeader(object? sender, RoutedEventArgs e) => ModelDemoActions.InsertHeader(Editor.Model);

    private void OnAppendLine(object? sender, RoutedEventArgs e) => ModelDemoActions.AppendLine(Editor.Model);

    private void OnDeleteFirstLine(object? sender, RoutedEventArgs e) => ModelDemoActions.DeleteFirstLine(Editor.Model);

    private void OnReplaceHello(object? sender, RoutedEventArgs e) => ModelDemoActions.ReplaceHello(Editor.Model);

    private void OnUndo(object? sender, RoutedEventArgs e) => Editor.Model.Undo();

    private void OnRedo(object? sender, RoutedEventArgs e) => Editor.Model.Redo();

    private void OnLf(object? sender, RoutedEventArgs e) => ModelDemoActions.SetEol(Editor.Model, EndOfLineSequence.LF);

    private void OnCrlf(object? sender, RoutedEventArgs e) => ModelDemoActions.SetEol(Editor.Model, EndOfLineSequence.CRLF);

    private void OnReset(object? sender, RoutedEventArgs e)
    {
        // The Text property does not change when the model was edited, so setting it to the same value again would be
        // ignored: go through an empty text first.
        Editor.Text = string.Empty;
        Editor.Text = SampleText.HelloWorld;
    }

    // IsChecked / SelectedIndex set in XAML raise these handlers during InitializeComponent, before Editor exists: the control's defaults match.
    private void OnSmoothScrollingToggled(object? sender, RoutedEventArgs e)
    {
        if (Editor is not null)
        {
            Editor.SmoothScrolling = SmoothScrollingSwitch.IsChecked == true;
        }
    }

    private void OnMinimapToggled(object? sender, RoutedEventArgs e)
    {
        if (Editor is not null)
        {
            Editor.MinimapEnabled = MinimapSwitch.IsChecked == true;
        }
    }

    private void OnStickyScrollToggled(object? sender, RoutedEventArgs e)
    {
        if (Editor is not null)
        {
            Editor.StickyScrollEnabled = StickyScrollSwitch.IsChecked == true;
        }
    }

    private void OnScreenReaderChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Editor is not null && ScreenReaderBox.SelectedIndex >= 0)
        {
            Editor.AccessibilitySupport = (EditorAccessibilitySupport)ScreenReaderBox.SelectedIndex;
        }
    }

    private void OnTabFocusToggled(object? sender, RoutedEventArgs e)
    {
        if (Editor is not null)
        {
            Editor.TabMovesFocus = TabFocusSwitch.IsChecked == true;
        }
    }

    private void OnSystemHighContrastToggled(object? sender, RoutedEventArgs e)
    {
        if (Editor is not null)
        {
            Editor.UseSystemHighContrast = SystemHighContrastSwitch.IsChecked == true;
        }
    }

    // One line of 12 000 characters: "dominated by long lines", which forces wrapping in screen reader mode.
    private void OnLongLineSample(object? sender, RoutedEventArgs e)
    {
        Editor.LanguageId = "plaintext";
        LanguageBox.SelectedIndex = Array.IndexOf(LanguageIds, "plaintext");
        WordWrapSwitch.IsChecked = false;
        Editor.WordWrap = EditorWordWrap.Off;
        Editor.Text = string.Concat(Enumerable.Repeat("lorem ", 2000)) + "\n" + string.Join('\n', Enumerable.Range(1, 20).Select(n => $"line {n}"));
    }

    // UI Automation is a Windows client API: the self-check exists only in the WinUI Gallery.
    private void OnUiaSelfCheck(object? sender, RoutedEventArgs e)
    {
        SelfCheckText.Text = Program.UiaNotAvailable;
        SelfCheckScroll.IsVisible = true;
        StatusText.Text = Program.UiaNotAvailable;
    }

    private void OnWordWrapToggled(object? sender, RoutedEventArgs e)
    {
        if (Editor is not null)
        {
            Editor.WordWrap = WordWrapSwitch.IsChecked == true ? EditorWordWrap.On : EditorWordWrap.Off;
        }
    }

    private void OnControlCharactersToggled(object? sender, RoutedEventArgs e)
    {
        if (Editor is not null)
        {
            Editor.RenderControlCharacters = ControlCharactersSwitch.IsChecked == true;
        }
    }

    private void OnCursorStyleChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Editor is not null && CursorStyleBox.SelectedIndex >= 0)
        {
            Editor.CursorStyle = (TextEditorCursorStyle)CursorStyleBox.SelectedIndex;
        }
    }

    private void OnWhitespaceChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Editor is not null && WhitespaceBox.SelectedIndex >= 0)
        {
            Editor.RenderWhitespace = (RenderWhitespace)WhitespaceBox.SelectedIndex;
        }
    }

    private void OnGuidesChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Editor is null || GuidesBox.SelectedIndex < 0)
        {
            return;
        }

        Editor.RenderIndentGuides = GuidesBox.SelectedIndex != 2;
        Editor.BracketPairGuides = GuidesBox.SelectedIndex == 1 ? EditorBracketPairGuides.On : EditorBracketPairGuides.Off;
    }

    // Decorations of the old text are gone once a new text is loaded, so the ids are only valid for the current model state.
    private void OnInjectText(object? sender, RoutedEventArgs e)
    {
        Selection primary = Editor.Selections[0];
        _injectedTextIds.Add(ModelDemoActions.InjectText(Editor.Model, new Position(primary.PositionLineNumber, primary.PositionColumn)));
        Editor.Focus();
    }

    private void OnClearInjectedText(object? sender, RoutedEventArgs e)
    {
        ModelDemoActions.ClearInjectedText(Editor.Model, _injectedTextIds);
        _injectedTextIds.Clear();
    }

    private void OnInsertSnippet(object? sender, RoutedEventArgs e)
    {
        Editor.InsertSnippet(Phase10Samples.ForLoopSnippet);
        Editor.Focus();
    }

    private void OnInsertSnippetAroundSelection(object? sender, RoutedEventArgs e)
    {
        Editor.InsertSnippet(Phase10Samples.LogSelectionSnippet);
        Editor.Focus();
    }

    // The positions fit the "Problems" sample; on another text the markers are clamped by the model.
    private void OnAddSampleProblems(object? sender, RoutedEventArgs e)
    {
        Editor.Markers.Change(Phase10Samples.ProblemsOwner, Phase10Samples.SampleProblems);
        Editor.Focus();
    }

    private void OnReadOnlyToggled(object? sender, RoutedEventArgs e)
    {
        if (Editor is not null)
        {
            Editor.IsReadOnly = ReadOnlySwitch.IsChecked == true;
        }
    }

    // Three cursors on three lines: type, delete, indent and paste show the multi-cursor behavior.
    private void OnMultiCursorSample(object? sender, RoutedEventArgs e)
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
        Editor.Focus();
    }

    private async void OnLargeFile(object? sender, RoutedEventArgs e) => await LoadDocumentAsync(LargeDocuments.Create100MB);

    private async void OnLongLine(object? sender, RoutedEventArgs e) => await LoadDocumentAsync(LargeDocuments.CreateLongLine);

    private async void OnUnicode(object? sender, RoutedEventArgs e) => await LoadDocumentAsync(() => LargeDocuments.Unicode);

    private async void OnScrollTest(object? sender, RoutedEventArgs e)
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

    private async void OnOpenFile(object? sender, RoutedEventArgs e)
    {
        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Open file", AllowMultiple = false });
        if (files.Count == 0)
        {
            return;
        }

        await using Stream stream = await files[0].OpenReadAsync();
        using var reader = new StreamReader(stream);
        string text = await reader.ReadToEndAsync();
        string id = BuiltInLanguages.GetLanguageIdByFileName(files[0].Name) ?? "plaintext";
        Editor.LanguageId = id;
        LanguageBox.SelectedIndex = Array.IndexOf(LanguageIds, id);
        Editor.Text = string.Empty;
        Editor.Text = text;
        StatusText.Text = $"Opened {files[0].Name}";
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

    private Task NextRenderingAsync()
    {
        var completion = new TaskCompletionSource();
        GetTopLevel(this)!.RequestAnimationFrame(_ => completion.SetResult());
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

    private void OnFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (Editor is not null && e.NewValue is { } value)
        {
            Editor.FontSize = (double)value;
        }
    }

    // Same order as the items of FontFamilyBox; the first is the control's default (the monospace font of the operating system).
    private static readonly string?[] FontFamilies = [null, "Cascadia Code", "Consolas", "Menlo", "DejaVu Sans Mono", "Segoe UI"];

    private static readonly int[] FontWeights = [400, 500, 600, 700];

    private void OnFontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Editor is null)
        {
            return;
        }

        int index = FontFamilyBox.SelectedIndex;
        if (index < 0 || index >= FontFamilies.Length)
        {
            return;
        }

        if (FontFamilies[index] is { } family)
        {
            Editor.FontFamily = new FontFamily(family);
        }
        else
        {
            Editor.ClearValue(TemplatedControl.FontFamilyProperty);
        }
    }

    private void OnFontLigaturesToggled(object? sender, RoutedEventArgs e)
    {
        if (Editor is not null)
        {
            Editor.FontLigatures = FontLigaturesSwitch.IsChecked == true;
        }
    }

    private void OnFontWeightChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Editor is null)
        {
            return;
        }

        int index = FontWeightBox.SelectedIndex;
        if (index >= 0 && index < FontWeights.Length)
        {
            Editor.FontWeight = (FontWeight)FontWeights[index];
        }
    }

    private void OnWheelZoomToggled(object? sender, RoutedEventArgs e)
    {
        if (Editor is not null)
        {
            Editor.MouseWheelZoom = WheelZoomSwitch.IsChecked == true;
        }
    }

    private void OnZoomOutClick(object? sender, RoutedEventArgs e) => Editor.Commands.Execute(FontZoomCommands.FontZoomOut);

    private void OnZoomInClick(object? sender, RoutedEventArgs e) => Editor.Commands.Execute(FontZoomCommands.FontZoomIn);

    private void OnZoomResetClick(object? sender, RoutedEventArgs e) => Editor.Commands.Execute(FontZoomCommands.FontZoomReset);

    private void OnLanguageChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Editor is null)
        {
            return;
        }

        int index = LanguageBox.SelectedIndex;
        if (index >= 0 && index < LanguageIds.Length)
        {
            Editor.LanguageId = LanguageIds[index];
            Editor.Text = string.Empty;
            Editor.Text = HighlightingSamples.For(LanguageIds[index]);
        }
    }

    private void OnFeatureSampleChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Editor is null)
        {
            return;
        }

        int index = FeatureSampleBox.SelectedIndex;
        if (index < 0 || index >= FeatureSamples.All.Count)
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
        OnFeatureSampleLoaded(sample);
        Editor.Focus();
    }

    // The theme of the editor is the library's; the Fluent variant of the app follows it so the side panel matches.
    private void OnThemeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Editor is null)
        {
            return;
        }

        int index = ThemeBox.SelectedIndex;
        if (index >= 0 && index < EditorTheme.BuiltIn.Count)
        {
            Editor.Theme = EditorTheme.BuiltIn[index];
            DiffEditor.Theme = Editor.Theme;
            bool dark = Editor.Theme.Type is ThemeType.Dark or ThemeType.HighContrastDark;
            Application.Current!.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
        }
    }

    private async void OnLargeCSharp(object? sender, RoutedEventArgs e)
    {
        Editor.LanguageId = "csharp";
        LanguageBox.SelectedIndex = Array.IndexOf(LanguageIds, "csharp");
        await LoadDocumentAsync(HighlightingSamples.CreateLargeCSharp);
    }

    private void OnTabSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (Editor is not null && e.NewValue is { } value)
        {
            Editor.TabSize = (int)value;
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
        StatusText.Text = ModelDemoActions.FormatStatus(Editor.Model) + FormatCursors();
        UndoButton.IsEnabled = Editor.Model.CanUndo;
        RedoButton.IsEnabled = Editor.Model.CanRedo;
    }
}
