using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Tests.Model.TestHelpers;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model;

/// <summary>Ported from Monaco <c>model.test.ts</c>, suite <c>Editor Model - Model</c>.</summary>
public class ModelTests
{
    private const string Line1 = "My First Line";
    private const string Line2 = "\t\tMy Second Line";
    private const string Line3 = "    Third Line";
    private const string Line4 = "";
    private const string Line5 = "1";

    private readonly TextModel _model = TestTextModels.Create(
        Line1 + "\r\n" + Line2 + "\n" + Line3 + "\n" + Line4 + "\r\n" + Line5);

    // Monaco: Editor Model - Model: withEventCapturing (the raw event, or null when none was raised)
    private ModelRawContentChangedEventArgs? CaptureRawEvent(Action callback)
    {
        ModelRawContentChangedEventArgs? captured = null;
        int count = 0;
        void Handler(object? sender, ModelRawContentChangedEventArgs e)
        {
            count++;
            captured = e;
        }

        _model.RawContentChanged += Handler;
        callback();
        _model.RawContentChanged -= Handler;
        Assert.True(count <= 1, "Unexpected second event");
        return captured;
    }

    private static void AssertRawEvent(ModelRawContentChangedEventArgs? actual, int versionId, params ModelRawChange[] expected)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected, actual.Changes);
        Assert.Equal(versionId, actual.VersionId);
        Assert.False(actual.IsUndoing);
        Assert.False(actual.IsRedoing);
    }

    // --------- insert text

    // Monaco: 'model getValue'
    [Fact]
    public void ModelGetValue() =>
        Assert.Equal("My First Line\n\t\tMy Second Line\n    Third Line\n\n1", _model.GetValue());

    // Monaco: 'model insert empty text'
    [Fact]
    public void ModelInsertEmptyText()
    {
        _model.ApplyEdits([TextEdit.Insert(new Position(1, 1), string.Empty)]);
        Assert.Equal(5, _model.LineCount);
        Assert.Equal("My First Line", _model.GetLineContent(1));
    }

    // Monaco: 'model insert text without newline 1'
    [Fact]
    public void ModelInsertTextWithoutNewline1()
    {
        _model.ApplyEdits([TextEdit.Insert(new Position(1, 1), "foo ")]);
        Assert.Equal(5, _model.LineCount);
        Assert.Equal("foo My First Line", _model.GetLineContent(1));
    }

    // Monaco: 'model insert text without newline 2'
    [Fact]
    public void ModelInsertTextWithoutNewline2()
    {
        _model.ApplyEdits([TextEdit.Insert(new Position(1, 3), " foo")]);
        Assert.Equal(5, _model.LineCount);
        Assert.Equal("My foo First Line", _model.GetLineContent(1));
    }

    // Monaco: 'model insert text with one newline'
    [Fact]
    public void ModelInsertTextWithOneNewline()
    {
        _model.ApplyEdits([TextEdit.Insert(new Position(1, 3), " new line\nNo longer")]);
        Assert.Equal(6, _model.LineCount);
        Assert.Equal("My new line", _model.GetLineContent(1));
        Assert.Equal("No longer First Line", _model.GetLineContent(2));
    }

    // Monaco: 'model insert text with two newlines'
    [Fact]
    public void ModelInsertTextWithTwoNewlines()
    {
        _model.ApplyEdits([TextEdit.Insert(new Position(1, 3), " new line\nOne more line in the middle\nNo longer")]);
        Assert.Equal(7, _model.LineCount);
        Assert.Equal("My new line", _model.GetLineContent(1));
        Assert.Equal("One more line in the middle", _model.GetLineContent(2));
        Assert.Equal("No longer First Line", _model.GetLineContent(3));
    }

    // Monaco: 'model insert text with many newlines'
    [Fact]
    public void ModelInsertTextWithManyNewlines()
    {
        _model.ApplyEdits([TextEdit.Insert(new Position(1, 3), "\n\n\n\n")]);
        Assert.Equal(9, _model.LineCount);
        Assert.Equal("My", _model.GetLineContent(1));
        Assert.Equal(string.Empty, _model.GetLineContent(2));
        Assert.Equal(string.Empty, _model.GetLineContent(3));
        Assert.Equal(string.Empty, _model.GetLineContent(4));
        Assert.Equal(" First Line", _model.GetLineContent(5));
    }

    // --------- insert text eventing

    // Monaco: 'model insert empty text does not trigger eventing'
    [Fact]
    public void ModelInsertEmptyTextDoesNotTriggerEventing()
    {
        var e = CaptureRawEvent(() => _model.ApplyEdits([TextEdit.Insert(new Position(1, 1), string.Empty)]));
        Assert.Null(e);
    }

    // Monaco: 'model insert text without newline eventing'
    [Fact]
    public void ModelInsertTextWithoutNewlineEventing()
    {
        var e = CaptureRawEvent(() => _model.ApplyEdits([TextEdit.Insert(new Position(1, 1), "foo ")]));
        AssertRawEvent(e, 2, new ModelRawLineChanged(1, 1));
    }

    // Monaco: 'model insert text with one newline eventing'
    [Fact]
    public void ModelInsertTextWithOneNewlineEventing()
    {
        var e = CaptureRawEvent(() => _model.ApplyEdits([TextEdit.Insert(new Position(1, 3), " new line\nNo longer")]));
        AssertRawEvent(e, 2, new ModelRawLineChanged(1, 1), new ModelRawLinesInserted(2, 2, 1));
    }

    // --------- delete text

    // Monaco: 'model delete empty text'
    [Fact]
    public void ModelDeleteEmptyText()
    {
        _model.ApplyEdits([TextEdit.Delete(new Range(1, 1, 1, 1))]);
        Assert.Equal(5, _model.LineCount);
        Assert.Equal("My First Line", _model.GetLineContent(1));
    }

    // Monaco: 'model delete text from one line'
    [Fact]
    public void ModelDeleteTextFromOneLine()
    {
        _model.ApplyEdits([TextEdit.Delete(new Range(1, 1, 1, 2))]);
        Assert.Equal(5, _model.LineCount);
        Assert.Equal("y First Line", _model.GetLineContent(1));
    }

    // Monaco: 'model delete text from one line 2'
    [Fact]
    public void ModelDeleteTextFromOneLine2()
    {
        _model.ApplyEdits([TextEdit.Insert(new Position(1, 1), "a")]);
        Assert.Equal("aMy First Line", _model.GetLineContent(1));

        _model.ApplyEdits([TextEdit.Delete(new Range(1, 2, 1, 4))]);
        Assert.Equal(5, _model.LineCount);
        Assert.Equal("a First Line", _model.GetLineContent(1));
    }

    // Monaco: 'model delete all text from a line'
    [Fact]
    public void ModelDeleteAllTextFromALine()
    {
        _model.ApplyEdits([TextEdit.Delete(new Range(1, 1, 1, 14))]);
        Assert.Equal(5, _model.LineCount);
        Assert.Equal(string.Empty, _model.GetLineContent(1));
    }

    // Monaco: 'model delete text from two lines'
    [Fact]
    public void ModelDeleteTextFromTwoLines()
    {
        _model.ApplyEdits([TextEdit.Delete(new Range(1, 4, 2, 6))]);
        Assert.Equal(4, _model.LineCount);
        Assert.Equal("My Second Line", _model.GetLineContent(1));
    }

    // Monaco: 'model delete text from many lines'
    [Fact]
    public void ModelDeleteTextFromManyLines()
    {
        _model.ApplyEdits([TextEdit.Delete(new Range(1, 4, 3, 5))]);
        Assert.Equal(3, _model.LineCount);
        Assert.Equal("My Third Line", _model.GetLineContent(1));
    }

    // Monaco: 'model delete everything'
    [Fact]
    public void ModelDeleteEverything()
    {
        _model.ApplyEdits([TextEdit.Delete(new Range(1, 1, 5, 2))]);
        Assert.Equal(1, _model.LineCount);
        Assert.Equal(string.Empty, _model.GetLineContent(1));
    }

    // --------- delete text eventing

    // Monaco: 'model delete empty text does not trigger eventing'
    [Fact]
    public void ModelDeleteEmptyTextDoesNotTriggerEventing()
    {
        var e = CaptureRawEvent(() => _model.ApplyEdits([TextEdit.Delete(new Range(1, 1, 1, 1))]));
        Assert.Null(e);
    }

    // Monaco: 'model delete text from one line eventing'
    [Fact]
    public void ModelDeleteTextFromOneLineEventing()
    {
        var e = CaptureRawEvent(() => _model.ApplyEdits([TextEdit.Delete(new Range(1, 1, 1, 2))]));
        AssertRawEvent(e, 2, new ModelRawLineChanged(1, 1));
    }

    // Monaco: 'model delete all text from a line eventing'
    [Fact]
    public void ModelDeleteAllTextFromALineEventing()
    {
        var e = CaptureRawEvent(() => _model.ApplyEdits([TextEdit.Delete(new Range(1, 1, 1, 14))]));
        AssertRawEvent(e, 2, new ModelRawLineChanged(1, 1));
    }

    // Monaco: 'model delete text from two lines eventing'
    [Fact]
    public void ModelDeleteTextFromTwoLinesEventing()
    {
        var e = CaptureRawEvent(() => _model.ApplyEdits([TextEdit.Delete(new Range(1, 4, 2, 6))]));
        AssertRawEvent(e, 2, new ModelRawLineChanged(1, 1), new ModelRawLinesDeleted(2, 2, 1));
    }

    // Monaco: 'model delete text from many lines eventing'
    [Fact]
    public void ModelDeleteTextFromManyLinesEventing()
    {
        var e = CaptureRawEvent(() => _model.ApplyEdits([TextEdit.Delete(new Range(1, 4, 3, 5))]));
        AssertRawEvent(e, 2, new ModelRawLineChanged(1, 1), new ModelRawLinesDeleted(2, 3, 1));
    }

    // --------- getValueInRange

    // Monaco: 'getValueInRange'
    [Fact]
    public void GetValueInRange()
    {
        Assert.Equal(string.Empty, _model.GetValueInRange(new Range(1, 1, 1, 1)));
        Assert.Equal("M", _model.GetValueInRange(new Range(1, 1, 1, 2)));
        Assert.Equal("y", _model.GetValueInRange(new Range(1, 2, 1, 3)));
        Assert.Equal("My First Line", _model.GetValueInRange(new Range(1, 1, 1, 14)));
        Assert.Equal("My First Line\n", _model.GetValueInRange(new Range(1, 1, 2, 1)));
        Assert.Equal("My First Line\n\t", _model.GetValueInRange(new Range(1, 1, 2, 2)));
        Assert.Equal("My First Line\n\t\t", _model.GetValueInRange(new Range(1, 1, 2, 3)));
        Assert.Equal("My First Line\n\t\tMy Second Line", _model.GetValueInRange(new Range(1, 1, 2, 17)));
        Assert.Equal("My First Line\n\t\tMy Second Line\n", _model.GetValueInRange(new Range(1, 1, 3, 1)));
        Assert.Equal("My First Line\n\t\tMy Second Line\n    Third Line\n", _model.GetValueInRange(new Range(1, 1, 4, 1)));
    }

    // --------- getValueLengthInRange

    // Monaco: 'getValueLengthInRange'
    [Fact]
    public void GetValueLengthInRange()
    {
        Assert.Equal(0, _model.GetValueLengthInRange(new Range(1, 1, 1, 1)));
        Assert.Equal(1, _model.GetValueLengthInRange(new Range(1, 1, 1, 2)));
        Assert.Equal(1, _model.GetValueLengthInRange(new Range(1, 2, 1, 3)));
        Assert.Equal("My First Line".Length, _model.GetValueLengthInRange(new Range(1, 1, 1, 14)));
        Assert.Equal("My First Line\n".Length, _model.GetValueLengthInRange(new Range(1, 1, 2, 1)));
        Assert.Equal("My First Line\n\t".Length, _model.GetValueLengthInRange(new Range(1, 1, 2, 2)));
        Assert.Equal("My First Line\n\t\t".Length, _model.GetValueLengthInRange(new Range(1, 1, 2, 3)));
        Assert.Equal("My First Line\n\t\tMy Second Line".Length, _model.GetValueLengthInRange(new Range(1, 1, 2, 17)));
        Assert.Equal("My First Line\n\t\tMy Second Line\n".Length, _model.GetValueLengthInRange(new Range(1, 1, 3, 1)));
        Assert.Equal("My First Line\n\t\tMy Second Line\n    Third Line\n".Length, _model.GetValueLengthInRange(new Range(1, 1, 4, 1)));
    }

    // --------- setValue

    // Monaco: 'setValue eventing'
    [Fact]
    public void SetValueEventing()
    {
        var e = CaptureRawEvent(() => _model.SetValue("new value"));
        AssertRawEvent(e, 2, new ModelRawFlush());
    }

    // Monaco: 'issue #46342: Maintain edit operation order in applyEdits'
    [Fact]
    public void Issue46342_MaintainEditOperationOrderInApplyEdits()
    {
        var res = _model.ApplyEdits(
            [
                new TextEdit(new Range(2, 1, 2, 1), "a"),
                new TextEdit(new Range(1, 1, 1, 1), "b"),
            ],
            computeUndoEdits: true);

        Assert.Equal(new Range(2, 1, 2, 2), res[0].Range);
        Assert.Equal(new Range(1, 1, 1, 2), res[1].Range);
    }

    // --------- new tests (not in Monaco)

    [Fact]
    public void SetEol_Changed_RaisesRawEolChangedBeforeContentChanged()
    {
        var order = new List<string>();
        ModelRawContentChangedEventArgs? raw = null;
        _model.RawContentChanged += (_, e) =>
        {
            order.Add("raw");
            raw = e;
        };
        _model.ContentChanged += (_, _) => order.Add("content");

        _model.SetEol(EndOfLineSequence.CRLF);

        Assert.Equal(["raw", "content"], order);
        AssertRawEvent(raw, 2, new ModelRawEolChanged());
        Assert.NotNull(raw);
        Assert.True(raw.ContainsEvent(RawContentChangedType.EolChanged));
        Assert.False(raw.ContainsEvent(RawContentChangedType.Flush));
    }

    [Fact]
    public void ApplyEdits_ReplaceMoreLinesThanInserted_RaisesChangedThenDeleted()
    {
        var e = CaptureRawEvent(() => _model.ApplyEdits([TextEdit.Replace(new Range(1, 4, 4, 1), "x\n")]));
        AssertRawEvent(e, 2, new ModelRawLineChanged(2, 2), new ModelRawLineChanged(1, 1), new ModelRawLinesDeleted(3, 4, 2));
    }

    [Fact]
    public void ModelRawLinesInserted_ToLineNumbers_AreDerivedFromCount()
    {
        var inserted = new ModelRawLinesInserted(2, 5, 3);
        Assert.Equal(4, inserted.ToLineNumber);
        Assert.Equal(7, inserted.ToLineNumberPostEdit);
    }
}
