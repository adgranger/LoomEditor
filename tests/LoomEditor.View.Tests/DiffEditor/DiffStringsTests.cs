using LoomEditor.View.DiffEditor;

namespace LoomEditor.View.Tests.DiffEditor;

public sealed class DiffStringsTests
{
    [Fact]
    public void DiffStrings_BlockAction_OneLine() =>
        Assert.Equal("Stage block, line 12", DiffStrings.BlockAction("Stage block", 12, 12));

    [Fact]
    public void DiffStrings_BlockAction_Lines() =>
        Assert.Equal("Stage block, lines 12 to 15", DiffStrings.BlockAction("Stage block", 12, 15));

    [Fact]
    public void DiffStrings_SelectLine_Deleted() =>
        Assert.Equal("Select deleted line 7", DiffStrings.SelectLine(modified: false, 7));

    [Fact]
    public void DiffStrings_SelectLine_Added() =>
        Assert.Equal("Select added line 7", DiffStrings.SelectLine(modified: true, 7));
}
