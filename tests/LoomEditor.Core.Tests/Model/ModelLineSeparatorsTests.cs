using LoomEditor.Core.Model;
using LoomEditor.Core.Tests.Model.TestHelpers;

namespace LoomEditor.Core.Tests.Model;

/// <summary>Ported from Monaco <c>model.test.ts</c>, suite <c>Editor Model - Model Line Separators</c>.</summary>
public class ModelLineSeparatorsTests
{
    // U+2028 LINE SEPARATOR, built from its code point: the C# compiler (and some editors) treat the raw character as a line break.
    private static readonly string Ls = ((char)0x2028).ToString();

    private readonly TextModel _model = TestTextModels.Create(
        "My First Line" + Ls + "\t\tMy Second Line" + "\n" + "    Third Line" + Ls + string.Empty + "\r\n" + "1");

    // Monaco: 'model getValue'
    [Fact]
    public void ModelGetValue() =>
        Assert.Equal("My First Line" + Ls + "\t\tMy Second Line\n    Third Line" + Ls + "\n1", _model.GetValue());

    // Monaco: 'model lines'
    [Fact]
    public void ModelLines() => Assert.Equal(3, _model.LineCount);

    // Monaco: 'Bug 13333:Model should line break on lonely CR too'
    [Fact]
    public void Bug13333_ModelShouldLineBreakOnLonelyCrToo()
    {
        var model = TestTextModels.Create("Hello\rWorld!\r\nAnother line");
        Assert.Equal(3, model.LineCount);
        Assert.Equal("Hello\r\nWorld!\r\nAnother line", model.GetValue());
    }
}
