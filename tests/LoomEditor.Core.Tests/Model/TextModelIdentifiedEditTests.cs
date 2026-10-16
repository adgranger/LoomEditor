using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Tests.Model.TestHelpers;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model;

/// <summary>Tests of the internal <c>PushEditOperations</c> overload (identified edits) and of <c>ResultingSelection</c>. New tests.</summary>
public class TextModelIdentifiedEditTests
{
    [Fact]
    public void PushEditOperations_TwoIdentifiedEdits_ComputerReceivesInverseOperationsWithTheSameIdentifiers()
    {
        var model = TestTextModels.Create("abc\ndef");
        IReadOnlyList<ValidEditOperation>? received = null;

        model.PushEditOperations(
            null,
            [
                new IdentifiedEditOperation(new Range(1, 1, 1, 1), "x", new EditOperationIdentifier(0, 0)),
                new IdentifiedEditOperation(new Range(2, 1, 2, 1), "y", new EditOperationIdentifier(1, 0)),
            ],
            inverse =>
            {
                received = inverse;
                return null;
            });

        Assert.NotNull(received);
        Assert.Equal(2, received.Count);
        Assert.Equal(new EditOperationIdentifier(0, 0), received[0].Identifier);
        Assert.Equal(new EditOperationIdentifier(1, 0), received[1].Identifier);
        Assert.Equal("xabc\nydef", model.GetValue());
    }

    [Fact]
    public void PushEditOperations_ForceMoveMarkers_MovesTheEndOfATrackedRange()
    {
        var plain = TestTextModels.Create("0123456789");
        string plainId = plain.SetTrackedRange(null, new Range(1, 3, 1, 5), TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges)!;
        var forced = TestTextModels.Create("0123456789");
        string forcedId = forced.SetTrackedRange(null, new Range(1, 3, 1, 5), TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges)!;

        plain.PushEditOperations(null, [new IdentifiedEditOperation(new Range(1, 5, 1, 5), "zz", ForceMoveMarkers: false)], null);
        forced.PushEditOperations(null, [new IdentifiedEditOperation(new Range(1, 5, 1, 5), "zz", ForceMoveMarkers: true)], null);

        // Without ForceMoveMarkers the end stays; with it, it moves.
        Assert.Equal(new Range(1, 3, 1, 5), plain.GetTrackedRange(plainId));
        Assert.Equal(new Range(1, 3, 1, 7), forced.GetTrackedRange(forcedId));
    }

    [Fact]
    public void Undo_RaisesRawContentChangedWithTheBeforeCursorState()
    {
        var model = TestTextModels.Create("abc");
        var before = new[] { new Selection(new Position(1, 1), new Position(1, 2)) };
        var after = new[] { new Selection(new Position(1, 5), new Position(1, 5)) };
        model.PushEditOperations(before, [new IdentifiedEditOperation(new Range(1, 4, 1, 4), "de")], _ => after);

        var received = new List<ModelRawContentChangedEventArgs>();
        model.RawContentChanged += (_, e) => received.Add(e);

        IReadOnlyList<Selection>? undone = model.Undo();
        IReadOnlyList<Selection>? redone = model.Redo();

        Assert.Equal(before, undone);
        Assert.Equal(after, redone);
        Assert.Equal(2, received.Count);
        Assert.True(received[0].IsUndoing);
        Assert.Equal(before, received[0].ResultingSelection);
        Assert.True(received[1].IsRedoing);
        Assert.Equal(after, received[1].ResultingSelection);
    }

    [Fact]
    public void ApplyEdits_Plain_HasNoResultingSelection()
    {
        var model = TestTextModels.Create("abc");
        ModelRawContentChangedEventArgs? received = null;
        model.RawContentChanged += (_, e) => received = e;

        model.ApplyEdits([TextEdit.Insert(new Position(1, 1), "x")]);

        Assert.NotNull(received);
        Assert.Null(received.ResultingSelection);
    }

    [Fact]
    public void PushEditOperations_PublicOverload_StillGivesTextEditsToTheComputer()
    {
        var model = TestTextModels.Create("abc");
        IReadOnlyList<TextEdit>? received = null;

        model.PushEditOperations(
            null,
            [TextEdit.Insert(new Position(1, 1), "x")],
            inverse =>
            {
                received = inverse;
                return null;
            });

        Assert.NotNull(received);
        TextEdit edit = Assert.Single(received);
        Assert.Equal(new Range(1, 1, 1, 2), edit.Range);
        Assert.Equal(string.Empty, edit.Text);
    }

    [Fact]
    public void PushEditOperations_ContentEvents_AreDeliveredAfterTheCursorStateComputer()
    {
        var model = TestTextModels.Create("abc");
        var steps = new List<string>();
        model.RawContentChanged += (_, e) => steps.Add(e.ResultingSelection is null ? "event" : "event with selection");

        model.PushEditOperations(
            null,
            [new IdentifiedEditOperation(new Range(1, 1, 1, 1), "x")],
            _ =>
            {
                steps.Add("computer");
                return null;
            });

        Assert.Equal(["computer", "event"], steps);
    }

    [Fact]
    public void Undo_TextAndEolChange_RaisesOneMergedEventWithTheResultingSelection()
    {
        var model = TestTextModels.Create("abc");
        model.PushEditOperations([Selection.Collapsed(new Position(1, 2))], [new TextEdit(new Range(1, 1, 1, 1), "x")], null);
        model.SetEol(EndOfLineSequence.CRLF);
        model.PushEol(EndOfLineSequence.CRLF);
        var events = new List<ModelRawContentChangedEventArgs>();
        model.RawContentChanged += (_, e) => events.Add(e);

        model.Undo();

        ModelRawContentChangedEventArgs raised = Assert.Single(events);
        Assert.True(raised.IsUndoing);
        Assert.Equal([Selection.Collapsed(new Position(1, 2))], raised.ResultingSelection);
    }
}
