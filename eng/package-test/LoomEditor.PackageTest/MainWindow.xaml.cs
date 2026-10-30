using Range = LoomEditor.Core.Text.Range;
using LoomEditor.TextMate.Grammars;
using LoomEditor.Themes;
using LoomEditor.View.Decorations;
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
        UseHostApi();
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

    // Phase 13: touches every public member of the host API once, so a member that is not public in the package fails this build.
    private void UseHostApi()
    {
        Editor.LanguageId = BuiltInLanguages.GetLanguageIdByFileName("Program.cs") ?? "plaintext";
        CustomDecorationStyles.Register("packagetest-style", new CustomDecorationStyle { Background = ThemeColor.FromRgb(1, 2, 3) });
        DecorationsCollection decorations = Editor.CreateDecorationsCollection(
            (EditorDecoration[])[new EditorDecoration(new Range(1, 1, 1, 1), new EditorDecorationOptions { ClassName = "packagetest-style", IsWholeLine = true })]);
        decorations.Clear();
        Editor.ScrollChanged += (_, e) => _ = e.ScrollTop + Editor.LineHeight + Editor.GetTopForLineNumber(1, true);
        Editor.LayoutChanged += (_, _) => _ = Editor.LineHeight;
        Editor.GutterClicked += (_, e) => _ = (e.LineNumber, e.Area);
        Editor.LineNumbersEnabled = true;
        Editor.SetHiddenAreas(Array.Empty<Range>());
        Editor.Loaded += (_, _) =>
        {
            Editor.RemoveViewZone(Editor.AddViewZone(0, 1));
            Editor.ResetText(Editor.Model.GetValue());
        };
    }
}
