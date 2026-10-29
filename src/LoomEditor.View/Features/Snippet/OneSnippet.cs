// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetSession.ts (OneSnippet)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Snippets;
using LoomEditor.Core.Undo;

namespace LoomEditor.View.Features.Snippet;

/// <summary>
/// One inserted snippet, tracked in the model by one decoration per placeholder (Monaco: <c>OneSnippet</c>): it moves between the
/// tab stops and merges nested snippets.
/// </summary>
/// <remarks>
/// Behavior differences: the decorations belong to the editor (<see cref="EditorContext.Id"/>), not to a feature owner id; the
/// <c>min-width: 2px</c> of <c>snippetSession.css</c> is not ported. Threading: UI thread only.
/// </remarks>
internal sealed partial class OneSnippet : IDisposable
{
    private static readonly ModelDecorationOptions ActiveDecoration = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "snippet-placeholder-1",
        Stickiness = TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges,
        ClassName = "snippet-placeholder",
    });

    private static readonly ModelDecorationOptions InactiveDecoration = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "snippet-placeholder-2",
        Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
        ClassName = "snippet-placeholder",
    });

    private static readonly ModelDecorationOptions ActiveFinalDecoration = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "snippet-placeholder-3",
        Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
        ClassName = "finish-snippet-placeholder",
    });

    private static readonly ModelDecorationOptions InactiveFinalDecoration = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "snippet-placeholder-4",
        Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
        ClassName = "finish-snippet-placeholder",
    });

    private readonly EditorContext _editor;
    private readonly TextmateSnippet _snippet;
    private readonly string _snippetLineLeadingWhitespace;
    private Dictionary<Placeholder, string>? _placeholderDecorations;
    private List<List<Placeholder>> _placeholderGroups;
    private int _offset = -1;
    private int _nestingLevel = 1;

    /// <summary>Creates the snippet; call <see cref="Initialize"/> once its text is in the model.</summary>
    /// <param name="editor">The editor whose model holds the text.</param>
    /// <param name="snippet">The parsed snippet.</param>
    /// <param name="leadingWhitespace">The indentation of the line the snippet was inserted in.</param>
    public OneSnippet(EditorContext editor, TextmateSnippet snippet, string leadingWhitespace)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(snippet);
        ArgumentNullException.ThrowIfNull(leadingWhitespace);

        _editor = editor;
        _snippet = snippet;
        _snippetLineLeadingWhitespace = leadingWhitespace;
        _placeholderGroups = GroupByIndex(snippet.Placeholders);
        PlaceholderGroupsIdx = -1;
    }

    /// <summary>The index of the active group of placeholders (Monaco: <c>_placeholderGroupsIdx</c>), <c>-1</c> before the first move.</summary>
    internal int PlaceholderGroupsIdx { get; set; }

    /// <summary>Monaco: <c>_nestingLevel</c>.</summary>
    internal int NestingLevel => _nestingLevel;

    /// <summary>Tells the snippet where its text starts in the model.</summary>
    public void Initialize(TextChange textChange) => _offset = textChange.NewPosition;

    /// <summary>Removes the decorations.</summary>
    public void Dispose()
    {
        if (_placeholderDecorations is not null)
        {
            _editor.RemoveDecorations(_placeholderDecorations.Values.ToList());
        }

        _placeholderGroups.Clear();
    }

    /// <summary>True at the first tab stop, or when there is none.</summary>
    public bool IsAtFirstPlaceholder => PlaceholderGroupsIdx <= 0 || _placeholderGroups.Count == 0;

    /// <summary>True at the last tab stop.</summary>
    public bool IsAtLastPlaceholder => PlaceholderGroupsIdx == _placeholderGroups.Count - 1;

    /// <summary>True when the snippet has at least one placeholder.</summary>
    public bool HasPlaceholder => _snippet.Placeholders.Count > 0;

    /// <summary>A snippet is trivial when it has no placeholder, or only a final placeholder at its very end.</summary>
    public bool IsTrivialSnippet
    {
        get
        {
            IReadOnlyList<Placeholder> placeholders = _snippet.Placeholders;
            return placeholders.Count == 0
                || (placeholders.Count == 1 && placeholders[0].IsFinalTabstop && _snippet.RightMostDescendant == placeholders[0]);
        }
    }

    /// <summary>The number of placeholders in the active group.</summary>
    public int ActivePlaceholderCount => PlaceholderGroupsIdx < 0 ? 0 : _placeholderGroups[PlaceholderGroupsIdx].Count;

    /// <summary>True when the snippet contains a choice.</summary>
    public bool HasChoice
    {
        get
        {
            bool result = false;
            _snippet.Walk(marker =>
            {
                result = marker is Choice;
                return !result;
            });
            return result;
        }
    }

    /// <summary>The choice of the active placeholder and its range, or <see langword="null"/>.</summary>
    public (Choice Choice, Range Range)? ActiveChoice
    {
        get
        {
            if (_placeholderDecorations is null || PlaceholderGroupsIdx < 0)
            {
                return null;
            }

            Placeholder placeholder = _placeholderGroups[PlaceholderGroupsIdx][0];
            if (placeholder.Choice is not { } choice
                || !_placeholderDecorations.TryGetValue(placeholder, out string? id)
                || _editor.Model.GetDecorationRange(id) is not { } range)
            {
                return null;
            }

            return (choice, range);
        }
    }

    /// <summary>The ranges of the tab stops by index, the final tab stop excluded (Monaco: <c>computePossibleSelections</c>).</summary>
    public Dictionary<double, List<Range>> ComputePossibleSelections()
    {
        var result = new Dictionary<double, List<Range>>();
        foreach (List<Placeholder> placeholdersWithEqualIndex in _placeholderGroups)
        {
            List<Range>? ranges = null;
            foreach (Placeholder placeholder in placeholdersWithEqualIndex)
            {
                if (placeholder.IsFinalTabstop)
                {
                    // Ignore those.
                    break;
                }

                if (ranges is null)
                {
                    ranges = [];
                    result[placeholder.Index] = ranges;
                }

                string id = _placeholderDecorations![placeholder];
                if (_editor.Model.GetDecorationRange(id) is not { } range)
                {
                    // One of the placeholders lost its decoration and therefore we bail out and pretend the placeholder
                    // (with its mirrors) does not exist anymore.
                    result.Remove(placeholder.Index);
                    break;
                }

                ranges.Add(range);
            }
        }

        return result;
    }

    /// <summary>The range that contains every placeholder of the snippet.</summary>
    public Range? GetEnclosingRange()
    {
        Range? result = null;
        foreach (string decorationId in _placeholderDecorations!.Values)
        {
            Range? placeholderRange = _editor.Model.GetDecorationRange(decorationId);
            if (result is null)
            {
                result = placeholderRange;
            }
            else if (placeholderRange is { } range)
            {
                result = result.Value.Plus(range);
            }
        }

        return result;
    }

    // Monaco: groupBy(data, compare): a stable sort, then the runs of equal elements.
    private static List<List<Placeholder>> GroupByIndex(IReadOnlyList<Placeholder> placeholders)
    {
        var result = new List<List<Placeholder>>();
        foreach (Placeholder placeholder in placeholders.Order(Comparer<Placeholder>.Create(Placeholder.CompareByIndex)))
        {
            if (result.Count > 0 && Placeholder.CompareByIndex(result[^1][0], placeholder) == 0)
            {
                result[^1].Add(placeholder);
            }
            else
            {
                result.Add([placeholder]);
            }
        }

        return result;
    }

    // Monaco: _initDecorations. One decoration per placeholder, created on the first move.
    private void InitDecorations()
    {
        if (_offset == -1)
        {
            throw new InvalidOperationException("Snippet not initialized!");
        }

        if (_placeholderDecorations is not null)
        {
            // Already initialized.
            return;
        }

        var decorations = new Dictionary<Placeholder, string>();
        _placeholderDecorations = decorations;
        var model = _editor.Model;
        _editor.ChangeDecorations(accessor =>
        {
            foreach (Placeholder placeholder in _snippet.Placeholders)
            {
                int placeholderOffset = _snippet.Offset(placeholder);
                int placeholderLength = TextmateSnippet.FullLength(placeholder);
                var range = new Range(
                    model.GetPositionAt(_offset + placeholderOffset),
                    model.GetPositionAt(_offset + placeholderOffset + placeholderLength));
                decorations[placeholder] = accessor.AddDecoration(range, placeholder.IsFinalTabstop ? InactiveFinalDecoration : InactiveDecoration);
            }
        });
    }
}
