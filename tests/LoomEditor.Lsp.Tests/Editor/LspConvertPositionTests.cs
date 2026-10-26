using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Lsp.Editor;
using LoomEditor.Lsp.Protocol;

namespace LoomEditor.Lsp.Tests.Editor;

public class LspConvertPositionTests
{
    private static LspPosition Lsp(int line, int character) => new() { Line = line, Character = character };

    [Fact]
    public void ToLsp_Position_IsZeroBased()
    {
        LspPosition p = LspConvert.ToLsp(new Position(3, 5));

        Assert.Equal((2, 4), (p.Line, p.Character));
    }

    [Fact]
    public void ToLsp_Range()
    {
        LspRange r = LspConvert.ToLsp(new Range(1, 2, 3, 4));

        Assert.Equal((0, 1, 2, 3), (r.Start.Line, r.Start.Character, r.End.Line, r.End.Character));
    }

    [Fact]
    public void ToPosition_RoundTrips()
    {
        TextModel model = new("abc\ndef");

        Assert.Equal(new Position(2, 3), LspConvert.ToPosition(LspConvert.ToLsp(new Position(2, 3)), model));
    }

    [Fact]
    public void ToPosition_CharacterBeyondLine_ClampedToLineEnd()
        => Assert.Equal(new Position(1, 4), LspConvert.ToPosition(Lsp(0, 99), new TextModel("abc\ndef")));

    [Fact]
    public void ToPosition_LineBeyondDocument_ClampedToEnd()
        => Assert.Equal(new Position(2, 4), LspConvert.ToPosition(Lsp(50, 0), new TextModel("abc\ndef")));

    [Fact]
    public void ToPosition_Negative_ClampedToStart()
        => Assert.Equal(new Position(1, 1), LspConvert.ToPosition(Lsp(-3, -2), new TextModel("abc")));

    [Fact]
    public void ToRange_Swapped_Normalized()
    {
        LspRange r = new() { Start = Lsp(1, 2), End = Lsp(0, 1) };

        Assert.Equal(new Range(1, 2, 2, 3), LspConvert.ToRange(r, new TextModel("abc\ndef")));
    }

    [Fact]
    public void ToPosition_Utf16Surrogates_CharacterIsUtf16Index()
        => Assert.Equal(new Position(1, 4), LspConvert.ToPosition(Lsp(0, 3), new TextModel("a😀b")));
}
