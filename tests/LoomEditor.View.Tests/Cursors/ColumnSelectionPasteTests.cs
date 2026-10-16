// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/columnSelectionPaste.test.ts (suite 'Column selection paste')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;

namespace LoomEditor.View.Tests.Cursors;

// Not ported: 'uses successive model lines when word wrap is enabled' (word wrap: phase 6), 'reports pasted ranges for
// cursors and multiline destination selections' (needs the onDidPaste editor event, P3-47), 'respects overtype and pads
// short rows' (overtype mode) and the nested suite 'clipboard metadata' (browser clipboard events; P3-47 has its own tests).
// 'responds to spread, full, and spread configuration transitions' leaves out the check of EditorOptions.multiCursorPaste.validate.
public class ColumnSelectionPasteTests
{
    private const string Block = "A\nBC\nD";

    // The overrides of Monaco's paste(editor, text, overrides): isBlock: undefined is IsBlock false, multicursorText: null is NoMulticursorText.
    private sealed record PasteOverrides(bool IsBlock = true, bool NoMulticursorText = false);

    private static void Paste(TestCodeEditor editor, string text = Block, PasteOverrides? overrides = null)
    {
        overrides ??= new PasteOverrides();
        editor.ViewModel.Paste(text, false, overrides.NoMulticursorText ? null : Strings.SplitLines(text), "keyboard", overrides.IsBlock);
    }

    private static void AssertPaste(
        string[] text,
        Selection[] selections,
        string[] expected,
        string? pastedText = null,
        int tabSize = 4,
        EditorCursorOptions? options = null,
        PasteOverrides? payload = null)
    {
        TestCodeEditor.WithTestCodeEditor(
            text,
            editor =>
            {
                editor.Model.UpdateOptions(new TextModelUpdateOptions(TabSize: tabSize));
                editor.ViewModel.SetSelections(null, selections);
                Paste(editor, pastedText ?? Block, payload);
                Assert.Equal(expected, editor.Model.GetLinesContent());
            },
            new TestCodeEditorOptions(CursorOptions: options ?? EditorCursorOptions.Default));
    }

    private static (string[] Text, List<Selection> Selections) Snapshot(TestCodeEditor editor) =>
        (editor.Model.GetLinesContent(), editor.ViewModel.GetSelections());

    private static void AssertSnapshot(string[] text, Selection[] selections, TestCodeEditor editor)
    {
        (string[] actualText, List<Selection> actualSelections) = Snapshot(editor);
        Assert.Equal(text, actualText);
        Assert.Equal(selections, actualSelections);
    }

    // Monaco: 'inserts successive rows from a single cursor'
    [Fact]
    public void InsertsSuccessiveRowsFromASingleCursor() =>
        AssertPaste(
            text: ["left--right", "left--right", "left--right"],
            selections: [new Selection(1, 5, 1, 5)],
            expected: ["leftA --right", "leftBC--right", "leftD --right"]);

    // Monaco: 'replaces a single-line selection with the first row'
    [Fact]
    public void ReplacesASingleLineSelectionWithTheFirstRow() =>
        AssertPaste(
            text: ["left--right", "left--right", "left--right"],
            selections: [new Selection(1, 5, 1, 7)],
            expected: ["leftA right", "leftBC--right", "leftD --right"]);

    // Monaco: 'uses the start of a reversed selection'
    [Fact]
    public void UsesTheStartOfAReversedSelection() =>
        AssertPaste(
            text: ["left--right", "left--right", "left--right"],
            selections: [new Selection(1, 7, 1, 5)],
            expected: ["leftA right", "leftBC--right", "leftD --right"]);

    // Monaco: 'replaces an equal-height continuous selection with the first row'
    [Fact]
    public void ReplacesAnEqualHeightContinuousSelectionWithTheFirstRow() =>
        AssertPaste(
            text: ["left--right", "left--right", "left--right"],
            selections: [new Selection(1, 5, 3, 7)],
            expected: ["leftA right", "    BC", "    D "]);

