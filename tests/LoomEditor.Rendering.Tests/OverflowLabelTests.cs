namespace LoomEditor.Rendering.Tests;

public class OverflowLabelTests
{
    [Theory]
    [InlineData(500, "Show more (500 chars)")]
    [InlineData(1023, "Show more (1023 chars)")]
    [InlineData(2048, "Show more (2.0 KB)")]
    [InlineData(15_000, "Show more (14.6 KB)")]
    [InlineData(3 * 1024 * 1024, "Show more (3.0 MB)")]
    public void Format_Count_UsesCharsKbOrMb(int count, string expected)
    {
        Assert.Equal(expected, OverflowLabel.Format(count));
    }
}
