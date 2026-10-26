using System.Text.Json;
using LoomEditor.Core.Markers;
using LoomEditor.Core.Model;
using LoomEditor.Lsp.Editor;
using LoomEditor.Lsp.Protocol;

namespace LoomEditor.Lsp.Tests.Editor;

public class LspConvertDiagnosticsTests
{
    private static LspRange R(int l1, int c1, int l2, int c2)
        => new() { Start = new LspPosition { Line = l1, Character = c1 }, End = new LspPosition { Line = l2, Character = c2 } };

    private static LspDiagnostic Diag(LspRange? range = null, JsonElement? code = null, int[]? tags = null)
        => new() { Range = range ?? R(0, 0, 0, 1), Message = "m", Code = code, Tags = tags };

    private static JsonElement Json(string json) => JsonElement.Parse(json);

    private static MarkerData Marker(LspDiagnostic d, string text = "abcdefghij") => LspConvert.ToMarker(d, new TextModel(text), null);

    [Fact]
    public void Severity_AllValues()
    {
        Assert.Equal(MarkerSeverity.Error, LspConvert.ToSeverity(1));
        Assert.Equal(MarkerSeverity.Warning, LspConvert.ToSeverity(2));
        Assert.Equal(MarkerSeverity.Info, LspConvert.ToSeverity(3));
        Assert.Equal(MarkerSeverity.Hint, LspConvert.ToSeverity(4));
        Assert.Equal(MarkerSeverity.Error, LspConvert.ToSeverity(null));
        Assert.Equal(MarkerSeverity.Error, LspConvert.ToSeverity(0));
        Assert.Equal(MarkerSeverity.Error, LspConvert.ToSeverity(99));
    }

    [Fact]
    public void Marker_Range_ToOneBased()
    {
        MarkerData m = Marker(Diag(R(0, 2, 0, 5)));

        Assert.Equal((1, 3, 1, 6), (m.StartLineNumber, m.StartColumn, m.EndLineNumber, m.EndColumn));
    }

    [Fact]
    public void Marker_RangeBeyondText_Clamped()
    {
        MarkerData m = Marker(Diag(R(0, 1, 9, 50)), "abc");

        Assert.Equal((1, 2, 1, 4), (m.StartLineNumber, m.StartColumn, m.EndLineNumber, m.EndColumn));
    }

    [Fact]
    public void Marker_Code_String() => Assert.Equal("CS0001", Marker(Diag(code: Json("\"CS0001\""))).Code);

    [Fact]
    public void Marker_Code_Integer() => Assert.Equal("2", Marker(Diag(code: Json("2"))).Code);

    [Fact]
    public void Marker_Code_Decimal_Invariant() => Assert.Equal("2.5", LspConvert.CodeToString(Json("2.5")));

    [Theory]
    [InlineData("{}")]
    [InlineData("[1]")]
    [InlineData("true")]
    public void Marker_Code_Other_Null(string json) => Assert.Null(Marker(Diag(code: Json(json))).Code);

    [Fact]
    public void Marker_Source_Message_Version()
    {
        LspDiagnostic d = Diag();
        d.Source = "csharp";
        MarkerData m = LspConvert.ToMarker(d, new TextModel("abc"), 7);

        Assert.Equal(("csharp", "m", 7), (m.Source, m.Message, m.ModelVersionId));
    }

    [Fact]
    public void Marker_Tags()
    {
        Assert.Equal([MarkerTag.Unnecessary, MarkerTag.Deprecated], Marker(Diag(tags: [1, 2, 9])).Tags);
        Assert.Null(Marker(Diag(tags: [9])).Tags);
        Assert.Null(Marker(Diag()).Tags);
    }

    [Fact]
    public void Marker_SwappedRange_Normalized()
    {
        MarkerData m = Marker(Diag(R(0, 5, 0, 2)));

        Assert.Equal((3, 6), (m.StartColumn, m.EndColumn));
    }
}
