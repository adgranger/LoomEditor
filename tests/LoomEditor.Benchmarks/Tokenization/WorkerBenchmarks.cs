using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;
using LoomEditor.Core.Model.Tokens;
using LoomEditor.Core.Threading;

namespace LoomEditor.Benchmarks.Tokenization;

/// <summary>
/// The time for the background worker to tokenize a whole 100 000-line C# file: from <c>SetLanguage</c> until
/// <see cref="BackgroundTokenizationState.Completed"/>. It includes starting the worker and compiling the grammar, and the
/// model and highlighter are created in the measured call. This thread plays the UI thread: it runs what the worker posts.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 3)]
public class WorkerBenchmarks
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(5);

    /// <summary>The number of lines of the C# file.</summary>
    [Params(100_000)]
    public int LineCount { get; set; }

    /// <summary>Tokenizes 100 000 lines on the worker thread and returns the line count once the model says it is done.</summary>
    [Benchmark]
    public int TokenizeInBackground()
    {
        var dispatcher = new QueueDispatcher();
        (var model, var highlighter) = TokenizationBenchmarkData.CreateHighlightedModel("csharp", LineCount, dispatcher);
        using (highlighter)
        using (model.AttachView())
        {
            highlighter.SetLanguage("csharp");
            long deadline = Environment.TickCount64 + (long)Timeout.TotalMilliseconds;
            while (model.Tokenization.BackgroundTokenizationState != BackgroundTokenizationState.Completed)
            {
                if (Environment.TickCount64 > deadline)
                {
                    throw new TimeoutException("The worker did not finish.");
                }

                dispatcher.RunPending();
            }

            return model.LineCount;
        }
    }

    private sealed class QueueDispatcher : IUiDispatcher
    {
        private readonly BlockingCollection<Action> _queue = [];

        public bool Post(Action action)
        {
            _queue.Add(action);
            return true;
        }

        public void RunPending()
        {
            if (_queue.TryTake(out Action? action, 10))
            {
                action();
            }
        }
    }
}
