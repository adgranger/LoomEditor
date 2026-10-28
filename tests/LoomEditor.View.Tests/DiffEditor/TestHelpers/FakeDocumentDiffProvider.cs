using LoomEditor.Core.Diff;
using LoomEditor.Core.Model;

namespace LoomEditor.View.Tests.DiffEditor.TestHelpers;

/// <summary>An <see cref="IDocumentDiffProvider"/> whose results the test completes by hand (or that returns a prepared diff at once).</summary>
internal sealed class FakeDocumentDiffProvider : IDocumentDiffProvider
{
    public sealed record Call(LinesDiffComputerOptions Options, TaskCompletionSource<DocumentDiff> Result, CancellationToken Token);

    public List<Call> Calls { get; } = [];

    /// <summary>When set, every call completes at once with this diff.</summary>
    public DocumentDiff? Immediate { get; set; }

    /// <summary>When set, every call throws it.</summary>
    public Exception? Throw { get; set; }

    public Task<DocumentDiff> ComputeDiffAsync(TextModel original, TextModel modified, LinesDiffComputerOptions options, CancellationToken token)
    {
        if (Throw is not null)
        {
            throw Throw;
        }

        var call = new Call(options, new TaskCompletionSource<DocumentDiff>(), token);
        Calls.Add(call);
        if (Immediate is not null)
        {
            call.Result.SetResult(Immediate);
        }

        return call.Result.Task;
    }
}
