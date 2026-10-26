using LoomEditor.Lsp.Client;
using LoomEditor.Lsp.FakeServer;
using LoomEditor.Lsp.Tests.TestHelpers;

namespace LoomEditor.Lsp.Tests.Client;

public class LspServerProcessTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static async Task WaitForLineAsync(LspServerProcess process, string prefix)
    {
        DateTime end = DateTime.UtcNow + Timeout;
        while (!process.RecentErrorLines.Any(l => l.StartsWith(prefix, StringComparison.Ordinal)))
        {
            Assert.True(DateTime.UtcNow < end, $"No stderr line starting with '{prefix}' within {Timeout}: {string.Join(" | ", process.RecentErrorLines)}");
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task Start_ExposesStreams_AndId()
    {
        await using LspServerProcess process = LspServerProcess.Start(FakeServerLocator.GetCommand());

        Assert.True(process.Id > 0);
        Assert.True(process.StandardInput.CanWrite);
        Assert.True(process.StandardOutput.CanRead);
        Assert.False(process.HasExited);
        Assert.Null(process.ExitCode);
    }

    [Fact]
    public async Task ErrorLines_Raised_AndKept()
    {
        List<string> raised = [];
        LspServerProcess process = LspServerProcess.Start(FakeServerLocator.GetCommand(new FakeServerOptions { Stderr = "hello" }));
        process.ErrorLine += line =>
        {
            lock (raised)
            {
                raised.Add(line);
            }
        };
        await using LspServerProcess owned = process;

        await WaitForLineAsync(process, "hello");

        Assert.Contains("hello", process.RecentErrorLines);
    }

    [Fact]
    public async Task Exited_RaisedOnce_WithExitCode()
    {
        // Input closed without shutdown: the fake server exits with 1.
        LspServerProcess process = LspServerProcess.Start(FakeServerLocator.GetCommand());
        await using LspServerProcess owned = process;
        List<int> codes = [];
        process.Exited += code =>
        {
            lock (codes)
            {
                codes.Add(code);
            }
        };

        process.StandardInput.Close();
        await process.WaitForExitAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
        await Task.Delay(200, TestContext.Current.CancellationToken);

        lock (codes)
        {
            Assert.Equal([1], codes);
        }

        Assert.Equal(1, process.ExitCode);
    }

    [Fact]
    public async Task Kill_StopsProcess_Idempotent()
    {
        await using LspServerProcess process = LspServerProcess.Start(FakeServerLocator.GetCommand());

        process.Kill();
        await process.WaitForExitAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
        process.Kill();

        Assert.True(process.HasExited);
    }

    [Fact]
    public async Task Dispose_KillsRunningProcess()
    {
        LspServerProcess process = LspServerProcess.Start(FakeServerLocator.GetCommand());

        await process.DisposeAsync();

        Assert.True(process.HasExited);
        await process.DisposeAsync();
    }

    [Fact]
    public async Task Environment_Added()
    {
        LspServerCommand command = FakeServerLocator.GetCommand(new FakeServerOptions { EchoEnv = "LOOM_TEST_VAR" }) with
        {
            Environment = new Dictionary<string, string> { ["LOOM_TEST_VAR"] = "from-test" },
        };
        await using LspServerProcess process = LspServerProcess.Start(command);

        await WaitForLineAsync(process, "LOOM_TEST_VAR=from-test");
    }

    [Fact]
    public async Task WorkingDirectory_Used()
    {
        string dir = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar);
        LspServerCommand command = FakeServerLocator.GetCommand(new FakeServerOptions { EchoCwd = true }) with { WorkingDirectory = dir };
        await using LspServerProcess process = LspServerProcess.Start(command);

        await WaitForLineAsync(process, "cwd=");

        Assert.Equal("cwd=" + dir, process.RecentErrorLines.First(l => l.StartsWith("cwd=", StringComparison.Ordinal)).TrimEnd(Path.DirectorySeparatorChar), ignoreCase: OperatingSystem.IsWindows());
    }

    [Fact]
    public async Task Arguments_PassedWithSpaces()
    {
        await using LspServerProcess process = LspServerProcess.Start(FakeServerLocator.GetCommand(null, ["--stderr=hello world"]));

        await WaitForLineAsync(process, "hello world");
    }
}
