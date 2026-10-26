using System.Diagnostics;
using System.Text.Json;
using LoomEditor.Lsp.FakeServer;
using LoomEditor.Lsp.Protocol;
using LoomEditor.Lsp.Tests.TestHelpers;

namespace LoomEditor.Lsp.Tests.FakeServer;

public class FakeServerProcessTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Process_Initialize_Shutdown_Exit_ExitCodeZero()
    {
        await using FakeServerHost host = FakeServerHost.StartProcess();
        await host.InitializeAsync();

        await host.RequestAsync(LspMethods.Shutdown, "null");
        host.Notify(LspMethods.Exit, "null");

        Assert.Equal(0, await host.ServerExit.WaitAsync(Timeout, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Process_StderrText_IsCollected()
    {
        await using FakeServerHost host = FakeServerHost.StartProcess(new FakeServerOptions { Stderr = "hello from stderr" });
        await host.InitializeAsync();
        await host.RequestAsync(LspMethods.Shutdown, "null");
        host.Notify(LspMethods.Exit, "null");
        await host.ServerExit.WaitAsync(Timeout, TestContext.Current.CancellationToken);

        Assert.Contains("hello from stderr", host.StderrText);
    }

    [Fact]
    public async Task Process_ExitOn_ExitCodeThree()
    {
        await using FakeServerHost host = FakeServerHost.StartProcess(new FakeServerOptions { ExitOn = LspMethods.DidOpen });
        await host.InitializeAsync();

        host.Open("text");

        Assert.Equal(3, await host.ServerExit.WaitAsync(Timeout, TestContext.Current.CancellationToken));
        await host.Connection.Completion.WaitAsync(Timeout, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Process_DidChange_Diagnostics_Roundtrip()
    {
        await using FakeServerHost host = FakeServerHost.StartProcess();
        await host.InitializeAsync();

        host.Open("fine");
        Assert.Equal(0, (await host.WaitForDiagnosticsAsync()).GetProperty("diagnostics").GetArrayLength());
        await host.ChangeAsync(2, FakeServerHost.Edit(0, 0, 0, 4, "WARN ERROR"));
        JsonElement published = await host.WaitForDiagnosticsAsync();

        Assert.Equal(2, published.GetProperty("diagnostics").GetArrayLength());
        Assert.Equal(2, published.GetProperty("version").GetInt32());
    }

    [Fact]
    public async Task Process_Kill_ConnectionCloses()
    {
        await using FakeServerHost host = FakeServerHost.StartProcess();
        await host.InitializeAsync();

        host.Kill();

        await host.Connection.Completion.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        await host.ServerExit.WaitAsync(Timeout, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Process_PublishedExe_Initialize()
    {
        string? exe = Environment.GetEnvironmentVariable("LOOMEDITOR_FAKESERVER_EXE");
        Assert.SkipUnless(!string.IsNullOrEmpty(exe), "LOOMEDITOR_FAKESERVER_EXE is not set");

        ProcessStartInfo info = new(exe!)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        await using FakeServerHost host = FakeServerHost.StartProcess(info);
        InitializeResult result = await host.InitializeAsync();

        Assert.Equal("LoomEditor.FakeServer", result.ServerInfo?.Name);
        await host.RequestAsync(LspMethods.Shutdown, "null");
        host.Notify(LspMethods.Exit, "null");
        Assert.Equal(0, await host.ServerExit.WaitAsync(Timeout, TestContext.Current.CancellationToken));
    }
}
