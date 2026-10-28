// Ported from microsoft/vscode: src/vs/editor/common/services/editorWebWorker.ts, src/vs/editor/browser/widget/diffEditor/diffProviderFactoryService.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Diff;

/// <summary>
/// Diffs two models on a thread-pool thread (Monaco: <c>EditorWorker.computeDiff</c> behind <c>WorkerBasedDocumentDiffProvider</c>).
/// </summary>
/// <remarks>
/// <para>Differences from Monaco: no worker, no result cache, no telemetry, always the advanced algorithm.</para>
/// <para>Threading: ComputeDiffAsync must be called on the UI thread (it reads the models); the diff itself runs on a pool thread and touches no model.</para>
/// </remarks>
internal sealed class DocumentDiffProvider(ILinesDiffComputer? computer = null) : IDocumentDiffProvider
{
    private readonly ILinesDiffComputer _computer = computer ?? LinesDiffComputers.GetDefault();

    /// <inheritdoc />
    public Task<DocumentDiff> ComputeDiffAsync(TextModel original, TextModel modified, LinesDiffComputerOptions options, CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            return Task.FromCanceled<DocumentDiff>(token);
        }

        // Monaco: the "original file is empty" shortcut.
        if (original.LineCount == 1 && original.GetLineMaxColumn(1) == 1)
        {
            if (modified.LineCount == 1 && modified.GetLineMaxColumn(1) == 1)
            {
                return Task.FromResult(DocumentDiff.Null);
            }

            var change = new DetailedLineRangeMapping(
                new LineRange(1, 2),
                new LineRange(1, modified.LineCount + 1),
                [new RangeMapping(original.FullModelRange, modified.FullModelRange)]);
            return Task.FromResult(new DocumentDiff(false, false, [change], []));
        }

        // Private copies: the models may change while the worker runs.
        string[] originalLines = original.GetLinesContent();
        string[] modifiedLines = modified.GetLinesContent();
        return Task.Run(() => Compute(originalLines, modifiedLines, options, token), token);
    }

    private DocumentDiff Compute(string[] originalLines, string[] modifiedLines, LinesDiffComputerOptions options, CancellationToken token)
    {
        LinesDiff result = _computer.ComputeDiff(originalLines, modifiedLines, options);
        if (token.IsCancellationRequested)
        {
            return new DocumentDiff(false, true, [], []);
        }

        bool identical = result.Changes.Count == 0 && originalLines.AsSpan().SequenceEqual(modifiedLines);
        return new DocumentDiff(identical, result.HitTimeout, result.Changes, result.Moves);
    }
}
