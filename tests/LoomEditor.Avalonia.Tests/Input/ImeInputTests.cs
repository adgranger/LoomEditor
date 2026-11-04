using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input.TextInput;
using Avalonia.Threading;
using LoomEditor.Avalonia.Tests.TestHelpers;
using LoomEditor.Core.Text;
using LoomEditor.Themes;
using LoomEditor.View.Controller;
using Editor = global::LoomEditor.Avalonia.Controls.LoomEditor;

namespace LoomEditor.Avalonia.Tests.Input;

// The twin of the WinUI IME (no upstream test, all tests are new). The client is called directly: no real IME.
public class ImeInputTests
{
    private static (Editor Editor, Window Window, TextInputMethodClient Client) OpenFocused(string text, Position caret)
    {
        var editor = new Editor { Text = text };
        Window window = EditorWindow.Open(editor);
        editor.Focus();
        editor.SetSelections([Selection.Collapsed(caret)]);
        Dispatcher.UIThread.RunJobs();
        Assert.True(editor.IsFocused, "the editor has no focus");
        return (editor, window, editor.ImeClient);
    }

    // The pixels of the composition border color on the last pixel row of line 1 (the thin underline).
    private static int CountUnderlinePixels(Editor editor, uint[] pixels)
    {
        ThemeColor color = EditorTheme.Dark.CompositionBorder;
        uint argb = ((uint)color.A << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B;
        int y = editor.ViewModel.FontInfo.LineHeight - 1;
        int count = 0;
        for (int x = 0; x < EditorWindow.Width; x++)
        {
            if (pixels[(y * EditorWindow.Width) + x] == argb)
            {
                count++;
            }
        }

        return count;
    }

    [Fact]
    public async Task Preedit_IsDrawnAsComposition()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window, TextInputMethodClient client) = OpenFocused("ab", new Position(1, 2));

            client.SetPreeditText("に", 1);
            int underline = CountUnderlinePixels(editor, EditorWindow.Capture(window));

            Assert.True(underline > 0, "no composition underline in the frame");
            // As WinUI (Monaco), the composed text is in the model while composing; nothing is committed until the end.
            CompositionFormat format = Assert.Single(editor.ViewModel.CompositionFormats);
            Assert.Equal(new Range(1, 2, 1, 3), format.Range);
            Assert.NotEqual(CompositionUnderline.None, format.Underline);
            Assert.Equal("aにb", editor.Model.GetValue());

            client.SetPreeditText(null);

            Assert.Empty(editor.ViewModel.CompositionFormats);
            Assert.Equal("ab", editor.Model.GetValue());
            Assert.Equal(0, CountUnderlinePixels(editor, EditorWindow.Capture(window)));
            window.Close();
        });
    }

    [Fact]
    public async Task Commit_InsertsText()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window, TextInputMethodClient client) = OpenFocused("ab", new Position(1, 2));

            client.SetPreeditText("に", 1);
            client.SetPreeditText("にほ", 2);
            window.KeyTextInput("日");
            client.SetPreeditText(null);

            Assert.Equal("a日b", editor.Model.GetValue());
            Assert.Empty(editor.ViewModel.CompositionFormats);
            Assert.Equal(Selection.Collapsed(new Position(1, 3)), editor.Selections[0]);
            window.Close();
        });
    }

    [Fact]
    public async Task CursorRectangle_FollowsCaret()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window, TextInputMethodClient client) = OpenFocused("one\ntwo\nthree", new Position(3, 3));
            EditorWindow.Capture(window);

            Rect rectangle = client.CursorRectangle;
            int lineHeight = editor.ViewModel.FontInfo.LineHeight;

            Assert.Same(editor, client.TextViewVisual);
            Assert.InRange(rectangle.Y, (2 * lineHeight) - 0.5, (3 * lineHeight) - 1);
            Assert.True(rectangle.X > editor.ViewModel.LayoutInfo.ContentLeft);
            window.Close();
        });
    }

    [Fact]
    public async Task SurroundingText_IsCaretLine()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window, TextInputMethodClient client) = OpenFocused("one\ntwo\nthree", new Position(2, 2));
            var raised = 0;
            client.SurroundingTextChanged += (_, _) => raised++;

            Assert.True(client.SupportsSurroundingText);
            Assert.True(client.SupportsPreedit);
            Assert.Equal("two", client.SurroundingText);
            Assert.Equal(new TextSelection(1, 1), client.Selection);

            editor.SetSelections([Selection.Collapsed(new Position(3, 4))]);

            Assert.Equal("three", client.SurroundingText);
            Assert.Equal(new TextSelection(3, 3), client.Selection);
            Assert.True(raised > 0);
            window.Close();
        });
    }

    [Fact]
    public async Task Preedit_Surrogates_NotSplit()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window, TextInputMethodClient client) = OpenFocused("ab", new Position(1, 2));

            // A cursor between the two halves of the pair goes after the pair.
            client.SetPreeditText("👍", 1);

            Assert.Equal("a👍b", editor.Model.GetValue());
            Assert.Equal(Selection.Collapsed(new Position(1, 4)), editor.Selections[0]);

            window.KeyTextInput("👍");

            Assert.Equal("a👍b", editor.Model.GetValue());
            Assert.Equal(Selection.Collapsed(new Position(1, 4)), editor.Selections[0]);
            window.Close();
        });
    }

    [Fact]
    public async Task ReadOnly_Commit_Ignored()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window, TextInputMethodClient client) = OpenFocused("ab", new Position(1, 2));
            editor.IsReadOnly = true;

            client.SetPreeditText("に", 1);
            window.KeyTextInput("日");
            client.SetPreeditText(null);

            Assert.Equal("ab", editor.Model.GetValue());
            Assert.Empty(editor.ViewModel.CompositionFormats);
            window.Close();
        });
    }
}
