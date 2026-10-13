using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model;

/// <summary>Ported from Monaco <c>editableTextModel.test.ts</c>: suite <c>CRLF edit normalization</c>.</summary>
public class EditableTextModelCrlfTests
{
    // Monaco: 'edit ending with \r followed by \n in buffer should strip trailing \r'
    [Fact]
    public void EditEndingWithCr_FollowedByLfInBuffer_StripsTrailingCr()
    {
        // Document: "abc\r\ndef\r\n"
        // Edit: Replace range (1,1)-(1,4) "abc" with "xyz\r"
        // The \r at end of replacement should be stripped since next char is \n
        var model = new TextModel("abc\r\ndef\r\n");
        model.SetEol(EndOfLineSequence.CRLF);

        Assert.Equal("\r\n", model.Eol);
        Assert.Equal(3, model.LineCount);
        Assert.Equal("abc", model.GetLineContent(1));
        Assert.Equal("def", model.GetLineContent(2));

        model.ApplyEdits([TextEdit.Replace(new Range(1, 1, 1, 4), "xyz\r")]);

        // The trailing \r should be stripped, so we get "xyz" not "xyz\r"
        Assert.Equal("xyz", model.GetLineContent(1));
        Assert.Equal("def", model.GetLineContent(2));
        Assert.Equal(3, model.LineCount);
    }

    // Monaco: 'edit ending with \r\n should NOT be modified'
    [Fact]
    public void EditEndingWithCrLf_IsNotModified()
    {
        var model = new TextModel("abc\r\ndef\r\n");
        model.SetEol(EndOfLineSequence.CRLF);

        model.ApplyEdits([TextEdit.Replace(new Range(1, 1, 1, 4), "xyz\r\n")]);

        // Should add a new line
        Assert.Equal("xyz", model.GetLineContent(1));
        Assert.Equal("", model.GetLineContent(2));
        Assert.Equal("def", model.GetLineContent(3));
        Assert.Equal(4, model.LineCount);
    }

    // Monaco: 'edit ending with \r NOT followed by \n should NOT be modified'
    [Fact]
    public void EditEndingWithCr_NotFollowedByLf_IsNotModified()
    {
        var model = new TextModel("abcdef");
        model.SetEol(EndOfLineSequence.CRLF);

        model.ApplyEdits([TextEdit.Replace(new Range(1, 1, 1, 4), "xyz\r")]);

        // Since the buffer uses CRLF, the lone \r is normalized to \r\n
        Assert.Equal(2, model.LineCount);
    }

    // Monaco: 'edit in LF buffer should NOT strip trailing \r'
    [Fact]
    public void EditInLfBuffer_DoesNotStripTrailingCr()
    {
        var model = new TextModel("abc\ndef\n");
        model.SetEol(EndOfLineSequence.LF);

        Assert.Equal("\n", model.Eol);
        Assert.Equal(3, model.LineCount);

        model.ApplyEdits([TextEdit.Replace(new Range(1, 1, 1, 4), "xyz\r")]);

        // The \r is normalized to \n (the buffer EOL)
        Assert.Equal(4, model.LineCount);
    }

    // Monaco: 'LSP include sorting scenario - edit ending with \r should be normalized'
    [Fact]
    public void LspIncludeSortingScenario_EditEndingWithCr_IsNormalized()
    {
        // Real-world scenario: replace lines 1-3 with reordered includes, the edit ending with a lone \r
        var model = new TextModel("#include \"a.h\"\r\n#include \"c.h\"\r\n#include \"b.h\"\r\n");
        model.SetEol(EndOfLineSequence.CRLF);

        Assert.Equal("\r\n", model.Eol);
        Assert.Equal(4, model.LineCount);
        Assert.Equal("#include \"a.h\"", model.GetLineContent(1));
        Assert.Equal("#include \"c.h\"", model.GetLineContent(2));
        Assert.Equal("#include \"b.h\"", model.GetLineContent(3));

        // Range covers: #include "a.h"\r\n#include "c.h"\r\n#include "b.h" (line 3 col 16 is just before the \r\n)
        model.ApplyEdits(
        [
            TextEdit.Replace(new Range(1, 1, 3, 16), "#include \"a.h\"\r\n#include \"b.h\"\r\n#include \"c.h\"\r"),
        ]);

        // The trailing \r should be stripped because the next char after the range is \n
        Assert.Equal(4, model.LineCount);
        Assert.Equal("#include \"a.h\"", model.GetLineContent(1));
        Assert.Equal("#include \"b.h\"", model.GetLineContent(2));
        Assert.Equal("#include \"c.h\"", model.GetLineContent(3));
        Assert.Equal("", model.GetLineContent(4));
    }
}
