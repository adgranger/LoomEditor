using System.Text.Json;
using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Text;
using LoomEditor.Lsp.Editor;
using LoomEditor.Lsp.FakeServer;
using LoomEditor.Lsp.JsonRpc;
using LoomEditor.Lsp.Protocol;
using LoomEditor.Lsp.Tests.TestHelpers;

namespace LoomEditor.Lsp.Tests.Editor;

public class LspDefinitionProviderTests
{
    private const string Text = "def alpha\nclass Beta\nalpha Beta external";

    private static Task<IReadOnlyList<LocationLink>?> DefineAsync(ProviderRig rig, Position position, CancellationToken token = default)
        => new LspDefinitionProvider(rig.Client, ProviderRig.Uri).ProvideDefinitionAsync(rig.Model, position, token);

    [Fact]
    public async Task Definition_Def_SingleLocation()
    {
        await using ProviderRig rig = await ProviderRig.StartFakeAsync(Text);

        IReadOnlyList<LocationLink>? links = await DefineAsync(rig, new Position(3, 2), TestContext.Current.CancellationToken);

        LocationLink link = Assert.Single(links!);
        Assert.Equal(ProviderRig.Uri, link.Uri);
        Assert.Equal(new Range(1, 5, 1, 10), link.Range);
        Assert.Equal(link.Range, link.TargetSelectionRange);
    }

    [Fact]
    public async Task Definition_Class_LocationLink()
    {
        await using ProviderRig rig = await ProviderRig.StartFakeAsync(Text);

        IReadOnlyList<LocationLink>? links = await DefineAsync(rig, new Position(3, 8), TestContext.Current.CancellationToken);

        LocationLink link = Assert.Single(links!);
        Assert.Equal(new Range(3, 7, 3, 11), link.OriginSelectionRange);
        Assert.Equal(new Range(2, 1, 2, 11), link.Range);
        Assert.Equal(new Range(2, 7, 2, 11), link.TargetSelectionRange);
    }

    [Fact]
    public async Task Definition_External_OtherUri()
    {
        await using ProviderRig rig = await ProviderRig.StartFakeAsync(Text);

        IReadOnlyList<LocationLink>? links = await DefineAsync(rig, new Position(3, 13), TestContext.Current.CancellationToken);

        Assert.Equal("file:///other/file.txt", Assert.Single(links!).Uri);
    }

    [Fact]
    public async Task Definition_NotFound_Null_Or_Empty()
    {
        await using ProviderRig rig = await ProviderRig.StartFakeAsync(Text);

        IReadOnlyList<LocationLink>? links = await DefineAsync(rig, new Position(1, 2), TestContext.Current.CancellationToken);

        Assert.NotNull(links);
        Assert.Empty(links);
    }

    [Fact]
    public async Task Definition_ClientNotRunning_Null()
    {
        await using ProviderRig rig = await ProviderRig.StartFakeAsync(Text);
        await rig.Client.ShutdownAsync(TestContext.Current.CancellationToken);

        Assert.Null(await DefineAsync(rig, new Position(3, 2), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Definition_ContentModified_Null()
    {
        ScriptedServer server = new ScriptedServer().On(LspMethods.Definition, _ => throw new JsonRpcException(JsonRpcErrorCodes.ContentModified, "modified"));
        await using ProviderRig rig = await ProviderRig.StartScriptedAsync(server);

        Assert.Null(await DefineAsync(rig, new Position(1, 1), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Definition_Cancelled_TaskCanceled()
    {
        await using ProviderRig rig = await ProviderRig.StartFakeAsync(Text, new FakeServerOptions { DelayMs = 500 });
        using CancellationTokenSource cts = new();

        Task<IReadOnlyList<LocationLink>?> task = DefineAsync(rig, new Position(3, 2), cts.Token);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    [Fact]
    public async Task Definition_PositionIsSentZeroBased()
    {
        JsonElement? seen = null;
        ScriptedServer server = new ScriptedServer().On(LspMethods.Definition, p =>
        {
            seen = p;
            return null;
        });
        await using ProviderRig rig = await ProviderRig.StartScriptedAsync(server);

        await DefineAsync(rig, new Position(3, 5), TestContext.Current.CancellationToken);

        JsonElement position = seen!.Value.GetProperty("position");
        Assert.Equal(2, position.GetProperty("line").GetInt32());
        Assert.Equal(4, position.GetProperty("character").GetInt32());
        Assert.Equal(ProviderRig.Uri, seen.Value.GetProperty("textDocument").GetProperty("uri").GetString());
    }
}
