using LoomEditor.View.DiffEditor;

namespace LoomEditor.View.Tests.DiffEditor;

// Monaco registers these options in registrations.contribution.ts and has no test for them: all tests are new.
public class DiffDecorationOptionsTests
{
    [Fact]
    public void DiffDecorationOptions_ClassNamesAndFlags()
    {
        Assert.Equal("line-insert", DiffDecorationOptions.LineAddBackgroundWithIndicator.ClassName);
        Assert.True(DiffDecorationOptions.LineAddBackgroundWithIndicator.IsWholeLine);
        Assert.Equal("gutter-insert", DiffDecorationOptions.LineAddBackgroundWithIndicator.MarginClassName);
        Assert.Equal("insert-sign", DiffDecorationOptions.LineAddBackgroundWithIndicator.LinesDecorationsClassName);
        Assert.Equal("delete-sign", DiffDecorationOptions.LineDeleteBackgroundWithIndicator.LinesDecorationsClassName);
        Assert.Null(DiffDecorationOptions.LineAddBackground.LinesDecorationsClassName);
        Assert.Equal("gutter-delete", DiffDecorationOptions.LineDeleteBackground.MarginClassName);
        Assert.True(DiffDecorationOptions.AddDecoration.ShouldFillLineOnLineBreak);
        Assert.False(DiffDecorationOptions.AddDecoration.IsWholeLine);
        Assert.True(DiffDecorationOptions.WholeLineDeleteDecoration.IsWholeLine);
        Assert.Equal("char-delete", DiffDecorationOptions.DeleteDecoration.ClassName);
        Assert.Equal("char-insert diff-range-empty", DiffDecorationOptions.AddDecorationEmpty.ClassName);
        Assert.Equal("char-delete diff-range-empty", DiffDecorationOptions.DeleteDecorationEmpty.Description);
    }
}
