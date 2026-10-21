// Ported from microsoft/vscode: src/vs/editor/contrib/find/browser/findDecorations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Search;
using LoomEditor.Core.Text;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.Find;

/// <summary>The model decorations of the find matches, the current match, its line and the find scope (Monaco: <c>FindDecorations</c>).</summary>
/// <remarks>
/// Behavior difference: no overview ruler and no minimap parts, so a result of more than 1000 matches only switches to the
/// options without z-index and inline class (phase 6 brings the ruler). Decorations belong to owner 0 (any owner), like
/// the other decorations of the editor.
/// Threading: UI thread only.
/// </remarks>
internal sealed class FindDecorations(ViewModel viewModel) : IDisposable
{
    private const int OwnerId = 0;

    private static readonly ModelDecorationOptions CurrentFindMatchDecoration = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "current-find-match",
        Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
        ZIndex = 13,
        ClassName = "currentFindMatch",
        InlineClassName = "currentFindMatchInline",
        ShowIfCollapsed = true,
    });

    private static readonly ModelDecorationOptions FindMatchDecoration = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "find-match",
        Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
        ZIndex = 10,
        ClassName = "findMatch",
        InlineClassName = "findMatchInline",
        ShowIfCollapsed = true,
    });

    private static readonly ModelDecorationOptions FindMatchNoOverviewDecoration = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "find-match-no-overview",
        Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
        ClassName = "findMatch",
        ShowIfCollapsed = true,
    });

    private static readonly ModelDecorationOptions RangeHighlightDecoration = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "find-range-highlight",
        Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
        ClassName = "rangeHighlight",
        IsWholeLine = true,
    });

    private static readonly ModelDecorationOptions FindScopeDecoration = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "find-scope",
        ClassName = "findScope",
        IsWholeLine = true,
    });

    private readonly TextModel _model = viewModel.Model;
    private string[] _decorations = [];
    private string[] _findScopeDecorationIds = [];
    private string? _rangeHighlightDecorationId;
    private string? _highlightedDecorationId;
    private Position _startPosition = viewModel.GetPosition();

    /// <summary>Removes every decoration.</summary>
    public void Dispose()
    {
        _model.ChangeDecorations(OwnerId, accessor => accessor.DeltaDecorations(AllDecorations(), []));
        Reset();
    }

    /// <summary>Forgets the decoration ids without removing them.</summary>
    public void Reset()
    {
        _decorations = [];
        _findScopeDecorationIds = [];
        _rangeHighlightDecorationId = null;
        _highlightedDecorationId = null;
    }

    /// <summary>The number of find matches.</summary>
    public int GetCount() => _decorations.Length;

    /// <summary>The first find scope, if any.</summary>
    public Range? GetFindScope() => _findScopeDecorationIds.Length > 0 ? _model.GetDecorationRange(_findScopeDecorationIds[0]) : null;

    /// <summary>The find scopes, or <c>null</c> when there is none.</summary>
    public List<Range>? GetFindScopes()
    {
        if (_findScopeDecorationIds.Length == 0)
        {
            return null;
        }

        List<Range> scopes = [.. _findScopeDecorationIds.Select(_model.GetDecorationRange).OfType<Range>()];
        return scopes.Count > 0 ? scopes : null;
    }

    /// <summary>Where the search started (the cursor when the find widget opened).</summary>
    public Position GetStartPosition() => _startPosition;

    /// <summary>Sets the start position and clears the current match.</summary>
    public void SetStartPosition(Position newStartPosition)
    {
        _startPosition = newStartPosition;
        SetCurrentFindMatch(null);
    }

    private int GetDecorationIndex(string decorationId)
    {
        var index = Array.IndexOf(_decorations, decorationId);
        return index >= 0 ? index + 1 : 1;
    }

    /// <summary>The range of the match at a 0-based index, or <c>null</c>.</summary>
    public Range? GetDecorationRangeAt(int index) => index >= 0 && index < _decorations.Length ? _model.GetDecorationRange(_decorations[index]) : null;

    /// <summary>The 1-based position of the match in a range among all matches, or 0 when unknown ('?' in the widget).</summary>
    public int GetCurrentMatchesPosition(Range desiredRange)
    {
        foreach (ModelDecoration candidate in _model.GetDecorationsInRange(desiredRange))
        {
            ModelDecorationOptions options = candidate.Options;
            if (ReferenceEquals(options, FindMatchDecoration)
                || ReferenceEquals(options, FindMatchNoOverviewDecoration)
                || ReferenceEquals(options, CurrentFindMatchDecoration))
            {
                return GetDecorationIndex(candidate.Id);
            }
        }

        // We don't know the current match position, so returns zero to show '?' in find widget
        return 0;
    }

    /// <summary>Highlights the match with this range (or none for <c>null</c>); returns its 1-based position, or 0.</summary>
    public int SetCurrentFindMatch(Range? nextMatch)
    {
        string? newCurrentDecorationId = null;
        var matchPosition = 0;
        if (nextMatch is { } next)
        {
            for (var i = 0; i < _decorations.Length; i++)
            {
                if (_model.GetDecorationRange(_decorations[i]) == next)
                {
                    newCurrentDecorationId = _decorations[i];
                    matchPosition = i + 1;
                    break;
                }
            }
        }

        if (_highlightedDecorationId is not null || newCurrentDecorationId is not null)
        {
            _model.ChangeDecorations(OwnerId, accessor =>
            {
                if (_highlightedDecorationId is not null)
                {
                    accessor.ChangeDecorationOptions(_highlightedDecorationId, FindMatchDecoration);
                    _highlightedDecorationId = null;
                }

                if (newCurrentDecorationId is not null)
                {
                    _highlightedDecorationId = newCurrentDecorationId;
                    accessor.ChangeDecorationOptions(_highlightedDecorationId, CurrentFindMatchDecoration);
                }

                if (_rangeHighlightDecorationId is not null)
                {
                    accessor.RemoveDecoration(_rangeHighlightDecorationId);
                    _rangeHighlightDecorationId = null;
                }

                if (newCurrentDecorationId is not null)
                {
                    Range rng = _model.GetDecorationRange(newCurrentDecorationId)!.Value;
                    if (rng.StartLineNumber != rng.EndLineNumber && rng.EndColumn == 1)
                    {
                        var lineBeforeEnd = rng.EndLineNumber - 1;
                        rng = new Range(rng.StartLineNumber, rng.StartColumn, lineBeforeEnd, _model.GetLineMaxColumn(lineBeforeEnd));
                    }

                    _rangeHighlightDecorationId = accessor.AddDecoration(rng, RangeHighlightDecoration);
                }
            });
        }

        return matchPosition;
    }

    /// <summary>Replaces the decorations by one per find match and one per find scope.</summary>
    public void Set(IReadOnlyList<FindMatch> findMatches, IReadOnlyList<Range>? findScopes)
    {
        _model.ChangeDecorations(OwnerId, accessor =>
        {
            // above 1000 matches: no z-index and no inline class (Monaco also feeds the overview ruler, phase 6)
            ModelDecorationOptions findMatchesOptions = findMatches.Count > 1000 ? FindMatchNoOverviewDecoration : FindMatchDecoration;

            // Find matches
            _decorations = accessor.DeltaDecorations(_decorations, [.. findMatches.Select(m => new ModelDeltaDecoration(m.Range, findMatchesOptions))]);

            // Range highlight
            if (_rangeHighlightDecorationId is not null)
            {
                accessor.RemoveDecoration(_rangeHighlightDecorationId);
                _rangeHighlightDecorationId = null;
            }

            // Find scope
            if (_findScopeDecorationIds.Length > 0)
            {
                foreach (var id in _findScopeDecorationIds)
                {
                    accessor.RemoveDecoration(id);
                }

                _findScopeDecorationIds = [];
            }

            if (findScopes is { Count: > 0 })
            {
                _findScopeDecorationIds = [.. findScopes.Select(scope => accessor.AddDecoration(scope, FindScopeDecoration))];
            }
        });
    }

    /// <summary>The last match ending at or before a position, wrapping to the last match.</summary>
    public Range? MatchBeforePosition(Position position)
    {
        if (_decorations.Length == 0)
        {
            return null;
        }

        for (var i = _decorations.Length - 1; i >= 0; i--)
        {
            if (_model.GetDecorationRange(_decorations[i]) is not { } r || r.EndLineNumber > position.LineNumber)
            {
                continue;
            }

            if (r.EndLineNumber < position.LineNumber || r.EndColumn <= position.Column)
            {
                return r;
            }
        }

        return _model.GetDecorationRange(_decorations[^1]);
    }

    /// <summary>The first match starting at or after a position, wrapping to the first match.</summary>
    public Range? MatchAfterPosition(Position position)
    {
        if (_decorations.Length == 0)
        {
            return null;
        }

        foreach (var id in _decorations)
        {
            if (_model.GetDecorationRange(id) is not { } r || r.StartLineNumber < position.LineNumber)
            {
                continue;
            }

            if (r.StartLineNumber > position.LineNumber || r.StartColumn >= position.Column)
            {
                return r;
            }
        }

        return _model.GetDecorationRange(_decorations[0]);
    }

    private string[] AllDecorations()
    {
        List<string> result = [.. _decorations, .. _findScopeDecorationIds];
        if (_rangeHighlightDecorationId is not null)
        {
            result.Add(_rangeHighlightDecorationId);
        }

        return [.. result];
    }
}
