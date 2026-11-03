using System.Text;
using LoomEditor.Themes;
using Avalonia.Interactivity;
using LoomEditor.Avalonia.Controls;

namespace LoomEditor.Gallery;

// The block and line actions of phase 14, as a git client would use them to stage. The demo only logs the events: it has no git behavior.
public sealed partial class DiffDemoWindow
{
    private const int MaxLogLines = 200;

    private readonly List<string> _stagingLog = [];

    private void InitializeActions()
    {
        DiffEditor.PrimaryBlockActionToolTip = "Stage block";
        DiffEditor.SecondaryBlockActionToolTip = "Discard block";
        DiffEditor.LineActionToolTip = "Stage lines";
        DiffEditor.BlockActionInvoked += (_, e) => LogStaging($"BlockActionInvoked {e.Action} {e.Block}");
        DiffEditor.LineActionInvoked += (_, e) => LogStaging($"LineActionInvoked {Format(e.Lines)}");
        DiffEditor.SelectionChanged += (_, _) =>
        {
            IReadOnlyList<DiffLine> lines = DiffEditor.SelectedLines;
            SelectedLinesText.Text = $"Selected: {Format(lines)}";
            LogStaging($"SelectionChanged {lines.Count}");
        };
        DiffEditor.DiffChanged += (_, _) => LogStaging($"DiffChanged {DiffEditor.Changes.Count} blocks");
        DiffEditor.Loaded += (_, _) => ApplyWordWrap();
        OnStagingOptionChanged(this, new RoutedEventArgs());
    }

    // Original / Modified exist only once the template is applied (before Loaded): the constructor runs earlier.
    private void ApplyWordWrap()
    {
        EditorWordWrap wrap = WordWrapBox.IsChecked == true ? EditorWordWrap.On : EditorWordWrap.Off;
        DiffEditor.Original.WordWrap = wrap;
        DiffEditor.Modified.WordWrap = wrap;
    }

    // The ComboBox raises SelectionChanged inside InitializeComponent, before the other controls exist.
    private void OnStagingOptionChanged(object? sender, RoutedEventArgs e)
    {
        if (DiffEditor is null || LightThemeBox is null)
        {
            return;
        }

        DiffEditor.BlockActionsVisibility = (DiffBlockActionsVisibility)Math.Max(0, BlockActionsBox.SelectedIndex);
        DiffEditor.PrimaryBlockActionGlyph = StageActionBox.IsChecked == true ? Glyph("\ue710", "+") : null;
        DiffEditor.SecondaryBlockActionGlyph = DiscardActionBox.IsChecked == true ? Glyph("\ue7a7", "\u2715") : null;
        DiffEditor.LineSelectionEnabled = LineSelectionBox.IsChecked == true;
        DiffEditor.LineActionGlyph = LineHoverButtonBox.IsChecked == true ? Glyph("\ue710", "+") : null;
        if (DiffEditor.IsLoaded)
        {
            ApplyWordWrap();
        }

        DiffEditor.LineHeight = LineHeight30Box.IsChecked == true ? 30 : 0;

        // Only on a click: the window opens with the theme of the main window.
        if (ReferenceEquals(sender, LightThemeBox))
        {
            DiffEditor.Theme = LightThemeBox.IsChecked == true ? EditorTheme.Light : EditorTheme.Dark;
        }
    }

    // The glyphs are characters of Segoe Fluent Icons (the default glyph font of the diff editor), which only exists on Windows.
    private static string Glyph(string segoe, string fallback) => OperatingSystem.IsWindows() ? segoe : fallback;

    private void OnStagingSample(object? sender, RoutedEventArgs e)
    {
        DiffEditor.OriginalText = string.Join("\r\n", StagingOriginal);
        DiffEditor.ModifiedText = string.Join('\n', StagingModified);
    }

    private void OnLargeFile(object? sender, RoutedEventArgs e)
    {
        (string original, string modified) = CreateLargeStagingPair();
        DiffEditor.OriginalText = original;
        DiffEditor.ModifiedText = modified;
    }

    private void OnClearSelection(object? sender, RoutedEventArgs e) => DiffEditor.ClearSelection();

    private void OnSelectAllAdded(object? sender, RoutedEventArgs e) =>
        DiffEditor.SetSelection(DiffEditor.Changes.SelectMany(block =>
            Enumerable.Range(block.ModifiedStartLineNumber, block.ModifiedLineCount).Select(line => new DiffLine(DiffSide.Modified, line))));

    private void LogStaging(string message)
    {
        _stagingLog.Insert(0, $"{DateTime.Now:HH:mm:ss} {message}");
        if (_stagingLog.Count > MaxLogLines)
        {
            _stagingLog.RemoveAt(MaxLogLines);
        }

        StagingLogText.Text = string.Join('\n', _stagingLog);
    }

    // "O7 M7": the side's initial and the line number.
    private static string Format(IEnumerable<DiffLine> lines) =>
        string.Join(' ', lines.Select(line => $"{(line.Side == DiffSide.Original ? 'O' : 'M')}{line.LineNumber}"));

    // Five blocks: 2,0 -> 2,1 (pure insertion), 4,1 -> 5,0 (pure deletion), 7,1 -> 7,1 (modified pair), 10,2 -> 10,4 and 14,1 -> 16,1.
    // The original is joined with CRLF and the modified with LF: the end of line is not a change.
    private static readonly string[] StagingOriginal =
    [
        "using System;",
        "using System.Text;",
        "",
        "// Obsolete helpers below.",
        "namespace Demo;",
        "",
        "public static class Greeter",
        "{",
        "    public static string Hello(string name)",
        "        => \"Hello \" +",
        "           name;",
        "",
        "    public static string Bye(string name)",
        "        => \"Bye \" + name;",
        "}",
    ];

    private static readonly string[] StagingModified =
    [
        "using System;",
        "using System.Linq;",
        "using System.Text;",
        "",
        "namespace Demo;",
        "",
        "public static class Welcome",
        "{",
        "    public static string Hello(string name)",
        "    {",
        "        ArgumentNullException.ThrowIfNull(name);",
        "        return $\"Hello, {name}!\";",
        "    }",
        "",
        "    public static string Bye(string name)",
        "        => \"Goodbye \" + name;",
        "}",
    ];

    // 50 000 original lines; about one line in 25 is changed, deleted or followed by an inserted line, which gives about 2 000 blocks.
    private static (string Original, string Modified) CreateLargeStagingPair()
    {
        var random = new Random(14);
        var original = new StringBuilder();
        var modified = new StringBuilder();
        for (int i = 1; i <= 50_000; i++)
        {
            string line = $"    total += Measure({i % 89}, {i * 17 % 1009}); // line {i}";
            original.Append(line).Append('\n');
            switch (random.Next(75))
            {
                case 0:
                    modified.Append(line.Replace("Measure", "Estimate", StringComparison.Ordinal)).Append('\n');
                    break;
                case 1:
                    break;
                case 2:
                    string inserted = $"    total += Bonus({i}); // inserted";
                    modified.Append(line).Append('\n').Append(inserted).Append('\n');
                    break;
                default:
                    modified.Append(line).Append('\n');
                    break;
            }
        }

        return (original.ToString(), modified.ToString());
    }
}
