using LoomEditor.Core.Model;
using LoomEditor.Core.Tests.Model.TestHelpers;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model;

/// <summary>
/// Tests of <see cref="TextModel.PushEditOperations"/>, <see cref="TextModel.Undo"/> and <see cref="TextModel.Redo"/>.
/// New tests, except the one marked with a <c>// Monaco:</c> comment.
/// </summary>
public class TextModelUndoTests
{
    private static void Push(TextModel model, Range range, string text) =>
        model.PushEditOperations(null, [new TextEdit(range, text)], null);

    private static void Type(TextModel model, int line, int column, string text) =>
        Push(model, new Range(line, column, line, column), text);

    [Fact]
    public void Undo_TwoPushesWithoutStackElement_RevertsBoth()
    {
        var model = TestTextModels.Create("abc");

        Type(model, 1, 4, "d");
        Type(model, 1, 5, "e");
        Assert.Equal("abcde", model.GetValue());

        model.Undo();

        Assert.Equal("abc", model.GetValue());
        Assert.False(model.CanUndo);
    }

    [Fact]
    public void Undo_TwoPushesWithStackElementBetween_NeedsTwoUndos()
    {
        var model = TestTextModels.Create("abc");

        Type(model, 1, 4, "d");
        model.PushStackElement();
        Type(model, 1, 5, "e");

        model.Undo();
        Assert.Equal("abcd", model.GetValue());
        Assert.True(model.CanUndo);

        model.Undo();
        Assert.Equal("abc", model.GetValue());
        Assert.False(model.CanUndo);
    }

    [Fact]
    public void PopStackElement_AfterPushStackElement_MergesNextEditIntoSameStep()
    {
        var model = TestTextModels.Create("abc");

        Type(model, 1, 4, "d");
        model.PushStackElement();
        model.PopStackElement();
        Type(model, 1, 5, "e");

        model.Undo();

        Assert.Equal("abc", model.GetValue());
        Assert.False(model.CanUndo);
    }

    [Fact]
    public void Undo_NothingPushed_ReturnsNullAndLeavesTextAlone()
    {
        var model = TestTextModels.Create("abc");

        Assert.Null(model.Undo());
        Assert.Null(model.Redo());
        Assert.Equal("abc", model.GetValue());
        Assert.Equal(1, model.VersionId);
    }

    [Fact]
    public void ApplyEdits_Plain_IsNotRecordedInUndoStack()
    {
        var model = TestTextModels.Create("abc");

        model.ApplyEdits([TextEdit.Insert(new Position(1, 4), "d")]);

        Assert.False(model.CanUndo);
    }

    [Fact]
    public void Redo_AfterUndo_RestoresTextAndAlternativeVersionId()
    {
        var model = TestTextModels.Create("abc");
        Type(model, 1, 4, "d");
        model.PushStackElement();
        Type(model, 1, 5, "e");
        string text = model.GetValue();
        int alternativeVersionId = model.AlternativeVersionId;

        model.Undo();
        model.Redo();

        Assert.Equal(text, model.GetValue());
        Assert.Equal(alternativeVersionId, model.AlternativeVersionId);
    }

    [Fact]
    public void Undo_ToInitialState_RestoresInitialAlternativeVersionId()
    {
        var model = TestTextModels.Create("abc");
        Assert.Equal(1, model.AlternativeVersionId);

        Type(model, 1, 4, "d");
        Assert.NotEqual(1, model.AlternativeVersionId);
        int versionIdAfterEdit = model.VersionId;

        model.Undo();

        Assert.Equal(1, model.AlternativeVersionId);
        Assert.True(model.VersionId > versionIdAfterEdit);
    }

    [Fact]
    public void Push_AfterUndo_ClearsRedo()
    {
        var model = TestTextModels.Create("abc");
        Type(model, 1, 4, "d");
        model.Undo();
        Assert.True(model.CanRedo);

        Type(model, 1, 4, "x");

        Assert.False(model.CanRedo);
        Assert.Null(model.Redo());
        Assert.Equal("abcx", model.GetValue());
    }

    [Fact]
    public void PushEol_Crlf_IsUndoableAndRedoable()
    {
        var model = TestTextModels.Create("a\nb\nc");

        model.PushEol(EndOfLineSequence.CRLF);
        Assert.Equal("\r\n", model.Eol);
        Assert.True(model.CanUndo);

        model.Undo();
        Assert.Equal("\n", model.Eol);
        Assert.Equal("a\nb\nc", model.GetValue());

        model.Redo();
        Assert.Equal("\r\n", model.Eol);
        Assert.Equal("a\r\nb\r\nc", model.GetValue());
    }

