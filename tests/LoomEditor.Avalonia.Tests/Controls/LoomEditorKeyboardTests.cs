using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using LoomEditor.Avalonia.Tests.TestHelpers;
using Editor = global::LoomEditor.Avalonia.Controls.LoomEditor;

namespace LoomEditor.Avalonia.Tests.Controls;

// The twin of the WinUI control: no upstream test, all tests are new.
public class LoomEditorKeyboardTests
{
    private static readonly RawInputModifiers CommandKey = OperatingSystem.IsMacOS() ? RawInputModifiers.Meta : RawInputModifiers.Control;

    private static (Editor Editor, Window Window) OpenFocused(string text)
    {
        var editor = new Editor { Text = text };
        Window window = EditorWindow.Open(editor);
        editor.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.True(editor.IsFocused, "the editor has no focus");
        return (editor, window);
    }

    [Fact]
    public async Task Typing_Hello_InsertsText()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window) = OpenFocused(string.Empty);

            window.KeyTextInput("hello");

            Assert.Equal("hello", editor.Model.GetValue());
            window.Close();
        });
    }

    [Fact]
    public async Task CtrlA_SelectsAll()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window) = OpenFocused("one\ntwo");

            window.KeyPress(Key.A, CommandKey, PhysicalKey.A, "a");

            Core.Text.Selection selection = Assert.Single(editor.Selections);
            Assert.False(selection.IsEmpty);
            Assert.Equal(1, selection.SelectionStartLineNumber);
            Assert.Equal(2, selection.PositionLineNumber);
            window.Close();
        });
    }

    [Fact]
    public async Task Enter_InsertsNewLineWithIndent()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window) = OpenFocused("    abc");
            editor.SetSelections([Core.Text.Selection.Collapsed(new Core.Text.Position(1, 8))]);

            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);

            Assert.Equal("    abc\n    ", editor.Model.GetValue());
            window.Close();
        });
    }

    [Fact]
    public async Task CopyPaste_RoundTrips()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (Editor editor, Window window) = OpenFocused("abc");
            Assert.NotNull(window.Clipboard);

            window.KeyPress(Key.A, CommandKey, PhysicalKey.A, "a");
            window.KeyPress(Key.C, CommandKey, PhysicalKey.C, "c");
            await Task.Delay(50);
            editor.SetSelections([Core.Text.Selection.Collapsed(new Core.Text.Position(1, 4))]);
            window.KeyPress(Key.V, CommandKey, PhysicalKey.V, "v");
            await Task.Delay(50);

            Assert.Equal("abcabc", editor.Model.GetValue());
            window.Close();
        });
    }

    [Fact]
    public async Task Tab_InsertsIndent_KeepsFocus()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window) = OpenFocused(string.Empty);

            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);

            Assert.Equal("    ", editor.Model.GetValue().Replace("\t", "    ", StringComparison.Ordinal));
            Assert.True(editor.IsFocused);
            window.Close();
        });
    }
}
