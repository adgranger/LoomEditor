using LoomEditor.Lsp.Client;
using LoomEditor.Lsp.Editor;
using LoomEditor.Lsp.JsonRpc;

namespace LoomEditor.Lsp.Tests.Editor;

public class LspRequestErrorsTests
{
    [Theory]
    [InlineData(JsonRpcErrorCodes.ContentModified)]
    [InlineData(JsonRpcErrorCodes.RequestCancelled)]
    [InlineData(JsonRpcErrorCodes.ServerCancelled)]
    public void IsBenign_ContentModified_RequestCancelled_ServerCancelled(int code)
    {
        Assert.True(LspRequestErrors.IsBenign(new JsonRpcException(code, "x")));
    }

    [Fact]
    public void IsBenign_ClientException()
    {
        Assert.True(LspRequestErrors.IsBenign(new LspClientException("stopped")));
    }

    [Fact]
    public void IsBenign_OtherJsonRpcError_False()
    {
        Assert.False(LspRequestErrors.IsBenign(new JsonRpcException(JsonRpcErrorCodes.InternalError, "x")));
    }

    [Fact]
    public void IsBenign_OtherException_False()
    {
        Assert.False(LspRequestErrors.IsBenign(new InvalidOperationException()));
    }
}