    // Monaco: 'replaces a reversed equal-height continuous selection'
    [Fact]
    public void ReplacesAReversedEqualHeightContinuousSelection() =>
        AssertPaste(
            text: ["left--right", "left--right", "left--right"],
            selections: [new Selection(3, 7, 1, 5)],
            expected: ["leftA right", "    BC", "    D "]);

    // Monaco: 'replaces a full-line selection including its final newline'
    [Fact]
    public void ReplacesAFullLineSelectionIncludingItsFinalNewline() =>
        AssertPaste(
            text: ["first", "second", "third", "unselected"],
            selections: [new Selection(1, 1, 4, 1)],
            expected: ["A unselected", "BC", "D "]);

    // Monaco: 'preserves the suffix when a reversed selection ends at column one'
    [Fact]
    public void PreservesTheSuffixWhenAReversedSelectionEndsAtColumnOne() =>
        AssertPaste(
            text: ["left--right", "second", "third", "unselected"],
            selections: [new Selection(4, 1, 1, 5)],
            expected: ["leftA unselected", "    BC", "    D "]);

    // Monaco: 'replaces a shorter continuous selection with the first row'
    [Fact]
    public void ReplacesAShorterContinuousSelectionWithTheFirstRow() =>
        AssertPaste(
            text: ["left--right", "left--right", "last"],
            selections: [new Selection(1, 5, 2, 7)],
            expected: ["leftA right", "lastBC", "    D "]);

    // Monaco: 'replaces a taller continuous selection with the first row'
    [Fact]
    public void ReplacesATallerContinuousSelectionWithTheFirstRow() =>
        AssertPaste(
            text: ["left--right", "middle", "middle", "left--right"],
            selections: [new Selection(1, 5, 4, 7)],
            expected: ["leftA right", "    BC", "    D "]);

    // Monaco: 'pads short and empty lines to the block column'
    [Fact]
    public void PadsShortAndEmptyLinesToTheBlockColumn() =>
        AssertPaste(
            text: ["left--right", "x", ""],
            selections: [new Selection(1, 5, 1, 5)],
            expected: ["leftA --right", "x   BC", "    D "]);

    // Monaco: 'appends missing rows without moving the existing suffix'
    [Fact]
    public void AppendsMissingRowsWithoutMovingTheExistingSuffix() =>
        AssertPaste(
            text: ["left--right"],
            selections: [new Selection(1, 5, 1, 5)],
            expected: ["leftA --right", "    BC", "    D "]);

    // Monaco: 'appends rows when the cursor is at the end of the file'
    [Fact]
    public void AppendsRowsWhenTheCursorIsAtTheEndOfTheFile() =>
        AssertPaste(
            text: ["left"],
            selections: [new Selection(1, 5, 1, 5)],
            expected: ["leftA ", "    BC", "    D "]);

    // Monaco: 'pastes into an empty document'
    [Fact]
    public void PastesIntoAnEmptyDocument() =>
        AssertPaste(
            text: [""],
            selections: [new Selection(1, 1, 1, 1)],
            expected: ["A ", "BC", "D "]);

    // Monaco: 'preserves empty and trailing copied rows'
    [Fact]
    public void PreservesEmptyAndTrailingCopiedRows() =>
        AssertPaste(
            text: ["left--right", "left--right", "left--right", "left--right"],
            selections: [new Selection(1, 5, 1, 5)],
            pastedText: "A\n\nD\n",
            expected: ["leftA--right", "left --right", "leftD--right", "left --right"]);

    // Monaco: 'pads short lines for empty and trailing copied rows'
    [Fact]
    public void PadsShortLinesForEmptyAndTrailingCopiedRows() =>
        AssertPaste(
            text: ["left", "x", "", "y"],
            selections: [new Selection(1, 5, 1, 5)],
            pastedText: "A\n\nD\n",
            expected: ["leftA", "x    ", "    D", "y    "]);

