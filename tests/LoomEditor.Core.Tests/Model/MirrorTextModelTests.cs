using LoomEditor.Core.Model;

namespace LoomEditor.Core.Tests.Model;

public class MirrorTextModelTests
{
    private static MirrorTextModel Create(string text, string eol = "\n") => new([.. text.Split(eol)], eol, 1);

    private static ModelContentChange Change(int startLine, int startColumn, int endLine, int endColumn, string text) =>
        new(new Range(startLine, startColumn, endLine, endColumn), 0, 0, text);

    [Fact]
    public void MirrorTextModel_AppliesChangesInOrder_AndTracksVersion()
    {
        MirrorTextModel model = Create("one\ntwo\nthree");

        // Delete across lines, then insert a multi-line text, in one event (applied in the given order).
        model.OnEvents(new MirrorModelChangedEvent(
            [Change(1, 2, 2, 3, string.Empty), Change(1, 2, 1, 2, "X\nY\r\nZ")],
            string.Empty,
            7));

        Assert.Equal("oX\nY\nZo\nthree", model.GetText());
        Assert.Equal(7, model.Version);
    }

    [Fact]
    public void OnEvents_NewEol_JoinsTheLinesWithIt()
    {
        MirrorTextModel model = Create("a\nb");

        model.OnEvents(new MirrorModelChangedEvent([], "\r\n", 2));

        Assert.Equal("a\r\nb", model.GetText());
    }

    [Fact]
    public void OnEvents_AfterLineStartsWereComputed_KeepsThemInSync()
    {
        var model = new LineStartsMirror(["ab", "cd"], "\n", 1);
        Assert.Equal(6, model.TotalLength()); // (2 + 1) + (2 + 1)

        model.OnEvents(new MirrorModelChangedEvent(
            [Change(1, 2, 2, 2, "Q\nRS\nT")], // "a" + "Q" / "RS" / "T" + "d"
            string.Empty,
            2));

        Assert.Equal("aQ\nRS\nTd", model.GetText());
        Assert.Equal(("aQ".Length + 1) + ("RS".Length + 1) + ("Td".Length + 1), model.TotalLength());
    }

    [Fact]
    public void Dispose_ClearsTheLines()
    {
        MirrorTextModel model = Create("a\nb");

        model.Dispose();

        Assert.Equal(string.Empty, model.GetText());
    }

    // Exposes the protected line starts like the worker's model will.
    private sealed class LineStartsMirror(List<string> lines, string eol, int versionId) : MirrorTextModel(lines, eol, versionId)
    {
        public int TotalLength()
        {
            EnsureLineStarts();
            return _lineStarts!.GetTotalSum();
        }
    }
}
