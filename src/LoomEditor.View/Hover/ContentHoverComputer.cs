// Ported from microsoft/vscode: src/vs/editor/contrib/hover/browser/contentHoverComputer.ts, markerHoverParticipant.ts (computeSync), markdownHoverParticipant.ts (computeAsync), getHover.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Markers;
using LoomEditor.Core.Model;

namespace LoomEditor.View.Hover;

using Hover = LoomEditor.Core.Languages.Features.Hover;

/// <summary>
/// Computes the parts of a hover: the markers of the anchor line (sync) and the answers of the hover providers (async).
/// Differences from Monaco: a provider hover without a range is kept (the anchor range is used; LSP's range is optional);
/// no decoration-message participant, verbosity, color picker, code actions or status bar.
/// </summary>
/// <remarks>Threading: UI thread only; the provider tasks complete anywhere, and the part building after them does not touch the model.</remarks>
internal sealed class ContentHoverComputer(
    TextModel model,
    MarkerDecorations markers,
    LanguageFeatureRegistry<IHoverProvider> providers,
    Func<string> getLanguageId) : IHoverComputer
{
    /// <inheritdoc />
    public IReadOnlyList<IHoverPart> ComputeSync(HoverComputerOptions options)
    {
        Range anchorRange = options.Anchor.Range;
        if (model.ValidateRange(anchorRange) != anchorRange)
        {
            return [];
        }

        int lineNumber = anchorRange.StartLineNumber;
        int maxColumn = model.GetLineMaxColumn(lineNumber);
        var result = new List<IHoverPart>();
        foreach (ModelDecoration decoration in GetLineDecorations(options.Anchor))
        {
            if (markers.GetMarker(decoration.Id) is not { } marker)
            {
                continue;
            }

            (int startColumn, int endColumn) = Columns(decoration, lineNumber, maxColumn);
            result.Add(new MarkerHoverPart(new Range(lineNumber, startColumn, lineNumber, endColumn), marker));
        }

        return result;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<IHoverPart>>? ComputeAsync(HoverComputerOptions options, CancellationToken token)
    {
        IReadOnlyList<IHoverProvider> ordered = providers.Ordered(getLanguageId());
        if (ordered.Count == 0)
        {
            return null;
        }

        Range anchorRange = options.Anchor.Range;
        var position = anchorRange.Start;
        var tasks = new Task<Hover?>[ordered.Count];
        for (int i = 0; i < ordered.Count; i++)
        {
            Task<Hover?> task;
            try
            {
                task = ordered[i].ProvideHoverAsync(model, position, token);
            }
            catch (Exception)
            {
                task = Task.FromResult<Hover?>(null);
            }

            tasks[i] = task.ContinueWith(static t => t.IsCompletedSuccessfully ? t.Result : null, TaskScheduler.Default);
        }

        // Runs on a thread-pool thread: it must not touch the model (the controller validates the ranges on the UI thread).
        return Task.WhenAll(tasks).ContinueWith(
            t =>
            {
                var parts = new List<IHoverPart>();
                for (int i = 0; i < t.Result.Length; i++)
                {
                    if (t.Result[i] is not { } hover || MarkdownString.IsNullOrEmpty(hover.Contents))
                    {
                        continue;
                    }

                    Range range = hover.Range is { } r && r.StartLineNumber == r.EndLineNumber ? r : anchorRange;
                    parts.Add(new MarkdownHoverPart(range, hover.Contents, i));
                }

                return (IReadOnlyList<IHoverPart>)parts;
            },
            TaskScheduler.Default);
    }

    // Monaco: _getLineDecorations
    private IEnumerable<ModelDecoration> GetLineDecorations(HoverAnchor anchor)
    {
        int lineNumber = anchor.Range.StartLineNumber;
        if (lineNumber > model.LineCount)
        {
            return [];
        }

        int maxColumn = model.GetLineMaxColumn(lineNumber);
        return model.GetLineDecorations(lineNumber).Where(d =>
        {
            if (d.Options.IsWholeLine)
            {
                return true;
            }

            (int startColumn, int endColumn) = Columns(d, lineNumber, maxColumn);
            if (d.Options.ShowIfCollapsed)
            {
                // relaxed by one character on each side
                return !(startColumn > anchor.Range.StartColumn + 1 || anchor.Range.EndColumn - 1 > endColumn);
            }

            return !(startColumn > anchor.Range.StartColumn || anchor.Range.EndColumn > endColumn);
        });
    }

    private static (int Start, int End) Columns(ModelDecoration d, int lineNumber, int maxColumn) =>
        (d.Range.StartLineNumber == lineNumber ? d.Range.StartColumn : 1, d.Range.EndLineNumber == lineNumber ? d.Range.EndColumn : maxColumn);
}