    [Fact]
    public void PushEol_SameEol_DoesNothing()
    {
        var model = TestTextModels.Create("a\nb");

        model.PushEol(EndOfLineSequence.LF);

        Assert.False(model.CanUndo);
        Assert.Equal(1, model.VersionId);
    }

    [Fact]
    public void Undo_EditThenEolChangeInOneStep_RevertsBoth()
    {
        var model = TestTextModels.Create("a\nb");

        Type(model, 1, 2, "x");
        model.PushEol(EndOfLineSequence.CRLF);
        Assert.Equal("ax\r\nb", model.GetValue());

        model.Undo();

        Assert.Equal("a\nb", model.GetValue());
        Assert.Equal("\n", model.Eol);
    }

    [Fact]
    public void Undo_Step_ReturnsBeforeCursorStateAndRedoReturnsAfterCursorState()
    {
        var model = TestTextModels.Create("abc");
        Selection[] before = [new Selection(1, 1, 1, 3)];
        Selection[] after = [Selection.Collapsed(new Position(1, 5))];
        IReadOnlyList<TextEdit>? computerInput = null;

        IReadOnlyList<Selection>? computed = model.PushEditOperations(
            before,
            [new TextEdit(new Range(1, 1, 1, 3), "wxyz")],
            inverse =>
            {
                computerInput = inverse;
                return after;
            });

        Assert.Same(after, computed);
        Assert.NotNull(computerInput);
        TextEdit inverseEdit = Assert.Single(computerInput);
        Assert.Equal(new Range(1, 1, 1, 5), inverseEdit.Range);
        Assert.Equal("ab", inverseEdit.Text);

        Assert.Same(before, model.Undo());
        Assert.Same(after, model.Redo());
    }

    [Fact]
    public void PushEditOperations_AppendedToOpenStep_KeepsFirstBeforeCursorStateAndLastAfterCursorState()
    {
        var model = TestTextModels.Create("abc");
        Selection[] before1 = [Selection.Collapsed(new Position(1, 4))];
        Selection[] after1 = [Selection.Collapsed(new Position(1, 5))];
        Selection[] before2 = [Selection.Collapsed(new Position(1, 5))];
        Selection[] after2 = [Selection.Collapsed(new Position(1, 6))];

        model.PushEditOperations(before1, [TextEdit.Insert(new Position(1, 4), "d")], _ => after1);
        model.PushEditOperations(before2, [TextEdit.Insert(new Position(1, 5), "e")], _ => after2);

        Assert.Same(before1, model.Undo());
        Assert.Same(after2, model.Redo());
    }

    [Fact]
    public void Undo_NoCursorState_ReturnsNull()
    {
        var model = TestTextModels.Create("abc");
        Type(model, 1, 4, "d");

        Assert.Null(model.Undo());
        Assert.Equal("abc", model.GetValue());
    }

    [Fact]
    public void Undo_Step_RaisesContentChangedWithIsUndoing()
    {
        var model = TestTextModels.Create("abc");
        Type(model, 1, 4, "d");
        var events = new List<ModelContentChangedEventArgs>();
        model.ContentChanged += (_, e) => events.Add(e);

        model.Undo();
        model.Redo();

        Assert.Equal(2, events.Count);
        Assert.True(events[0].IsUndoing);
        Assert.False(events[0].IsRedoing);
        Assert.True(events[1].IsRedoing);
        Assert.False(events[1].IsUndoing);

        // Plain edits after an undo are not flagged.
        events.Clear();
        Type(model, 1, 1, "z");
        Assert.False(events[0].IsUndoing);
        Assert.False(events[0].IsRedoing);
    }

    [Fact]
    public void SetValue_ClearsUndoStack()
    {
        var model = TestTextModels.Create("abc");
        Type(model, 1, 4, "d");
        model.Undo();
        Type(model, 1, 4, "e");
        model.Undo();
        Assert.True(model.CanRedo);

        model.SetValue("new");

        Assert.False(model.CanUndo);
        Assert.False(model.CanRedo);
    }

