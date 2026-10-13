using BenchmarkDotNet.Running;

namespace LoomEditor.Benchmarks;

/// <summary>Entry point: lets BenchmarkDotNet pick benchmarks from the command line (e.g. --filter "*Range*").</summary>
public static class Program
{
    /// <summary>Runs the benchmarks selected on the command line.</summary>
    public static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
