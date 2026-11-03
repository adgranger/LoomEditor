using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LoomEditor.Avalonia;
using LoomEditor.Avalonia.Controls;
using LoomEditor.Core.Text;
using LoomEditor.TextMate.Grammars;
using LoomEditor.Themes;
using LoomEditor.View.Decorations;
using LoomEditor.View.DiffEditor;
using Editor = LoomEditor.Avalonia.Controls.LoomEditor;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.PackageTest;

/// <summary>The package test app: the public API of LoomEditor.Avalonia used once, headless. Exit code 0 when it all works.</summary>
internal static class Program
{
    [STAThread]
    private static int Main()
    {
        try
        {
            AppBuilder.Configure<TestApp>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();
            UseEditor();
            UseDiffEditor();
            Console.WriteLine("LoomEditor.Avalonia package test passed.");
            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"LoomEditor.Avalonia package test FAILED: {e}");
            return 1;
        }
    }

    // Touches the host API once, so a member that is not public in the package fails this build.
    private static void UseEditor()
    {
        var editor = new Editor { Text = "// LoomEditor package test\nclass Program { static void Main() { } }\n", Theme = EditorTheme.Dark };
        editor.LanguageId = BuiltInLanguages.GetLanguageIdByFileName("Program.cs") ?? "plaintext";
        var window = new Window { Width = 400, Height = 200, Content = editor };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // A missing control template does not throw by itself: it leaves the control empty.
        if (!editor.GetVisualChildren().Any())
        {
            throw new InvalidOperationException("The LoomEditor control template was not applied: the styles are missing from the package.");
        }

        CustomDecorationStyles.Register("packagetest-style", new CustomDecorationStyle { Background = ThemeColor.FromRgb(1, 2, 3) });
        DecorationsCollection decorations = editor.CreateDecorationsCollection(
            (EditorDecoration[])[new EditorDecoration(new Range(1, 1, 1, 1), new EditorDecorationOptions { ClassName = "packagetest-style", IsWholeLine = true })]);
        decorations.Clear();
        editor.ScrollChanged += (_, e) => _ = e.ScrollTop + editor.LineHeight + editor.GetTopForLineNumber(1, true);
        editor.LayoutChanged += (_, _) => _ = editor.LineHeight;
        editor.GutterClicked += (_, e) => _ = (e.LineNumber, e.Area);
        editor.LineNumbersEnabled = true;
        editor.SetHiddenAreas(Array.Empty<Range>());
        editor.RemoveViewZone(editor.AddViewZone(0, 1));
        editor.ResetText(editor.Model.GetValue());
        editor.SetSelections([Selection.Collapsed(new Position(1, 1))]);
        Dispatcher.UIThread.RunJobs();
        window.Close();
    }

    // The diff editor: the change blocks, the selection and the block and line actions.
    private static void UseDiffEditor()
    {
        var diff = new LoomDiffEditor
        {
            OriginalText = "a\nb\nc\n",
            ModifiedText = "a\nB\nc\nd\n",
            BlockActionsVisibility = DiffBlockActionsVisibility.OnHover,
            PrimaryBlockActionGlyph = "+",
            SecondaryBlockActionGlyph = "x",
            LineSelectionEnabled = true,
            LineActionGlyph = "+",
        };
        diff.BlockActionInvoked += (_, e) =>
        {
            DiffChangeBlock block = e.Block;
            _ = (e.Action == DiffBlockAction.Secondary, block.OriginalStartLineNumber, block.ModifiedLineCount);
        };
        diff.LineActionInvoked += (_, e) => _ = e.Lines.Count;
        diff.SelectionChanged += (_, _) => _ = diff.SelectedLines.Count;
        var window = new Window { Width = 800, Height = 300, Content = diff };
        window.Show();
        Task diffReady = diff.WaitForDiffAsync();
        for (DateTime end = DateTime.UtcNow.AddSeconds(20); !diffReady.IsCompleted && DateTime.UtcNow < end;)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }

        if (!diffReady.IsCompletedSuccessfully || diff.Changes.Count == 0)
        {
            throw new InvalidOperationException("The diff editor did not compute its changes.");
        }

        diff.SetSelection([new DiffLine(DiffSide.Modified, 2)]);
        diff.ClearSelection();
        window.Close();
    }
}

/// <summary>The application: Fluent and the LoomEditor styles.</summary>
internal sealed class TestApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add(new LoomEditorStyles());
    }
}
