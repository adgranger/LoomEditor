// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/components/diffEditorViewZones/diffEditorViewZones.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Diagnostics;
using LoomEditor.Core.Text;
using LoomEditor.View.DiffEditor;
using LoomEditor.View.Layout;
using Microsoft.UI.Dispatching;

namespace LoomEditor.WinUI.Controls.DiffEditor;

/// <summary>
/// Keeps the two sides of a side-by-side diff level: an empty zone on the shorter side of every aligned block, and the scroll
/// positions of both editors in sync (Monaco: <c>DiffEditorViewZones</c>, side-by-side part).
/// </summary>
/// <remarks>
/// <para>
/// Deliberate difference: recomputation is event driven and coalesced in one dispatcher tick, instead of observables; a pass that
/// would produce the zones already in place is skipped, which ends the layout feedback loop. Not ported: the inline view (zones of
/// deleted lines), moved text padding and the animated scroll offsets.
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal sealed class DiffEditorViewZones : IDisposable
{
    private const int MaxPasses = 3;

    private readonly LoomEditor _original;
    private readonly LoomEditor _modified;
    private readonly DiffEditorViewModel _viewModel;
    private readonly DispatcherQueue _dispatcher;
    private readonly HashSet<string> _originalOwn = [];
    private readonly HashSet<string> _modifiedOwn = [];
    private List<(int After, int Height)> _originalSpecs = [];
    private List<(int After, int Height)> _modifiedSpecs = [];
    private bool _isUpdatingZones;
    private bool _isSyncing;
    private bool _recomputePending;
    private bool _isDisposed;
    private bool _warned;

    /// <summary>Starts listening and computes the zones of the current diff.</summary>
    public DiffEditorViewZones(LoomEditor original, LoomEditor modified, DiffEditorViewModel viewModel, DispatcherQueue dispatcher)
    {
        _original = original;
        _modified = modified;
        _viewModel = viewModel;
        _dispatcher = dispatcher;
        foreach (LoomEditor editor in new[] { original, modified })
        {
            editor.ViewZones.ZonesChanged += OnTrigger;
            editor.HostLayoutChanged += OnTrigger;
            editor.HostScrollChanged += OnScrollChanged;
        }

        _viewModel.DiffChanged += OnTrigger;
        Schedule();
    }

    /// <summary>Recomputes the zones (an option changed).</summary>
    public void Invalidate() => Schedule();

    /// <summary>Stops listening and removes the zones.</summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        foreach (LoomEditor editor in new[] { _original, _modified })
        {
            editor.ViewZones.ZonesChanged -= OnTrigger;
            editor.HostLayoutChanged -= OnTrigger;
            editor.HostScrollChanged -= OnScrollChanged;
        }

        _viewModel.DiffChanged -= OnTrigger;
        Apply(_original, _originalOwn, []);
        Apply(_modified, _modifiedOwn, []);
    }

    private void OnTrigger(object? sender, EventArgs e)
    {
        if (!_isUpdatingZones)
        {
            Schedule();
        }
    }

    private void Schedule()
    {
        if (_recomputePending || _isDisposed)
        {
            return;
        }

        _recomputePending = _dispatcher.TryEnqueue(() =>
        {
            _recomputePending = false;
            Recompute();
        });
    }

    private void Recompute()
    {
        if (_isDisposed || _viewModel.Diff is not { } diff)
        {
            return;
        }

        // The zones of a pass change the scroll height, which may re-trigger a pass; an unchanged result ends the loop.
        for (int pass = 0; pass < MaxPasses; pass++)
        {
            ComputeSpecs(diff, out List<(int, int)> originalSpecs, out List<(int, int)> modifiedSpecs);
            if (originalSpecs.SequenceEqual(_originalSpecs) && modifiedSpecs.SequenceEqual(_modifiedSpecs))
            {
                return;
            }

            _isUpdatingZones = true;
            try
            {
                Apply(_original, _originalOwn, originalSpecs);
                Apply(_modified, _modifiedOwn, modifiedSpecs);
            }
            finally
            {
                _isUpdatingZones = false;
            }

            _originalSpecs = originalSpecs;
            _modifiedSpecs = modifiedSpecs;
        }

        if (!_warned)
        {
            _warned = true;
            Debug.WriteLine($"DiffEditorViewZones: the zones did not settle after {MaxPasses} passes.");
        }
    }

    private void ComputeSpecs(DiffState diff, out List<(int, int)> originalSpecs, out List<(int, int)> modifiedSpecs)
    {
        originalSpecs = [];
        modifiedSpecs = [];
        var originalModel = _original.Model;

        IReadOnlyList<LineRangeAlignment> alignments = LineRangeAlignments.Compute(
            diff.Mappings,
            Extra(_original, _originalOwn),
            Extra(_modified, _modifiedOwn),
            LineHeightOf(_original),
            LineHeightOf(_modified),
            originalModel.GetLineMaxColumn,
            originalModel.LineCount,
            innerHunkAlignment: true);

        // Monaco: delta = modifiedHeight - originalHeight; a positive delta pads the original side.
        foreach (LineRangeAlignment a in alignments)
        {
            int delta = a.ModifiedHeightInPx - a.OriginalHeightInPx;
            if (delta > 0)
            {
                originalSpecs.Add((a.OriginalRange.EndLineNumberExclusive - 1, delta));
            }
            else if (delta < 0)
            {
                modifiedSpecs.Add((a.ModifiedRange.EndLineNumberExclusive - 1, -delta));
            }
        }
    }

    private static int LineHeightOf(LoomEditor editor) => (int)Math.Round(editor.LineHeight);

    private static IReadOnlyList<AdditionalLineHeight> Extra(LoomEditor editor, HashSet<string> own)
    {
        ICoordinatesConverter converter = editor.CoordinatesConverter;
        return AdditionalLineHeights.Compute(
            editor.GetWhitespaces().Select(w => (w.Id, w.AfterLineNumber, w.Height)),
            own,
            viewLine => converter.ConvertViewPositionToModelPosition(new Position(viewLine, 1)).LineNumber,
            editor.Model.LineCount,
            editor.IsWrapping ? converter.GetModelLineViewLineCount : null,
            LineHeightOf(editor));
    }

    // Monaco: applyViewZones. One change removes the previous zones of this class and adds the new ones.
    private static void Apply(LoomEditor editor, HashSet<string> own, List<(int After, int Height)> specs)
    {
        HashSet<string> previous = [.. own];
        own.Clear();
        editor.ViewZones.Change(accessor =>
        {
            foreach (string id in previous)
            {
                accessor.RemoveZone(id);
            }

            foreach ((int after, int height) in specs)
            {
                own.Add(accessor.AddZone(new ViewZone
                {
                    AfterLineNumber = after,
                    HeightInPx = height,
                    Kind = ViewZoneKind.Empty,
                    ShowInHiddenAreas = true,
                    SuppressMouseDown = true,
                }));
            }
        });
    }

    private void OnScrollChanged(object? sender, HostScrollEventArgs e)
    {
        if (e.ScrollHeightChanged && !_isUpdatingZones)
        {
            Schedule();
        }

        if (_isSyncing || (!e.ScrollTopChanged && !e.ScrollLeftChanged))
        {
            return;
        }

        LoomEditor target = ReferenceEquals(sender, _original) ? _modified : _original;
        _isSyncing = true;
        try
        {
            target.SetScrollPositionImmediate(e.ScrollLeftChanged ? e.ScrollLeft : null, e.ScrollTopChanged ? e.ScrollTop : null);
        }
        finally
        {
            _isSyncing = false;
        }
    }
}
