using LoomEditor.Core.Undo;

namespace LoomEditor.Core.Tests.Undo;

// Monaco has no test file for textChange.ts (it is tested through undo); these are LoomEditor additions.
public class TextChangeTests
{
    [Fact]
    public void Properties_Replace_ComputeLengthsAndEnds()
    {
        var change = new TextChange(5, "abc", 7, "wxyz");

        Assert.Equal(3, change.OldLength);
        Assert.Equal(8, change.OldEnd);
        Assert.Equal(4, change.NewLength);
        Assert.Equal(11, change.NewEnd);
    }

    [Fact]
    public void ToString_Insert_EscapesNewLines()
    {
        Assert.Equal("(insert@3 \"a\\r\\nb\")", new TextChange(3, "", 3, "a\r\nb").ToString());
    }

    [Fact]
    public void ToString_Delete_EscapesNewLines()
    {
        Assert.Equal("(delete@3 \"a\\nb\")", new TextChange(3, "a\nb", 3, "").ToString());
    }

    [Fact]
    public void ToString_Replace_ShowsBothTexts()
    {
        Assert.Equal("(replace@1 \"x\\n\" with \"y\\r\")", new TextChange(1, "x\n", 1, "y\r").ToString());
    }
}
