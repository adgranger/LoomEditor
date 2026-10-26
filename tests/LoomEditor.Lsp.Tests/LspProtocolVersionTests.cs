namespace LoomEditor.Lsp.Tests;

public class LspProtocolVersionTests
{
    [Fact]
    public void Version_Is317()
    {
        Assert.Equal("3.17", LspProtocolVersion.Version);
    }
}
