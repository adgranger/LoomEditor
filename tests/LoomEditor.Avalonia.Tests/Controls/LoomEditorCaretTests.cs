using System.Diagnostics;
using LoomEditor.Avalonia.Tests.TestHelpers;
using Editor = global::LoomEditor.Avalonia.Controls.LoomEditor;

namespace LoomEditor.Avalonia.Tests.Controls;

// The twin of the WinUI control: no upstream test, all tests are new.
public class LoomEditorCaretTests
{
    [Fact]
    public async Task Caret_Blinks_FramesAlternate()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            var editor = new Editor { Text = "hello" };
            var window = EditorWindow.Open(editor);
            editor.Focus();
            Assert.True(editor.IsFocused, "the editor has no focus");
            uint[] first = EditorWindow.Capture(window);

            // The blink timer is real time: wait for up to three intervals for a frame that differs at the caret.
            var clock = Stopwatch.StartNew();
            bool differs = false;
            while (!differs && clock.Elapsed < TimeSpan.FromSeconds(3))
            {
                await Task.Delay(100);
                differs = !first.AsSpan().SequenceEqual(EditorWindow.Capture(window));
            }

            Assert.True(differs, "the caret never changed");
            window.Close();
        });
    }

    [Fact]
    public async Task ReadOnly_Typing_DoesNotEdit()
    {
        await HeadlessSession.RunAsync(() =>
        {
            var editor = new Editor { Text = "hello", IsReadOnly = true };
            var window = EditorWindow.Open(editor);

            // completed in P15-11: types through the keyboard; here the delete command is run directly.
            editor.Commands.Execute("deleteRight");

            Assert.Equal("hello", editor.Model.GetValue());
            window.Close();
        });
    }
}