    [Fact]
    public void Undo_MultipleEditsInOnePush_RevertsAllOfThem()
    {
        var model = TestTextModels.Create("line1\nline2\nline3");

        model.PushEditOperations(
            null,
            [
                TextEdit.Insert(new Position(1, 1), "> "),
                TextEdit.Replace(new Range(2, 1, 2, 6), "LINE2"),
                TextEdit.Delete(new Range(3, 1, 3, 3)),
            ],
            null);
        Assert.Equal("> line1\nLINE2\nne3", model.GetValue());

        model.Undo();
        Assert.Equal("line1\nline2\nline3", model.GetValue());

        model.Redo();
        Assert.Equal("> line1\nLINE2\nne3", model.GetValue());
    }

    [Fact]
    public void Undo_ConsecutiveTyping_IsCompressedAndRevertedInOneStep()
    {
        var model = TestTextModels.Create(string.Empty);

        for (int i = 0; i < 100; i++)
        {
            Type(model, 1, i + 1, "x");
        }

        Assert.Equal(new string('x', 100), model.GetValue());

        model.Undo();

        Assert.Equal(string.Empty, model.GetValue());
    }

    // Monaco: 'issue #47733: Undo mangles unicode characters' (done with PushEditOperations)
    [Fact]
    public void Issue47733_UndoRedoWithSurrogatePairs_DoesNotMangleCharacters()
    {
        var model = TestTextModels.Create("'👁'");

        model.PushEditOperations(null, [Edit(1, 1, 1, 1, "\""), Edit(1, 2, 1, 2, "\"")], null);
        Assert.Equal("\"'\"👁'", model.GetValue(EndOfLinePreference.LF));

        model.PushStackElement();
        model.PushEditOperations(null, [Edit(1, 1, 1, 2, string.Empty), Edit(1, 3, 1, 4, string.Empty)], null);
        Assert.Equal("'👁'", model.GetValue(EndOfLinePreference.LF));

        model.Undo();
        Assert.Equal("\"'\"👁'", model.GetValue(EndOfLinePreference.LF));

        model.Undo();
        Assert.Equal("'👁'", model.GetValue(EndOfLinePreference.LF));

        model.Redo();
        Assert.Equal("\"'\"👁'", model.GetValue(EndOfLinePreference.LF));

        model.Redo();
        Assert.Equal("'👁'", model.GetValue(EndOfLinePreference.LF));

        static TextEdit Edit(int startLine, int startColumn, int endLine, int endColumn, string text) =>
            new(new Range(startLine, startColumn, endLine, endColumn), text);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void UndoAndRedoAll_RandomPushEditOperations_RestoreOriginalAndFinalText(int seed)
    {
        var random = new Random(seed);
        var model = TestTextModels.Create("hello\nworld\n\nfoo bar");
        string original = model.GetValue();

        for (int i = 0; i < 100; i++)
        {
            if (random.Next(3) == 0)
            {
                model.PushStackElement();
            }

            model.PushEditOperations(null, RandomEdits(random, model), null);
        }

        string final = model.GetValue();

        int undoCount = 0;
        while (model.CanUndo)
        {
            model.Undo();
            undoCount++;
            Assert.True(undoCount < 1000, $"seed {seed}: too many undo steps");
        }

        Assert.True(original == model.GetValue(), $"seed {seed}: text after undoing everything was '{model.GetValue()}'");
        Assert.Equal(1, model.AlternativeVersionId);

        int redoCount = 0;
        while (model.CanRedo)
        {
            model.Redo();
            redoCount++;
        }

        Assert.Equal(undoCount, redoCount);
        Assert.True(final == model.GetValue(), $"seed {seed}: text after redoing everything was '{model.GetValue()}'");
    }

    private static List<TextEdit> RandomEdits(Random random, TextModel model)
    {
        // Non-overlapping edits, one per line at most, so that the batch is always valid.
        var edits = new List<TextEdit>();
        int lineCount = model.LineCount;
        int firstLine = random.Next(1, lineCount + 1);
        int lastLine = Math.Min(lineCount, firstLine + random.Next(0, 3));
        for (int line = firstLine; line <= lastLine; line++)
        {
            if (random.Next(2) == 0)
            {
                continue;
            }

            int maxColumn = model.GetLineMaxColumn(line);
            int startColumn = random.Next(1, maxColumn + 1);
            int endColumn = random.Next(startColumn, maxColumn + 1);
            string text = random.Next(4) switch
            {
                0 => string.Empty,
                1 => "x",
                2 => "ab\ncd",
                _ => "👁 ",
            };
            edits.Add(new TextEdit(new Range(line, startColumn, line, endColumn), text));
        }

        return edits;
    }
}
