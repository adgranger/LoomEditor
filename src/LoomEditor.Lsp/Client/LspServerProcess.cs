using System.Diagnostics;
using System.Text;

namespace LoomEditor.Lsp.Client;

/// <summary>A language server child process with redirected standard streams.</summary>
/// <remarks>Threading: members are thread-safe; <see cref="ErrorLine"/> is raised on the standard error loop thread and <see cref="Exited"/> on a thread pool thread.</remarks>
internal sealed class LspServerProcess : IAsyncDisposable
{
    private const int MaxErrorLines = 50;

    private readonly Process _process;
    private readonly object _gate = new();
    private readonly Queue<string> _errorLines = new();
    private readonly Task _errorLoop;
    private readonly TaskCompletionSource _exited = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _exitedRaised;
    private int _disposed;

    private LspServerProcess(Process process)
    {
        _process = process;
        Id = process.Id;
        StandardInput = process.StandardInput.BaseStream;
        StandardOutput = process.StandardOutput.BaseStream;
        _errorLoop = Task.Factory.StartNew(ReadErrorLinesAsync, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        process.EnableRaisingEvents = true;
        process.Exited += OnExited;
        if (process.HasExited)
        {
            OnExited(null, EventArgs.Empty);
        }
    }

    /// <summary>Raised for each line the server writes to standard error.</summary>
    public event Action<string>? ErrorLine;

    /// <summary>Raised once, with the exit code.</summary>
    public event Action<int>? Exited;

    public int Id { get; }

    /// <summary>Completes when standard error reached its end (after the process ended).</summary>
    public Task ErrorLoopCompleted => _errorLoop;

    public Stream StandardInput { get; }

    public Stream StandardOutput { get; }

    public bool HasExited => _exited.Task.IsCompleted || _process.HasExited;

    public int? ExitCode => HasExited ? _process.ExitCode : null;

    /// <summary>The last 50 lines written to standard error.</summary>
    public IReadOnlyList<string> RecentErrorLines
    {
        get
        {
            lock (_gate)
            {
                return [.. _errorLines];
            }
        }
    }

    /// <summary>Starts the process; any start failure becomes an <see cref="LspServerStartException"/>.</summary>
    public static LspServerProcess Start(LspServerCommand command)
    {
        Process process = new()
        {
            StartInfo =
            {
                FileName = command.FileName,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardErrorEncoding = Encoding.UTF8,
            },
        };
        if (command.WorkingDirectory is { } cwd)
        {
            process.StartInfo.WorkingDirectory = cwd;
        }

        foreach (string argument in command.Arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        if (command.Environment is { } environment)
        {
            foreach ((string name, string value) in environment)
            {
                process.StartInfo.Environment[name] = value;
            }
        }

        try
        {
            process.Start();
        }
        catch (Exception e) when (e is System.Runtime.InteropServices.ExternalException or InvalidOperationException or IOException)
        {
            process.Dispose();
            throw new LspServerStartException($"Cannot start the language server '{command.FileName}': {e.Message}", e);
        }

        return new LspServerProcess(process);
    }

    public Task WaitForExitAsync(CancellationToken token) => _exited.Task.WaitAsync(token);

    /// <summary>Kills the process and its children; does nothing when it already exited.</summary>
    public void Kill()
    {
        try
        {
            _process.Kill(entireProcessTree: true);
        }
        catch (Exception e) when (e is InvalidOperationException or System.Runtime.InteropServices.ExternalException or NotSupportedException)
        {
            // Already exited.
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        try
        {
            if (!HasExited)
            {
                Kill();
                try
                {
                    await _exited.Task.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    // A process that survives a kill: abandon it.
                }
            }

            try
            {
                await _errorLoop.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // The loop ends with the process; a stuck pipe is abandoned.
            }

            _process.Dispose();
        }
        catch (Exception)
        {
            // Disposal never throws.
        }
    }

    private void OnExited(object? sender, EventArgs e)
    {
        if (Interlocked.Exchange(ref _exitedRaised, 1) != 0)
        {
            return;
        }

        int code;
        try
        {
            code = _process.ExitCode;
        }
        catch (InvalidOperationException)
        {
            code = -1;
        }

        _exited.TrySetResult();
        try
        {
            Exited?.Invoke(code);
        }
        catch (Exception)
        {
            // A handler must not break the process bookkeeping.
        }
    }

    private async Task ReadErrorLinesAsync()
    {
        try
        {
            while (await _process.StandardError.ReadLineAsync().ConfigureAwait(false) is { } line)
            {
                lock (_gate)
                {
                    _errorLines.Enqueue(line);
                    if (_errorLines.Count > MaxErrorLines)
                    {
                        _errorLines.Dequeue();
                    }
                }

                try
                {
                    ErrorLine?.Invoke(line);
                }
                catch (Exception)
                {
                    // A handler must not stop the loop.
                }
            }
        }
        catch (Exception)
        {
            // The pipe closed under us (kill or dispose).
        }
    }
}
