using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LoomEditor.Avalonia.Controls.Find;
using LoomEditor.Avalonia.Tests.TestHelpers;
using Editor = global::LoomEditor.Avalonia.Controls.LoomEditor;

namespace LoomEditor.Avalonia.Tests.Controls;

// The twin of the WinUI control: no upstream test, all tests are new.
public class FindWidgetTests
{
    private static readonly RawInputModifiers CommandKey = OperatingSystem.IsMacOS() ? RawInputModifiers.Meta : RawInputModifiers.Control;

    private static (Editor Editor, Window Window, FindWidget Widget) OpenFocused(string text)
    {
        var editor = new Editor { Text = text, MinimapEnabled = false };
        Window window = EditorWindow.Open(editor);
        editor.Focus();
        Dispatcher.UIThread.RunJobs();
        FindWidget widget = editor.GetVisualDescendants().OfType<FindWidget>().Single();
        return (editor, window, widget);
    }

    private static T Part<T>(FindWidget widget, string name)
        where T : Control => widget.GetVisualDescendants().OfType<T>().Single(control => control.Name == name);

    private static void Press(Window window, Key key, RawInputModifiers modifiers = RawInputModifiers.None, PhysicalKey? physicalKey = null, string? text = null)
    {
        window.KeyPress(key, modifiers, physicalKey ?? PhysicalKey.None, text);
        Dispatcher.UIThread.RunJobs();
    }

    [Fact]
    public async Task CtrlF_OpensWidget_FocusesFindBox()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor _, Window window, FindWidget widget) = OpenFocused("hello world");
            Assert.False(widget.IsVisible);

            Press(window, Key.F, CommandKey, PhysicalKey.F, "f");

            Assert.True(widget.IsVisible);
            Assert.True(Part<TextBox>(widget, "FindTextBox").IsFocused);
            window.Close();
        });
    }

    [Fact]
    public async Task TypingInFindBox_HighlightsMatches_ShowsCount()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor _, Window window, FindWidget widget) = OpenFocused("a1 a1 a1 b");
            Press(window, Key.F, CommandKey, PhysicalKey.F, "f");

            Part<TextBox>(widget, "FindTextBox").Text = "a1";
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("1 of 3", Part<TextBlock>(widget, "MatchesCountText").Text);
            window.Close();
        });
    }

    [Fact]
    public async Task Enter_GoesToNextMatch()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window, FindWidget widget) = OpenFocused("a1 a1 a1 b");
            Press(window, Key.F, CommandKey, PhysicalKey.F, "f");
            Part<TextBox>(widget, "FindTextBox").Text = "a1";
            Dispatcher.UIThread.RunJobs();
            Core.Text.Selection first = editor.Selections[0];

            // Typing only counts the matches: the first Enter selects the first one.
            Press(window, Key.Enter);
            Press(window, Key.Enter);

            Core.Text.Selection second = editor.Selections[0];
            Assert.NotEqual(first, second);
            Assert.Equal(4, second.SelectionStartColumn);
            Assert.Equal("2 of 3", Part<TextBlock>(widget, "MatchesCountText").Text);
            window.Close();
        });
    }

    [Fact]
    public async Task Escape_ClosesAndFocusesEditor()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window, FindWidget widget) = OpenFocused("hello");
            Press(window, Key.F, CommandKey, PhysicalKey.F, "f");
            Assert.True(widget.IsVisible);

            Press(window, Key.Escape);

            Assert.False(widget.IsVisible);
            Assert.True(editor.IsFocused);
            window.Close();
        });
    }

    [Fact]
    public async Task ReplaceAll_ReplacesEveryMatch()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window, FindWidget widget) = OpenFocused("cat dog cat");
            Press(window, Key.H, CommandKey, PhysicalKey.H, "h");
            Part<TextBox>(widget, "FindTextBox").Text = "cat";
            Part<TextBox>(widget, "ReplaceTextBox").Text = "bird";
            Dispatcher.UIThread.RunJobs();

            Part<Button>(widget, "ReplaceAllButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("bird dog bird", editor.Model.GetValue());
            window.Close();
        });
    }

    [Fact]
    public async Task RegexToggle_InvalidPattern_ShowsNoResults()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor _, Window window, FindWidget widget) = OpenFocused("a(b");
            Press(window, Key.F, CommandKey, PhysicalKey.F, "f");
            Part<TextBox>(widget, "FindTextBox").Text = "(";
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("1 of 1", Part<TextBlock>(widget, "MatchesCountText").Text);

            ToggleButton regex = Part<ToggleButton>(widget, "RegexToggle");
            regex.IsChecked = true;
            regex.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("No results", Part<TextBlock>(widget, "MatchesCountText").Text);
            window.Close();
        });
    }
}