    // Monaco: 'appends empty and trailing copied rows'
    [Fact]
    public void AppendsEmptyAndTrailingCopiedRows() =>
        AssertPaste(
            text: ["left"],
            selections: [new Selection(1, 5, 1, 5)],
            pastedText: "A\n\nD\n",
            expected: ["leftA", "     ", "    D", "     "]);

    // Monaco: 'aligns using visible columns across tabs and spaces'
    [Fact]
    public void AlignsUsingVisibleColumnsAcrossTabsAndSpaces() =>
        AssertPaste(
            text: ["\tleft", "    left", "  \tleft"],
            selections: [new Selection(1, 2, 1, 2)],
            expected: ["\tA left", "    BCleft", "  \tD left"]);

    // Monaco: 'pads short lines using the configured tab size'
    [Fact]
    public void PadsShortLinesUsingTheConfiguredTabSize() =>
        AssertPaste(
            text: ["\t--left", "\t", ""],
            selections: [new Selection(1, 4, 1, 4)],
            tabSize: 8,
            expected: ["\t--A left", "\t  BC", "          D "]);

    // Monaco: 'pads using visible widths of graphemes and full-width characters'
    [Fact]
    public void PadsUsingVisibleWidthsOfGraphemesAndFullWidthCharacters() =>
        AssertPaste(
            text: ["left--right", "e\u0301", "\u4E2D", "\U0001F600"],
            selections: [new Selection(1, 5, 1, 5)],
            pastedText: "A\nB\nC\nD",
            expected: ["leftA--right", "e\u0301   B", "\u4E2D  C", "\U0001F600  D"]);

    // Monaco: 'uses grapheme boundaries and full-width character columns'
    [Fact]
    public void UsesGraphemeBoundariesAndFullWidthCharacterColumns() =>
        AssertPaste(
            text: ["\U0001F600left", "e\u0301xleft", "\u4E2Dleft"],
            selections: [new Selection(1, 3, 1, 3)],
            expected: ["\U0001F600A left", "e\u0301xBCleft", "\u4E2DD left"]);

    // Monaco: 'pads copied rows to the same visible width'
    [Fact]
    public void PadsCopiedRowsToTheSameVisibleWidth() =>
        AssertPaste(
            text: ["left--right", "left--right", "left--right", "left--right"],
            selections: [new Selection(1, 5, 1, 5)],
            pastedText: "\tX\n\u4E2D\n\U0001F600\ne\u0301",
            expected: ["left\tX--right", "left\u4E2D   --right", "left\U0001F600   --right", "lefte\u0301    --right"]);

    // Monaco: 'does not create cursors or truncate at the multi-cursor limit'
    [Fact]
    public void DoesNotCreateCursorsOrTruncateAtTheMultiCursorLimit() =>
        AssertPaste(
            text: ["left--right", "left--right", "left--right"],
            selections: [new Selection(1, 5, 1, 5)],
            options: EditorCursorOptions.Default with { MultiCursorLimit = 1 },
            expected: ["leftA --right", "leftBC--right", "leftD --right"]);

    // Monaco: 'distributes matching cursors in document order'
    [Fact]
    public void DistributesMatchingCursorsInDocumentOrder() =>
        AssertPaste(
            text: ["xx", "yy", "zz"],
            selections: [new Selection(3, 2, 3, 2), new Selection(1, 2, 1, 2), new Selection(2, 2, 2, 2)],
            expected: ["xAx", "yBCy", "zDz"]);

    // Monaco: 'distributes matching non-empty selections'
    [Fact]
    public void DistributesMatchingNonEmptySelections() =>
        AssertPaste(
            text: ["[one]", "[two]", "[three]"],
            selections: [new Selection(1, 2, 1, 5), new Selection(2, 2, 2, 5), new Selection(3, 2, 3, 7)],
            expected: ["[A]", "[BC]", "[D]"]);

    // Monaco: 'repeats the whole text when multiple cursor counts differ'
    [Fact]
    public void RepeatsTheWholeTextWhenMultipleCursorCountsDiffer() =>
        AssertPaste(
            text: ["xx", "yy", "zz"],
            selections: [new Selection(1, 2, 1, 2), new Selection(3, 2, 3, 2)],
            expected: ["xA", "BC", "Dx", "yy", "zA", "BC", "Dz"]);

