using System.Diagnostics;
using LoomEditor.Lsp.Client;
using LoomEditor.Lsp.FakeServer;

namespace LoomEditor.Lsp.Tests.TestHelpers;

/// <summary>Finds how to start the fake language server as a child process.</summary>
/// <remarks>Threading: stateless, safe from any thread.</remarks>
internal static class FakeServerLocator
{
    /// <summary>The apphost next to the test assembly if it exists, else <c>dotnet</c> with the server dll.</summary>
    public static ProcessStartInfo GetStartInfo(string[] args)
    {
        string name = "LoomEditor.Lsp.FakeServer";
        string exe = Path.Combine(AppContext.BaseDirectory, OperatingSystem.IsWindows() ? name + ".exe" : name);
        string dll = Path.Combine(AppContext.BaseDirectory, name + ".dll");
        ProcessStartInfo info;
        if (File.Exists(exe))
        {
            info = new ProcessStartInfo(exe);
        }
        else if (File.Exists(dll))
        {
            info = new ProcessStartInfo("dotnet");
            info.ArgumentList.Add(dll);
        }
        else
        {
            throw new FileNotFoundException($"The fake server was not found. Tried: {exe} and {dll}");
        }

        foreach (string arg in args)
        {
            info.ArgumentList.Add(arg);
        }

        info.RedirectStandardInput = true;
        info.RedirectStandardOutput = true;
        info.RedirectStandardError = true;
        info.UseShellExecute = false;
        info.CreateNoWindow = true;
        return info;
    }

    /// <summary>The fake server as an <see cref="LspServerCommand"/>.</summary>
    public static LspServerCommand GetCommand(FakeServerOptions? options = null, string[]? extraArgs = null)
    {
        ProcessStartInfo info = GetStartInfo([.. ToArgs(options ?? new FakeServerOptions()), .. extraArgs ?? []]);
        return new LspServerCommand(info.FileName, [.. info.ArgumentList]);
    }

    /// <summary>The command line of <paramref name="options"/>.</summary>
    public static string[] ToArgs(LoomEditor.Lsp.FakeServer.FakeServerOptions options)
    {
        List<string> args = [$"--sync={options.Sync}", $"--delay={options.DelayMs}"];
        if (options.ExitOn is { } exitOn)
        {
            args.Add($"--exit-on={exitOn}");
        }

        if (options.Stderr is { } stderr)
        {
            args.Add($"--stderr={stderr}");
        }

        if (options.Incomplete)
        {
            args.Add("--incomplete");
        }

        if (options.Chatty)
        {
            args.Add("--chatty");
        }

        if (options.EchoEnv is { } echoEnv)
        {
            args.Add($"--echo-env={echoEnv}");
        }

        if (options.EchoCwd)
        {
            args.Add("--echo-cwd");
        }

        return [.. args];
    }
}
