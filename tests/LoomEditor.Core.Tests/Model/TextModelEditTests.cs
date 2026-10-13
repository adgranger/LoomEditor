using LoomEditor.Core.Model;
using LoomEditor.Core.Tests.Model.TestHelpers;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model;

/// <summary>
/// Ported from Monaco <c>textModel.test.ts</c> (<c>TextModel.mightContainRTL</c>: <c>setValue resets</c>) plus new tests
/// of <c>ApplyEdits</c>, <c>SetValue</c>, <c>SetEol</c> and <see cref="TextModel.ContentChanged"/>.
/// </summary>
public class TextModelEditTests
{
    // Monaco: 'setValue resets 1'
    [Fact]
    public void SetValueResets1()
    {
        var model = TestTextModels.Create("hello world!");
        Assert.False(model.MightContainRtl);
        model.SetValue("Hello,\nזוהי עובדה מבוססת שדעתו");
        Assert.True(model.MightContainRtl);
    }

    // Monaco: 'setValue resets 2'
    [Fact]
    public void SetValueResets2()
    {
        var model = TestTextModels.Create("Hello,\nهناك حقيقة مثبتة منذ زمن طويل");
        Assert.True(model.MightContainRtl);
        model.SetValue("hello world!");
        Assert.False(model.MightContainRtl);
    }

    [Fact]
    public void ApplyEdits_InsertDeleteReplace_UpdatesTextAndVersions()
    {
        var model = TestTextModels.Create("hello\nworld");
        var events = new List<ModelContentChangedEventArgs>();
        model.ContentChanged += (_, e) => events.Add(e);

        model.ApplyEdits([TextEdit.Insert(new Position(1, 6), "!")]);
        Assert.Equal("hello!\nworld", model.GetValue());
        Assert.Equal(2, model.VersionId);
        Assert.Equal(2, model.AlternativeVersionId);

        model.ApplyEdits([TextEdit.Delete(new Range(1, 1, 1, 3))]);
        Assert.Equal("llo!\nworld", model.GetValue());
        Assert.Equal(3, model.VersionId);

        model.ApplyEdits([TextEdit.Replace(new Range(2, 1, 2, 6), "there"), TextEdit.Insert(new Position(1, 1), ">")]);
        Assert.Equal(">llo!\nthere", model.GetValue());
        Assert.Equal(4, model.VersionId);
        Assert.Equal(4, model.AlternativeVersionId);

        Assert.Equal(3, events.Count);
        Assert.Equal([2, 3, 4], events.Select(e => e.VersionId));
        Assert.Equal(2, events[2].Changes.Count);
        Assert.All(events, e => Assert.False(e.IsFlush || e.IsEolChange || e.IsUndoing || e.IsRedoing));
    }

    [Fact]
    public void ApplyEdits_NothingChanges_RaisesNoEventAndKeepsVersion()
    {
        var model = TestTextModels.Create("abc");
        int count = 0;
        model.ContentChanged += (_, _) => count++;

        model.ApplyEdits([]);
        model.ApplyEdits([TextEdit.Insert(new Position(1, 2), string.Empty)]);

        Assert.Equal(0, count);
        Assert.Equal(1, model.VersionId);
    }

    [Fact]
    public void ApplyEdits_HandlerReadsValue_SeesNewText()
    {
        var model = TestTextModels.Create("abc");
        string? seen = null;
        model.ContentChanged += (_, _) => seen = model.GetValue();

        model.ApplyEdits([TextEdit.Insert(new Position(1, 4), "d")]);

        Assert.Equal("abcd", seen);
    }

    [Fact]
    public void ApplyEdits_ComputeUndoEdits_InverseRestoresText()
    {
        var model = TestTextModels.Create("one\ntwo\nthree");
        IReadOnlyList<TextEdit> undo = model.ApplyEdits(
            [TextEdit.Replace(new Range(1, 2, 2, 3), "X\nY\nZ"), TextEdit.Delete(new Range(3, 1, 3, 4))],
            computeUndoEdits: true);

        Assert.NotEqual("one\ntwo\nthree", model.GetValue());
        model.ApplyEdits(undo);
        Assert.Equal("one\ntwo\nthree", model.GetValue());
    }

    [Fact]
    public void ApplyEdits_TrailingLoneCrBeforeCrlf_IsStripped()
    {
        var model = new TextModel("a\r\nb", DefaultEndOfLine.CRLF);
        model.ApplyEdits([TextEdit.Insert(new Position(1, 2), "x\r")]);
        Assert.Equal("ax\r\nb", model.GetValue());
    }

    [Fact]
    public void ApplyEdits_RandomBatches_MirrorFollowsModel()
    {
        const int seed = 1234;
        var random = new Random(seed);
        var model = TestTextModels.Create("line one\nline two\r\nline three\n\nend");
        var mirror = new MirrorText(model);

        for (int batch = 0; batch < 200; batch++)
        {
            string message = $"seed={seed} batch={batch}";
            model.ApplyEdits(RandomEdits(model, random));
            Assert.True(model.GetValue() == mirror.Text, message);
            Assert.True(model.VersionId == mirror.VersionId, message);
        }
    }

    [Fact]
    public void SetValue_RaisesOneFlushEvent()
    {
        var model = TestTextModels.Create("abc");
        var mirror = new MirrorText(model);
        var events = new List<ModelContentChangedEventArgs>();
        model.ContentChanged += (_, e) => events.Add(e);

        model.SetValue("x\ny");

        ModelContentChangedEventArgs e = Assert.Single(events);
        Assert.True(e.IsFlush);
        Assert.Equal(2, model.VersionId);
        Assert.Equal("x\ny", mirror.Text);
        Assert.Equal(2, model.LineCount);
    }

    [Fact]
    public void SetEol_LfToCrlf_RaisesEolChangeEvent()
    {
        var model = TestTextModels.Create("a\nb");
        var mirror = new MirrorText(model);
        var events = new List<ModelContentChangedEventArgs>();
        model.ContentChanged += (_, e) => events.Add(e);

        model.SetEol(EndOfLineSequence.CRLF);

        Assert.Equal("\r\n", model.Eol);
        Assert.Equal(["a", "b"], model.GetLinesContent());
        ModelContentChangedEventArgs e = Assert.Single(events);
        Assert.True(e.IsEolChange);
        Assert.Equal("\r\n", e.Eol);
        Assert.Equal("a\r\nb", mirror.Text);
        Assert.Equal(2, model.VersionId);

        model.SetEol(EndOfLineSequence.CRLF);
        Assert.Single(events);
    }

    // Sorted random offsets paired up as ranges: the ranges never overlap (they may touch).
    private static List<TextEdit> RandomEdits(TextModel model, Random random)
    {
        int count = random.Next(0, 4);
        int length = model.GetValueLength();
        var offsets = new List<int>();
        for (int i = 0; i < count * 2; i++)
        {
            offsets.Add(random.Next(0, length + 1));
        }

        offsets.Sort();

        string[] texts = ["", "a", "xyz", "\n", "q\nr", "\r\n", "a\r\nb\nc"];
        var edits = new List<TextEdit>();
        for (int i = 0; i < count; i++)
        {
            Position start = model.GetPositionAt(offsets[2 * i]);
            Position end = model.GetPositionAt(offsets[(2 * i) + 1]);
            edits.Add(new TextEdit(
                new Range(start.LineNumber, start.Column, end.LineNumber, end.Column),
                texts[random.Next(texts.Length)]));
        }

        return edits;
    }
}
