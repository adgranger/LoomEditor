using LoomEditor.Themes;
using LoomEditor.View.Decorations;
using LoomEditor.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LoomEditor.Gallery;

// Host decorations, styles, hidden areas, view zones and gutter clicks, through the public API only.
public sealed partial class HostApiDemoWindow
{
    private readonly List<string> _zoneIds = [];
    private DecorationsCollection? _conflicts;
    private DecorationsCollection? _staged;

    private static void RegisterStyles()
    {
        CustomDecorationStyles.Register("gallery-blame-recent", new CustomDecorationStyle
        {
            Background = ThemeColor.Parse("#FFE08040"),
            DarkBackground = ThemeColor.Parse("#FF805020"),
        });
        CustomDecorationStyles.Register("gallery-staged", new CustomDecorationStyle
        {
            Border = ThemeColor.Parse("#FF2E9E4F"),
            DarkBorder = ThemeColor.Parse("#FF4EC970"),
            BorderWidth = 0,
            LeftBorderWidth = 3,
        });
        CustomDecorationStyles.Register("gallery-conflict-current", new CustomDecorationStyle
        {
            Background = ThemeColor.Parse("#40208040"),
            DarkBackground = ThemeColor.Parse("#40408060"),
        });
        CustomDecorationStyles.Register("gallery-conflict-incoming", new CustomDecorationStyle
        {
            Background = ThemeColor.Parse("#402060C0"),
            DarkBackground = ThemeColor.Parse("#403070C0"),
        });
    }

    private void InitializeGutter()
    {
        _conflicts = Editor.CreateDecorationsCollection();
        _staged = Editor.CreateDecorationsCollection();
        Editor.GutterClicked += OnGutterClicked;
    }

    private void OnConflictColors(object sender, RoutedEventArgs e)
    {
        var current = new EditorDecorationOptions
        {
            ClassName = "line-insert",
            IsWholeLine = true,
            OverviewRulerColor = ThemeColor.Parse("#FF208040"),
        };
        var incoming = new EditorDecorationOptions
        {
            ClassName = "line-delete",
            IsWholeLine = true,
            OverviewRulerColor = ThemeColor.Parse("#FF2060C0"),
        };
        EditorDecorationOptions MarkerOf(string className) => new() { ClassName = className, IsWholeLine = true };
        _conflicts!.Set(
        (EditorDecoration[])[
            new EditorDecoration(new Range(10, 1, 12, 1), current),
            new EditorDecoration(new Range(14, 1, 16, 1), incoming),
            new EditorDecoration(new Range(9, 1, 9, 1), MarkerOf("gallery-conflict-current")),
            new EditorDecoration(new Range(13, 1, 13, 1), MarkerOf("gallery-conflict-current")),
            new EditorDecoration(new Range(17, 1, 17, 1), MarkerOf("gallery-conflict-incoming")),
        ]);
        Log($"Conflict colors: {_conflicts.Count} decorations");
    }

    private void OnClearDecorations(object sender, RoutedEventArgs e)
    {
        _conflicts!.Clear();
        Log("Clear decorations");
    }

    private void OnHideUnchanged(object sender, RoutedEventArgs e) => Editor.SetHiddenAreas((Range[])[new Range(20, 1, 40, 1)]);

    private void OnShowAll(object sender, RoutedEventArgs e) => Editor.SetHiddenAreas(Array.Empty<Range>());

    private void OnAddConflictHeader(object sender, RoutedEventArgs e)
    {
        var current = new Button { Content = "Accept current" };
        current.Click += (_, _) => Log("Accept current");
        var incoming = new Button { Content = "Accept incoming" };
        incoming.Click += (_, _) => Log("Accept incoming");
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        panel.Children.Add(current);
        panel.Children.Add(incoming);
        _zoneIds.Add(Editor.AddViewZone(8, Editor.LineHeight, panel));
        Log("Add conflict header");
    }

    private void OnAddTextBoxZone(object sender, RoutedEventArgs e)
    {
        _zoneIds.Add(Editor.AddViewZone(30, Editor.LineHeight * 2, new TextBox { PlaceholderText = "Host text box" }));
        Log("Add text box zone");
    }

    private void OnRemoveZones(object sender, RoutedEventArgs e)
    {
        foreach (string id in _zoneIds)
        {
            Log($"RemoveViewZone -> {Editor.RemoveViewZone(id)}");
        }

        if (_zoneIds.Count > 0)
        {
            Log($"RemoveViewZone (again) -> {Editor.RemoveViewZone(_zoneIds[0])}");
        }

        _zoneIds.Clear();
    }

    // The line decorations toggle a "staged" bar and swallow the press (no caret move, no fold); the other areas only log.
    private void OnGutterClicked(object? sender, GutterClickedEventArgs e)
    {
        Log($"GutterClicked line {e.LineNumber} {e.Area}");
        if (e.Area != GutterArea.LineDecorations)
        {
            return;
        }

        e.Handled = true;
        var ranges = _staged!.GetRanges().ToList();
        int index = ranges.FindIndex(r => r.StartLineNumber == e.LineNumber);
        if (index >= 0)
        {
            ranges.RemoveAt(index);
        }
        else
        {
            ranges.Add(new Range(e.LineNumber, 1, e.LineNumber, 1));
        }

        var options = new EditorDecorationOptions { ClassName = "gallery-staged", IsWholeLine = true };
        _staged.Set(ranges.Select(r => new EditorDecoration(r, options)).ToArray());
    }
}