    // Monaco: 'uses normal replacement for multiple destinations including a multiline selection'
    [Fact]
    public void UsesNormalReplacementForMultipleDestinationsIncludingAMultilineSelection() =>
        AssertPaste(
            text: ["xx", "yy", "zz", "[one]", "[two]"],
            selections: [new Selection(1, 2, 1, 2), new Selection(4, 2, 5, 5)],
            expected: ["xA", "BC", "Dx", "yy", "zz", "[A", "BC", "D]"]);

    // Monaco: 'retains ordinary trailing-newline handling when spreading to multiple cursors'
    [Fact]
    public void RetainsOrdinaryTrailingNewlineHandlingWhenSpreadingToMultipleCursors() =>
        AssertPaste(
            text: ["xx", "yy"],
            selections: [new Selection(1, 2, 1, 2), new Selection(2, 2, 2, 2)],
            pastedText: "A\nB\n",
            expected: ["xAx", "yBy"]);

    // Monaco: 'block mode respects multiCursorPaste full for multiple destinations without row metadata'
    [Fact]
    public void BlockModeRespectsMultiCursorPasteFullForMultipleDestinationsWithoutRowMetadata() =>
        AssertPaste(
            text: ["xx", "yy", "zz"],
            selections: [new Selection(1, 2, 1, 2), new Selection(2, 2, 2, 2), new Selection(3, 2, 3, 2)],
            options: EditorCursorOptions.Default with { MultiCursorPaste = MultiCursorPasteMode.Full },
            payload: new PasteOverrides(NoMulticursorText: true),
            expected: ["xA", "BC", "Dx", "yA", "BC", "Dy", "zA", "BC", "Dz"]);

    // Monaco: 'full mode retains single-cursor multiline pasting'
    [Fact]
    public void FullModeRetainsSingleCursorMultilinePasting() =>
        AssertPaste(
            text: ["left--right", "last"],
            selections: [new Selection(1, 5, 1, 5)],
            options: EditorCursorOptions.Default with { MultiCursorPaste = MultiCursorPasteMode.Full },
            expected: ["leftA", "BC", "D--right", "last"]);

    // Monaco: 'full mode retains existing multi-cursor metadata distribution'
    [Fact]
    public void FullModeRetainsExistingMultiCursorMetadataDistribution() =>
        AssertPaste(
            text: ["xx", "yy", "zz"],
            selections: [new Selection(1, 2, 1, 2), new Selection(2, 2, 2, 2), new Selection(3, 2, 3, 2)],
            options: EditorCursorOptions.Default with { MultiCursorPaste = MultiCursorPasteMode.Full },
            expected: ["xAx", "yBCy", "zDz"]);

    // Monaco: 'ordinary multiline copies spread into a rectangle at a single cursor'
    [Fact]
    public void OrdinaryMultilineCopiesSpreadIntoARectangleAtASingleCursor() =>
        AssertPaste(
            text: ["left--right", "last"],
            selections: [new Selection(1, 5, 1, 5)],
            payload: new PasteOverrides(),
            expected: ["leftA --right", "lastBC", "    D "]);

    // Monaco: 'missing block metadata still spreads multiline text at a single cursor'
    [Fact]
    public void MissingBlockMetadataStillSpreadsMultilineTextAtASingleCursor() =>
        AssertPaste(
            text: ["left--right", "last"],
            selections: [new Selection(1, 5, 1, 5)],
            payload: new PasteOverrides(NoMulticursorText: true),
            expected: ["leftA --right", "lastBC", "    D "]);

    // Monaco: 'preserves ordinary spread pasting without metadata'
    [Fact]
    public void PreservesOrdinarySpreadPastingWithoutMetadata() =>
        AssertPaste(
            text: ["xx", "yy", "zz"],
            selections: [new Selection(1, 2, 1, 2), new Selection(2, 2, 2, 2), new Selection(3, 2, 3, 2)],
            payload: new PasteOverrides(IsBlock: false, NoMulticursorText: true),
            expected: ["xAx", "yBCy", "zDz"]);

