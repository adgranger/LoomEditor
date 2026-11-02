using Range = LoomEditor.Core.Text.Range;
using LoomEditor.TextMate.Grammars;
using LoomEditor.Themes;
using LoomEditor.View.Decorations;
using LoomEditor.View.DiffEditor;
using LoomEditor.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.System;

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

    // Phase 14: touches every public member of the diff line and block actions once, on a diff editor that is never shown, so a member that is not public
    // in the package fails this build.
    private static void UseDiffActions()
    {
        var diff = new LoomDiffEditor
        {
            BlockActionsVisibility = DiffBlockActionsVisibility.OnHover,
            PrimaryBlockActionGlyph = "\uE710",
            PrimaryBlockActionToolTip = "Stage block",
            SecondaryBlockActionGlyph = "\uE7A7",
            SecondaryBlockActionToolTip = "Discard block",
            LineSelectionEnabled = true,
            LineActionGlyph = "\uE710",
            LineActionToolTip = "Stage line",
            LineActionKey = VirtualKey.S,
        };
        DependencyProperty[] properties =
        [
            LoomDiffEditor.BlockActionsVisibilityProperty, LoomDiffEditor.PrimaryBlockActionGlyphProperty, LoomDiffEditor.PrimaryBlockActionToolTipProperty,
            LoomDiffEditor.SecondaryBlockActionGlyphProperty, LoomDiffEditor.SecondaryBlockActionToolTipProperty, LoomDiffEditor.LineSelectionEnabledProperty,
            LoomDiffEditor.LineActionGlyphProperty, LoomDiffEditor.LineActionToolTipProperty, LoomDiffEditor.LineActionKeyProperty,
        ];
        _ = properties.Length + (int)DiffBlockActionsVisibility.Off + (int)DiffBlockActionsVisibility.Always;
        diff.BlockActionInvoked += (_, e) =>
        {
            DiffChangeBlock block = e.Block;
            _ = (e.Action == DiffBlockAction.Secondary, block.OriginalStartLineNumber, block.OriginalLineCount, block.ModifiedStartLineNumber, block.ModifiedLineCount);
            foreach (DiffInnerChange inner in block.InnerChanges)
            {
                _ = (inner.OriginalRange, inner.ModifiedRange);
            }

            _ = new DiffBlockActionInvokedEventArgs(block, DiffBlockAction.Primary);
        };
        diff.LineActionInvoked += (_, e) => _ = new DiffLineActionInvokedEventArgs(e.Lines);
        diff.SelectionChanged += (_, _) => _ = diff.SelectedLines.Count;
        diff.DiffChanged += (_, _) => _ = diff.Changes.Count;
        diff.SetSelection((DiffLine[])[new DiffLine(DiffSide.Modified, 1)]);
        diff.ClearSelection();
    }
}
