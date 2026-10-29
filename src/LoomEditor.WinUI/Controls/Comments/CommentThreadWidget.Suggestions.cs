using LoomEditor.View.Comments;
using LoomEditor.View.Markdown;
using LoomEditor.WinUI.Controls.Markdown;

namespace LoomEditor.WinUI.Controls.Comments;

// ```suggestion blocks of a comment (or of the Preview) drawn as a SuggestionBlockView against the thread's current lines, with Apply.
internal sealed partial class CommentThreadWidget
{
    private const string SuggestionLanguage = "suggestion";

    // The reason the last Apply of a block failed, by (comment id, suggestion index); cleared when the line changes.
    private readonly Dictionary<(string CommentId, int Index), string> _applyToolTips = [];
    private bool _rebuildPending;

    /// <summary>Applies a suggestion: (comment id, suggestion index, rendered range text) → result. Set by the host; none = no Apply button.</summary>
    public Func<string, int, string, ApplySuggestionResult>? ApplySuggestion { get; set; }

    /// <summary>Whether the editor is read-only (Apply disabled). Set by the host.</summary>
    public Func<bool>? IsEditorReadOnly { get; set; }

    /// <summary>The text of the thread's lines changed (an Apply or a hand edit): redraw the blocks, once per dispatcher tick.</summary>
    public void OnLineTextChanged()
    {
        _applyToolTips.Clear();
        ScheduleRebuild();
    }

    /// <summary>Rebuilds now (e.g. the editor's read-only state changed).</summary>
    public void Refresh() => OnChanged(this, EventArgs.Empty);

    private void ScheduleRebuild()
    {
        if (_rebuildPending)
        {
            return;
        }

        _rebuildPending = DispatcherQueue.TryEnqueue(() =>
        {
            _rebuildPending = false;
            Refresh();
        });
    }

    // The builder calls the hook for every code block, also inside a quote; SuggestionExtractor counts top-level blocks only, so the index is
    // the position among the document's top-level suggestion blocks (by reference) and a nested block keeps the default drawing.
    private MarkdownBuildOptions SuggestionOptions(MarkdownDocument document, string? commentId)
    {
        var topLevel = new List<MarkdownCodeBlock>();
        foreach (MarkdownBlock block in document.Blocks)
        {
            if (block is MarkdownCodeBlock { Language: SuggestionLanguage } code)
            {
                topLevel.Add(code);
            }
        }

        string rangeText = _viewModel.SuggestionSeed;
        return new MarkdownBuildOptions
        {
            CustomCodeBlock = block =>
            {
                int index = block.Language == SuggestionLanguage ? topLevel.FindIndex(b => ReferenceEquals(b, block)) : -1;
                return index < 0 ? null : SuggestionView(new SuggestionBlock(index, block.Code), commentId, rangeText);
            },
        };
    }

    private SuggestionBlockView SuggestionView(SuggestionBlock block, string? commentId, string rangeText)
    {
        SuggestionDiffResult diff = SuggestionDiff.Compute(rangeText.Split('\n'), block);
        bool readOnly = IsEditorReadOnly?.Invoke() ?? false;
        Action? apply = commentId is null || ApplySuggestion is null ? null : () => OnApply(commentId, block.Index, rangeText);
        string? toolTip = commentId is not null && _applyToolTips.TryGetValue((commentId, block.Index), out string? reason)
            ? reason
            : readOnly ? CommentStrings.ApplyFailedReadOnly : null;
        var theme = new SuggestionBlockTheme(_foreground, _borderBrush, _removedLine, _insertedLine, _codeFont, _fontSize);
        return new SuggestionBlockView(diff, !readOnly, apply, theme, toolTip) { RenderedRangeText = rangeText };
    }

    private void OnApply(string commentId, int index, string renderedRangeText)
    {
        switch (ApplySuggestion!(commentId, index, renderedRangeText))
        {
            case ApplySuggestionResult.Applied:
                // The edit raises ThreadLineTextChanged, which redraws.
                return;
            case ApplySuggestionResult.Stale:
                _applyToolTips[(commentId, index)] = _viewModel.Thread.StartLineNumber == _viewModel.Thread.LineNumber
                    ? CommentStrings.ApplyFailedStale
                    : CommentStrings.ApplyFailedStaleLines;
                break;
            case ApplySuggestionResult.ReadOnly:
                _applyToolTips[(commentId, index)] = CommentStrings.ApplyFailedReadOnly;
                break;
        }

        ScheduleRebuild();
    }
}