    // Monaco: 'preserves ordinary full pasting without metadata'
    [Fact]
    public void PreservesOrdinaryFullPastingWithoutMetadata() =>
        AssertPaste(
            text: ["xx", "yy"],
            selections: [new Selection(1, 2, 1, 2), new Selection(2, 2, 2, 2)],
            pastedText: "A\nB",
            options: EditorCursorOptions.Default with { MultiCursorPaste = MultiCursorPasteMode.Full },
            payload: new PasteOverrides(IsBlock: false, NoMulticursorText: true),
            expected: ["xA", "Bx", "yA", "By"]);

    // Monaco: 'does not edit a read-only document'
    [Fact]
    public void DoesNotEditAReadOnlyDocument() =>
        AssertPaste(
            text: ["left--right", "last"],
            selections: [new Selection(1, 5, 1, 5)],
            options: EditorCursorOptions.Default with { ReadOnly = true },
            expected: ["left--right", "last"]);

    // Monaco: 'preserves multi-cursor distribution through undo and redo in block mode'
    [Fact]
    public void PreservesMultiCursorDistributionThroughUndoAndRedoInBlockMode()
    {
        string[] text = ["xx", "yy", "zz"];
        TestCodeEditor.WithTestCodeEditor(
            text,
            editor =>
            {
                Selection[] selections = [new Selection(1, 2, 1, 2), new Selection(2, 2, 2, 2), new Selection(3, 2, 3, 2)];
                editor.ViewModel.SetSelections(null, selections);
                Paste(editor);
                string[] expectedText = ["xAx", "yBCy", "zDz"];
                Selection[] expectedSelections = [new Selection(1, 3, 1, 3), new Selection(2, 4, 2, 4), new Selection(3, 3, 3, 3)];
                AssertSnapshot(expectedText, expectedSelections, editor);

                Undo(editor);
                AssertSnapshot(text, selections, editor);

                Redo(editor);
                AssertSnapshot(expectedText, expectedSelections, editor);
            });
    }

    // Monaco: 'replaces a multiline selection and aligns later rows through undo and redo'
    [Fact]
    public void ReplacesAMultilineSelectionAndAlignsLaterRowsThroughUndoAndRedo()
    {
        string[] text = ["\tfirst", "middle", "last--suffix", "x", string.Empty];
        TestCodeEditor.WithTestCodeEditor(
            text,
            editor =>
            {
                editor.Model.UpdateOptions(new TextModelUpdateOptions(TabSize: 4));
                var selection = new Selection(3, 7, 1, 2);
                editor.ViewModel.SetSelections(null, [selection]);
                Paste(editor, "\nBC\n");
                string[] expectedText = ["\t  suffix", "x   BC", "      "];
                Selection[] expectedSelections = [new Selection(3, 7, 3, 7)];
                AssertSnapshot(expectedText, expectedSelections, editor);

                Undo(editor);
                AssertSnapshot(text, [selection], editor);

                Redo(editor);
                AssertSnapshot(expectedText, expectedSelections, editor);
            });
    }

