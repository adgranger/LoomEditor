using System.Diagnostics;
using System.Text;
using System.Text.Json;
using LoomEditor.Lsp.FakeServer;
using LoomEditor.Lsp.JsonRpc;
using LoomEditor.Lsp.Protocol;

namespace LoomEditor.Lsp.Tests.TestHelpers;

/// <summary>A client connection to a fake language server, running in process or as a child process.</summary>
/// <remarks>Threading: members are safe to call from the test thread; notifications are recorded from the read loop thread.</remarks>
internal sealed class FakeServerHost : IAsyncDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly object _gate = new();
    private readonly Dictionary<string, int> _consumed = [];
    private readonly MemoryPipe? _pipe;
    private readonly Process? _process;
    private readonly StringBuilder _stderr = new();

    private FakeServerHost(MemoryPipe? pipe, Process? process, FakeLanguageServer? server, Task<int> serverExit, Stream input, Stream output)
    {
        _pipe = pipe;
        _process = process;
        Server = server;
        ServerExit = serverExit;
        Connection = new JsonRpcConnection(input, output);
        Connection.NotificationReceived += (method, @params) =>
        {
            lock (_gate)
            {
                Notifications.Add((method, @params));
            }
        };
        Connection.Start();
    }

    public JsonRpcConnection Connection { get; }

    /// <summary>The in-process server; <c>null</c> for a child process.</summary>
    public FakeLanguageServer? Server { get; }

    /// <summary>The exit code of the server (in process: of <c>RunAsync</c>).</summary>
    public Task<int> ServerExit { get; }

    public List<(string Method, JsonElement? Params)> Notifications { get; } = [];

    /// <summary>What the child process wrote to standard error.</summary>
    public string StderrText
    {
        get
        {
            lock (_gate)
            {
                return _stderr.ToString();
            }
        }
    }

    public static FakeServerHost StartInProcess(FakeServerOptions? options = null)
    {
        MemoryPipe pipe = new();
        FakeLanguageServer server = new(pipe.ServerInput, pipe.ServerOutput, options ?? new FakeServerOptions());
        Task<int> exit = Task.Run(async () =>
        {
            try
            {
                return await server.RunAsync(CancellationToken.None);
            }
            finally
            {
                server.Dispose();
            }
        });
        return new FakeServerHost(pipe, null, server, exit, pipe.ClientInput, pipe.ClientOutput);
    }

    public static FakeServerHost StartProcess(FakeServerOptions? options = null)
    {
        ProcessStartInfo info = FakeServerLocator.GetStartInfo(FakeServerLocator.ToArgs(options ?? new FakeServerOptions()));
        return StartProcess(info);
    }

    /// <summary>Starts the server from an explicit command (the published exe).</summary>
    public static FakeServerHost StartProcess(ProcessStartInfo info)
    {
        Process process = Process.Start(info) ?? throw new InvalidOperationException("The fake server process did not start");
        FakeServerHost host = null!;
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                lock (host._gate)
                {
                    host._stderr.AppendLine(e.Data);
                }
            }
        };
        Task<int> exit = process.WaitForExitAsync().ContinueWith(_ => process.ExitCode, TaskScheduler.Default);
        host = new FakeServerHost(null, process, null, exit, process.StandardOutput.BaseStream, process.StandardInput.BaseStream);
        process.BeginErrorReadLine();
        return host;
    }

    /// <summary>Sends <c>initialize</c> then <c>initialized</c>.</summary>
    public async Task<InitializeResult> InitializeAsync()
    {
        InitializeParams request = new() { ProcessId = null, RootUri = null, Capabilities = LspClientCapabilities.Create() };
        InitializeResult? result = await Connection.SendRequestAsync(
            LspMethods.Initialize, request, LspJsonContext.Default.InitializeParams, LspJsonContext.Default.InitializeResult, TestContext.Current.CancellationToken)
            .WaitAsync(Timeout, TestContext.Current.CancellationToken);
        Connection.SendNotification(LspMethods.Initialized, new InitializedParams(), LspJsonContext.Default.InitializedParams);
        return result!;
    }

    public async Task<JsonElement?> RequestAsync(string method, string paramsJson)
        => await Connection.SendRequestAsync(method, JsonElement.Parse(paramsJson), TestContext.Current.CancellationToken)
            .WaitAsync(Timeout, TestContext.Current.CancellationToken);

    public void Notify(string method, string paramsJson) => Connection.SendNotification(method, JsonElement.Parse(paramsJson));

    /// <summary>A <c>textDocument</c> and <c>position</c> params object.</summary>
    public static string At(int line, int character, string uri = "file:///t.txt")
        => $"{{\"textDocument\":{{\"uri\":{Quote(uri)}}},\"position\":{{\"line\":{line},\"character\":{character}}}}}";

    /// <summary>One incremental content change.</summary>
    public static string Edit(int line1, int ch1, int line2, int ch2, string text)
        => $"{{\"range\":{{\"start\":{{\"line\":{line1},\"character\":{ch1}}},\"end\":{{\"line\":{line2},\"character\":{ch2}}}}},\"text\":{Quote(text)}}}";

    /// <summary>A full-text content change.</summary>
    public static string Full(string text) => $"{{\"text\":{Quote(text)}}}";

    /// <summary>Sends <c>didChange</c> and waits until the server handled it.</summary>
    public async Task ChangeAsync(int version, string changesJson, string uri = "file:///t.txt")
    {
        Notify(LspMethods.DidChange, $"{{\"textDocument\":{{\"uri\":{Quote(uri)},\"version\":{version}}},\"contentChanges\":[{changesJson}]}}");
        await BarrierAsync();
    }

    /// <summary>Kills the child process.</summary>
    public void Kill() => _process!.Kill(entireProcessTree: true);

    public static string Quote(string text) => JsonSerializer.Serialize(text, TestJson.Default.String);

    public void Open(string text, string uri = "file:///t.txt", int version = 1)
        => Notify(LspMethods.DidOpen, $"{{\"textDocument\":{{\"uri\":{Quote(uri)},\"languageId\":\"fake\",\"version\":{version},\"text\":{Quote(text)}}}}}");

    /// <summary>Returns once the server has handled every message sent before (it answers in order); the answer itself is ignored.</summary>
    public async Task BarrierAsync()
    {
        try
        {
            await RequestAsync("test/barrier", "{}");
        }
        catch (JsonRpcException)
        {
            // MethodNotFound or ServerNotInitialized: either way the server got there.
        }
    }

    /// <summary>The next not yet returned notification of <paramref name="method"/>.</summary>
    public async Task<JsonElement> WaitForNotificationAsync(string method, TimeSpan timeout)
    {
        DateTime end = DateTime.UtcNow + timeout;
        while (true)
        {
            lock (_gate)
            {
                int seen = _consumed.GetValueOrDefault(method);
                int found = 0;
                foreach ((string name, JsonElement? @params) in Notifications)
                {
                    if (name == method && found++ == seen)
                    {
                        _consumed[method] = seen + 1;
                        return @params ?? default;
                    }
                }
            }

            if (DateTime.UtcNow > end)
            {
                throw new TimeoutException($"No notification {method} within {timeout}");
            }

            await Task.Delay(10, TestContext.Current.CancellationToken);
        }
    }

    public Task<JsonElement> WaitForDiagnosticsAsync() => WaitForNotificationAsync(LspMethods.PublishDiagnostics, Timeout);

    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
        if (_process is not null)
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
            }

            await _process.WaitForExitAsync();
            _process.Dispose();
        }
        else
        {
            await ServerExit.WaitAsync(Timeout);
            _pipe?.Dispose();
        }
    }
}
