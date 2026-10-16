// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursor.test.ts (suite 'Editor Controller', IME composition)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;

namespace LoomEditor.View.Tests.Cursors;

public class CompositionTests
{
    // Monaco: 'issue #9675: Undo/Redo adds a stop in between CHN Characters'
    [Fact]
    public void Issue9675UndoRedoAddsAStopInBetweenChnCharacters()
    {
        TestCodeEditor.WithTestCodeEditor(
            [],
            editor =>
            {
                TextModel model = editor.Model;
                var viewModel = editor.ViewModel;
                AssertCursor(viewModel, new Position(1, 1));

                // Typing sennsei in Japanese - Hiragana
                viewModel.Type("ｓ", "keyboard");
                viewModel.CompositionType("せ", 1, 0, 0);
                viewModel.CompositionType("せｎ", 1, 0, 0);
                viewModel.CompositionType("せん", 2, 0, 0);
                viewModel.CompositionType("せんｓ", 2, 0, 0);
                viewModel.CompositionType("せんせ", 3, 0, 0);
                viewModel.CompositionType("せんせ", 3, 0, 0);
                viewModel.CompositionType("せんせい", 3, 0, 0);
                viewModel.CompositionType("せんせい", 4, 0, 0);
                viewModel.CompositionType("せんせい", 4, 0, 0);
                viewModel.CompositionType("せんせい", 4, 0, 0);

                Assert.Equal("せんせい", model.GetLineContent(1));
                AssertCursor(viewModel, new Position(1, 5));

                Undo(editor);
                Assert.Equal(string.Empty, model.GetLineContent(1));
                AssertCursor(viewModel, new Position(1, 1));
            });
    }

    [Fact]
    public void EndComposition_AfterSteps_LeavesTheTextAndOneUndoStep()
    {
        TestCodeEditor.WithTestCodeEditor(
            string.Empty,
            editor =>
            {
                var viewModel = editor.ViewModel;

                viewModel.StartComposition();
                viewModel.CompositionType("に", 0, 0, 0);
                viewModel.CompositionType("にほ", 1, 0, 0);
                viewModel.EndComposition("keyboard");

                Assert.Equal("にほ", editor.Model.GetLineContent(1));
                AssertCursor(viewModel, new Position(1, 3));

                Undo(editor);
                Assert.Equal(string.Empty, editor.Model.GetLineContent(1));
            });
    }

    [Fact]
    public void CompositionType_EmptyTextWithPositionDelta_MovesTheCursorWithoutEditing()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["abc"],
            editor =>
            {
                var viewModel = editor.ViewModel;
                MoveTo(viewModel, 1, 3);

                viewModel.CompositionType(string.Empty, 0, 0, -1);

                Assert.Equal("abc", editor.Model.GetLineContent(1));
                AssertCursor(viewModel, new Position(1, 2));
            });
    }

    [Fact]
    public void CompositionType_TwoCursors_ReplacesAtBoth()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["a", "b"],
            editor =>
            {
                var viewModel = editor.ViewModel;
                viewModel.SetSelections("test", [new Selection(1, 2, 1, 2), new Selection(2, 2, 2, 2)]);

                viewModel.CompositionType("に", 1, 0, 0);

                Assert.Equal("に", editor.Model.GetLineContent(1));
                Assert.Equal("に", editor.Model.GetLineContent(2));
                AssertCursor(viewModel, [new Selection(1, 2, 1, 2), new Selection(2, 2, 2, 2)]);
            });
    }

    [Fact]
    public void CompositionType_NonEmptySelection_IsIgnored()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["abc"],
            editor =>
            {
                var viewModel = editor.ViewModel;
                viewModel.SetSelections("test", [new Selection(1, 1, 1, 3)]);

                viewModel.CompositionType("に", 0, 0, 0);

                Assert.Equal("abc", editor.Model.GetLineContent(1));
            });
    }

    [Fact]
    public void DeduceOutcome_PureInsertion_GivesTheInsertedText()
    {
        TextModel model = new("ab");
        var state = new CompositionState(model, [new Selection(1, 3, 1, 3)]);

        model.SetValue("abc");
        List<CompositionOutcome>? outcome = state.DeduceOutcome(model, [new Selection(1, 4, 1, 4)]);

        CompositionOutcome single = Assert.Single(outcome!);
        Assert.Equal("c", single.InsertedText);
        Assert.Equal(string.Empty, single.DeletedText);
        Assert.Equal(new Range(1, 3, 1, 4), single.InsertedTextRange);
    }

    [Fact]
    public void DeduceOutcome_SelectionSpanningLines_IsNull()
    {
        TextModel model = new("ab\ncd");
        var state = new CompositionState(model, [new Selection(1, 1, 2, 1)]);

        Assert.Null(state.DeduceOutcome(model, [new Selection(1, 1, 1, 1)]));
    }

    [Fact]
    public void Type_DuringComposition_DoesNotApplyEnterInterceptors()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["  ab"],
            editor =>
            {
                var viewModel = editor.ViewModel;
                MoveTo(viewModel, 1, 5);
                viewModel.Type("\n", "keyboard");
                string withEnter = editor.Model.GetLineContent(2);

                Undo(editor);
                viewModel.StartComposition();
                viewModel.Type("\n", "keyboard");
                string whileComposing = editor.Model.GetLineContent(2);

                Assert.Equal("  ", withEnter);
                Assert.Equal(string.Empty, whileComposing);
            });
    }
}
