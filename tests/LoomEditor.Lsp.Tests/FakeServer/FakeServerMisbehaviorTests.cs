using System.Diagnostics;
using System.Text.Json;
using LoomEditor.Lsp.FakeServer;
using LoomEditor.Lsp.JsonRpc;
using LoomEditor.Lsp.Protocol;
using LoomEditor.Lsp.Tests.TestHelpers;

namespace LoomEditor.Lsp.Tests.FakeServer;

public class FakeServerMisbehaviorTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private static async Task<FakeServerHost> OpenAsync(FakeServerOptions options, string text = "hello")
    {
        FakeServerHost host = FakeServerHost.StartInProcess(options);
        await host.InitializeAsync();
        host.Open(text);
        await host.BarrierAsync();
        return host;
    }

    [Fact]
    public async Task Delay_AnswersAfterTheDelay()
    {
        await using FakeServerHost host = await OpenAsync(new FakeServerOptions { DelayMs = 300 });

        Stopwatch watch = Stopwatch.StartNew();
        JsonElement? hover = await host.RequestAsync(LspMethods.Hover, FakeServerHost.At(0, 1));

        Assert.True(watch.ElapsedMilliseconds >= 295, $"answered after {watch.ElapsedMilliseconds} ms");
        Assert.NotEqual(JsonValueKind.Null, hover!.Value.ValueKind);
    }

    [Fact]
    public async Task Delay_ReadLoopStillReads()
    {
        await using FakeServerHost host = await OpenAsync(new FakeServerOptions { DelayMs = 1500 });
        await host.WaitForDiagnosticsAsync();

        Task<JsonElement?> hover = host.RequestAsync(LspMethods.Hover, FakeServerHost.At(0, 1));
        await host.ChangeAsync(2, FakeServerHost.Full("ERROR"));
        JsonElement published = await host.WaitForNotificationAsync(LspMethods.PublishDiagnostics, TimeSpan.FromMilliseconds(1000));

        Assert.False(hover.IsCompleted);
        Assert.Equal(2, published.GetProperty("version").GetInt32());
        Assert.Equal("ERROR", host.Server!.DocumentText);
        await hover;
    }

    [Fact]
    public async Task Cancel_DelayedRequest_AnswersRequestCancelled()
    {
        await using FakeServerHost host = await OpenAsync(new FakeServerOptions { DelayMs = 10_000 });

        Stopwatch watch = Stopwatch.StartNew();
        Task<JsonElement?> hover = host.RequestAsync(LspMethods.Hover, FakeServerHost.At(0, 1));
        await host.BarrierAsync(); // the hover is request 3 (initialize 1, the barrier of OpenAsync 2)
        host.Notify(LspMethods.CancelRequest, """{"id":3}""");

        JsonRpcException e = await Assert.ThrowsAsync<JsonRpcException>(() => hover);
        Assert.Equal(JsonRpcErrorCodes.RequestCancelled, e.Code);
        Assert.True(watch.ElapsedMilliseconds < 3000, $"cancelled after {watch.ElapsedMilliseconds} ms");
        Assert.Contains(LspMethods.CancelRequest, host.Server!.ReceivedMethods);
    }

    [Fact]
    public async Task Cancel_UnknownId_Ignored()
    {
        await using FakeServerHost host = await OpenAsync(new FakeServerOptions());

        host.Notify(LspMethods.CancelRequest, """{"id":999}""");
        JsonElement? hover = await host.RequestAsync(LspMethods.Hover, FakeServerHost.At(0, 1));

        Assert.NotEqual(JsonValueKind.Null, hover!.Value.ValueKind);
        Assert.Contains(LspMethods.CancelRequest, host.Server!.ReceivedMethods);
    }

    [Fact]
    public async Task ExitOn_InProcess_ReturnsThree()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess(new FakeServerOptions { ExitOn = LspMethods.DidOpen });
        await host.InitializeAsync();

        host.Open("text");

        Assert.Equal(3, await host.ServerExit.WaitAsync(Timeout, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Chatty_SendsTheServerRequests_AndRecordsResponses()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess(new FakeServerOptions { Chatty = true });
        host.Connection.RequestHandler = (method, _, _) => Task.FromResult<JsonElement?>(method switch
        {
            LspMethods.WorkspaceConfiguration => JsonElement.Parse("[null]"),
            LspMethods.ApplyEdit => JsonElement.Parse("""{"applied":false}"""),
            _ => null,
        });
        await host.InitializeAsync();

        await WaitUntilAsync(() => host.Server!.ReceivedMethods.Count(m => m.StartsWith("response(", StringComparison.Ordinal)) == 4);

        Assert.Equal("[null]", host.Server!.LastResponseResult("cfg-1")!.Value.GetRawText());
        Assert.Equal("""{"applied":false}""", host.Server.LastResponseResult("edit-1")!.Value.GetRawText());
        Assert.Null(host.Server.LastResponseResult("reg-1"));
        Assert.Null(host.Server.LastResponseResult("unknown"));
        // The client answers server requests concurrently: the order of the answers is not fixed.
        Assert.Equal(
            ["response(cfg-1)", "response(edit-1)", "response(reg-1)", "response(wdp-1)"],
            host.Server.ReceivedMethods.Where(m => m.StartsWith("response(", StringComparison.Ordinal)).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Chatty_SendsLogAndShowMessage()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess(new FakeServerOptions { Chatty = true });
        await host.InitializeAsync();

        JsonElement log = await host.WaitForNotificationAsync(LspMethods.LogMessage, Timeout);
        JsonElement show = await host.WaitForNotificationAsync(LspMethods.ShowMessage, Timeout);

        Assert.Equal(3, log.GetProperty("type").GetInt32());
        Assert.Equal("fake server ready", log.GetProperty("message").GetString());
        Assert.Equal(2, show.GetProperty("type").GetInt32());
        Assert.Equal("fake warning", show.GetProperty("message").GetString());
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        DateTime end = DateTime.UtcNow + Timeout;
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < end, "The condition was not met in time");
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }
    }
}
