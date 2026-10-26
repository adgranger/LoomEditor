using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Text;
using LoomEditor.Lsp.Editor;
using LoomEditor.Lsp.FakeServer;
using LoomEditor.Lsp.JsonRpc;
using LoomEditor.Lsp.Protocol;
using LoomEditor.Lsp.Tests.TestHelpers;

namespace LoomEditor.Lsp.Tests.Editor;

public class LspHoverProviderTests
{
    private const string Text = "let value = 1;\nsecond value";

    private static Task<Hover?> HoverAsync(ProviderRig rig, Position position, CancellationToken token = default)
        => new LspHoverProvider(rig.Client, ProviderRig.Uri).ProvideHoverAsync(rig.Model, position, token);

    [Fact]
    public async Task Hover_OnWord_ReturnsMarkdownAndRange()
    {
        await using ProviderRig rig = await ProviderRig.StartFakeAsync(Text);

        Hover? hover = await HoverAsync(rig, new Position(1, 5), TestContext.Current.CancellationToken);

        MarkdownString contents = Assert.Single(hover!.Contents);
        Assert.Equal("```fake\nvalue\n```\nHover for **value**", contents.Value);
        Assert.Equal(new Range(1, 5, 1, 10), hover.Range);
    }

    [Fact]
    public async Task Hover_PositionAtWordEnd_StillInsideTheWord()
    {
        await using ProviderRig rig = await ProviderRig.StartFakeAsync(Text);

        Hover? hover = await HoverAsync(rig, new Position(1, 10), TestContext.Current.CancellationToken);

        Assert.Equal(new Range(1, 5, 1, 10), hover!.Range);
    }

    [Fact]
    public async Task Hover_OnWhitespace_Null()
    {
        await using ProviderRig rig = await ProviderRig.StartFakeAsync(Text);

        Assert.Null(await HoverAsync(rig, new Position(1, 12), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Hover_SecondLine_PositionIsZeroBased()
    {
        await using ProviderRig rig = await ProviderRig.StartFakeAsync(Text);

        Hover? hover = await HoverAsync(rig, new Position(2, 2), TestContext.Current.CancellationToken);

        Assert.Contains("**second**", hover!.Contents[0].Value);
        Assert.Equal(new Range(2, 1, 2, 7), hover.Range);
    }

    [Fact]
    public async Task Hover_AfterEdit_UsesTheNewText()
    {
        await using ProviderRig rig = await ProviderRig.StartFakeAsync(Text);
        rig.Model.ApplyEdits([new TextEdit(new Range(1, 5, 1, 10), "other")]);
        await rig.Host.BarrierAsync();

        Hover? hover = await HoverAsync(rig, new Position(1, 5), TestContext.Current.CancellationToken);

        Assert.Contains("**other**", hover!.Contents[0].Value);
    }

    [Fact]
    public async Task Hover_ClientNotRunning_Null()
    {
        await using ProviderRig rig = await ProviderRig.StartFakeAsync(Text);
        await rig.Client.ShutdownAsync(TestContext.Current.CancellationToken);

        Assert.Null(await HoverAsync(rig, new Position(1, 5), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Hover_ContentModified_Null()
    {
        ScriptedServer server = new ScriptedServer().On(LspMethods.Hover, _ => throw new JsonRpcException(JsonRpcErrorCodes.ContentModified, "modified"));
        await using ProviderRig rig = await ProviderRig.StartScriptedAsync(server);

        Assert.Null(await HoverAsync(rig, new Position(1, 1), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Hover_OtherError_Faults()
    {
        ScriptedServer server = new ScriptedServer().On(LspMethods.Hover, _ => throw new JsonRpcException(JsonRpcErrorCodes.InternalError, "boom"));
        await using ProviderRig rig = await ProviderRig.StartScriptedAsync(server);

        JsonRpcException e = await Assert.ThrowsAsync<JsonRpcException>(() => HoverAsync(rig, new Position(1, 1), TestContext.Current.CancellationToken));

        Assert.Equal(JsonRpcErrorCodes.InternalError, e.Code);
    }

    [Fact]
    public async Task Hover_Cancelled_TaskCanceled()
    {
        await using ProviderRig rig = await ProviderRig.StartFakeAsync(Text, new FakeServerOptions { DelayMs = 500 });
        using CancellationTokenSource cts = new();

        Task<Hover?> task = HoverAsync(rig, new Position(1, 5), cts.Token);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    [Fact]
    public async Task Hover_MarkedStringArray_Converted()
    {
        ScriptedServer server = new ScriptedServer().On(LspMethods.Hover, _ => """{"contents":["plain",{"language":"cs","value":"int x;"}]}""");
        await using ProviderRig rig = await ProviderRig.StartScriptedAsync(server);

        Hover? hover = await HoverAsync(rig, new Position(1, 1), TestContext.Current.CancellationToken);

        Assert.Equal(2, hover!.Contents.Count);
        Assert.Contains("plain", hover.Contents[0].Value);
        Assert.Contains("int x;", hover.Contents[1].Value);
    }

    [Fact]
    public async Task Hover_NullResult_Null()
    {
        ScriptedServer server = new ScriptedServer().On(LspMethods.Hover, _ => null);
        await using ProviderRig rig = await ProviderRig.StartScriptedAsync(server);

        Assert.Null(await HoverAsync(rig, new Position(1, 1), TestContext.Current.CancellationToken));
    }
}