    // Monaco: 'preserves CRLF line endings'
    [Fact]
    public void PreservesCrlfLineEndings()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["left", "x"],
            editor =>
            {
                editor.Model.SetEol(EndOfLineSequence.CRLF);
                editor.ViewModel.SetSelections(null, [Selection.Collapsed(new Position(1, 5))]);
                Paste(editor, "A\r\nBC\r\nD");
                Assert.Equal("leftA \r\nx   BC\r\n    D ", editor.Model.GetValue());
            });
    }

    // Monaco: 'leaves the cursor after rectangular padding on an empty trailing pasted row'
    [Fact]
    public void LeavesTheCursorAfterRectangularPaddingOnAnEmptyTrailingPastedRow()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["left", "left", "left"],
            editor =>
            {
                editor.ViewModel.SetSelections(null, [Selection.Collapsed(new Position(1, 5))]);
                Paste(editor, "A\nBC\n");
                AssertSnapshot(["leftA ", "leftBC", "left  "], [new Selection(3, 7, 3, 7)], editor);
            });
    }

    // Monaco: 'leaves the cursor after padding on an empty trailing pasted row'
    [Fact]
    public void LeavesTheCursorAfterPaddingOnAnEmptyTrailingPastedRow()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["left", "x", string.Empty],
            editor =>
            {
                editor.ViewModel.SetSelections(null, [Selection.Collapsed(new Position(1, 5))]);
                Paste(editor, "A\nBC\n");
                AssertSnapshot(["leftA ", "x   BC", "      "], [new Selection(3, 7, 3, 7)], editor);
            });
    }

    // Monaco: 'leaves the cursor after padding on an appended empty row'
    [Fact]
    public void LeavesTheCursorAfterPaddingOnAnAppendedEmptyRow()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["left"],
            editor =>
            {
                editor.ViewModel.SetSelections(null, [Selection.Collapsed(new Position(1, 5))]);
                Paste(editor, "A\nBC\n");
                AssertSnapshot(["leftA ", "    BC", "      "], [new Selection(3, 7, 3, 7)], editor);
            });
    }

    // Monaco: 'undo and redo preserve text and the original single selection'
    [Fact]
    public void UndoAndRedoPreserveTextAndTheOriginalSingleSelection()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["left--right", "x"],
            editor =>
            {
                var selection = new Selection(1, 7, 1, 5);
                editor.ViewModel.SetSelections(null, [selection]);
                Paste(editor);
                AssertSnapshot(["leftA right", "x   BC", "    D "], [new Selection(3, 7, 3, 7)], editor);

                Undo(editor);
                AssertSnapshot(["left--right", "x"], [selection], editor);

                Redo(editor);
                AssertSnapshot(["leftA right", "x   BC", "    D "], [new Selection(3, 7, 3, 7)], editor);
            });
    }

    // Monaco: 'changes only the row ranges in one content change'
    [Fact]
    public void ChangesOnlyTheRowRangesInOneContentChange()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["left--right", "x", string.Empty],
            editor =>
            {
                var changes = new List<List<(Range Range, string Text)>>();
                editor.Model.ContentChanged += (_, e) => changes.Add(e.Changes.Select(c => (c.Range, c.Text)).ToList());
                editor.ViewModel.SetSelections(null, [Selection.Collapsed(new Position(1, 5))]);
                Paste(editor);

                List<(Range Range, string Text)> change = Assert.Single(changes);
                Assert.Equal(
                    [(new Range(3, 1, 3, 1), "    D "), (new Range(2, 2, 2, 2), "   BC"), (new Range(1, 5, 1, 5), "A ")],
                    change);
            });
    }

    // Monaco: 'responds to spread, full, and spread configuration transitions'
    [Fact]
    public void RespondsToSpreadFullAndSpreadConfigurationTransitions()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["left", "x", string.Empty],
            editor =>
            {
                var results = new List<string[]>();
                Assert.Equal(MultiCursorPasteMode.Spread, editor.ViewModel.CursorOptions.MultiCursorPaste);
                foreach (MultiCursorPasteMode multiCursorPaste in new[] { MultiCursorPasteMode.Spread, MultiCursorPasteMode.Full, MultiCursorPasteMode.Spread })
                {
                    editor.ViewModel.SetCursorOptions(editor.ViewModel.CursorOptions with { MultiCursorPaste = multiCursorPaste });
                    editor.ViewModel.SetSelections(null, [Selection.Collapsed(new Position(1, 5))]);
                    Paste(editor);
                    results.Add(editor.Model.GetLinesContent());
                    editor.Model.Undo();
                }

                Assert.Equal(
                    [
                        ["leftA ", "x   BC", "    D "],
                        ["leftA", "BC", "D", "x", string.Empty],
                        ["leftA ", "x   BC", "    D "],
                    ],
                    results);
            });
    }
}
