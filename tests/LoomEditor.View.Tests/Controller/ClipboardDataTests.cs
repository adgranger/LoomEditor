using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Controller;
using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;

namespace LoomEditor.View.Tests.Controller;

// No Monaco test targets these functions directly (Monaco tests them through the clipboard events): all tests are new.
public class ClipboardDataTests
{
    [Fact]
    public void GenerateDataToCopy_TwoCursors_CopiesOneTextPerCursor()
    {
        TestCodeEditor.WithTestCodeEditor(
            "ab\r\ncd",
            editor =>
            {
                editor.ViewModel.SetSelections("test", [new Selection(1, 1, 1, 2), new Selection(2, 1, 2, 2)]);

                (ClipboardDataToCopy data, ClipboardStoredMetadata metadata) = ClipboardUtils.GenerateDataToCopyAndStoreInMemory(editor.ViewModel);

                Assert.Equal(["a", "c"], data.MulticursorText);
                Assert.Equal("a\r\nc", data.Text);
                Assert.Equal(["a", "c"], metadata.MulticursorText);
                Assert.False(data.IsFromEmptySelection);
                Assert.False(data.IsBlock);
                Assert.Equal(1, metadata.Version);
                Assert.NotNull(metadata.Id);
                Assert.Null(metadata.Mode);
            });
    }

    [Fact]
    public void GetPlainTextToCopy_TwoCursorsOnAnLfModel_ReturnsOneTextPerCursor()
    {
        TestCodeEditor.WithTestCodeEditor(
            "ab\ncd",
            editor =>
            {
                (IReadOnlyList<Range> ranges, string? text, IReadOnlyList<string>? texts) =
                    editor.ViewModel.GetPlainTextToCopy([new Range(2, 1, 2, 2), new Range(1, 1, 1, 2)], true, true);

                Assert.Null(text);
                Assert.Equal(["a", "c"], texts);
                Assert.Equal([new Range(1, 1, 1, 2), new Range(2, 1, 2, 2)], ranges);
            });
    }

    [Fact]
    public void GenerateDataToCopy_EmptySelection_CopiesTheWholeLineWithItsLineBreak()
    {
        TestCodeEditor.WithTestCodeEditor(
            "a\nbb\nc",
            editor =>
            {
                MoveTo(editor.ViewModel, 2, 2);

                (ClipboardDataToCopy data, _) = ClipboardUtils.GenerateDataToCopyAndStoreInMemory(editor.ViewModel);

                Assert.Equal("bb\r\n", data.Text);
                Assert.True(data.IsFromEmptySelection);
                Assert.Null(data.MulticursorText);
                Assert.Equal([new Range(2, 1, 2, 3)], data.SourceRanges);
            });
    }

    [Fact]
    public void GenerateDataToCopy_EmptySelectionWithEmptySelectionClipboardOff_CopiesNothing()
    {
        TestCodeEditor.WithTestCodeEditor(
            "a\nbb\nc",
            editor =>
            {
                MoveTo(editor.ViewModel, 2, 2);

                (ClipboardDataToCopy data, _) = ClipboardUtils.GenerateDataToCopyAndStoreInMemory(editor.ViewModel);

                Assert.Equal(string.Empty, data.Text);
                Assert.False(data.IsFromEmptySelection);
                Assert.Empty(data.SourceRanges);
            },
            new TestCodeEditorOptions(CursorOptions: EditorCursorOptions.Default with { EmptySelectionClipboard = false }));
    }

    [Fact]
    public void GenerateDataToCopy_AfterAColumnSelection_IsBlock()
    {
        TestCodeEditor.WithTestCodeEditor(
            "abc\ndef",
            editor =>
            {
                ViewModel vm = editor.ViewModel;
                MoveTo(vm, 1, 1, false);
                CoreNavigationCommands.ColumnSelect.RunCoreEditorCommand(vm, new ColumnSelectCommandArgs(new Position(2, 3), new Position(2, 3), 3, true));

                (ClipboardDataToCopy data, ClipboardStoredMetadata metadata) = ClipboardUtils.GenerateDataToCopyAndStoreInMemory(vm);

                Assert.True(data.IsBlock);
                Assert.True(metadata.IsBlock);
                Assert.Equal(["ab", "de"], data.MulticursorText);
            });
    }

    [Fact]
    public void InMemoryClipboardMetadataManager_Get_ReturnsTheMetadataForTheSameTextAndForgetsForAnotherText()
    {
        var manager = new InMemoryClipboardMetadataManager();
        var metadata = new ClipboardStoredMetadata(1, "id", true, false, null, null);
        manager.Set("text", metadata);

        Assert.Same(metadata, manager.Get("text"));
        Assert.Null(manager.Get("other"));
        Assert.Null(manager.Get("text"));
    }

    [Fact]
    public void Paste_AfterCopyingAnEmptySelection_InsertsTheLineAboveTheCursor()
    {
        TestCodeEditor.WithTestCodeEditor(
            "one\ntwo\nthree",
            editor =>
            {
                ViewModel vm = editor.ViewModel;
                MoveTo(vm, 1, 2);
                (ClipboardDataToCopy data, ClipboardStoredMetadata metadata) = ClipboardUtils.GenerateDataToCopyAndStoreInMemory(vm);
                Assert.True(metadata.IsFromEmptySelection);

                MoveTo(vm, 3, 2);
                vm.Paste(data.Text, metadata.IsFromEmptySelection, metadata.MulticursorText);

                // The model normalizes the pasted CRLF to its own EOL.
                Assert.Equal("one\ntwo\none\nthree", editor.Model.GetValue());
                AssertCursor(vm, new Position(4, 2));
            });
    }
}
