// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/diffEditorViewModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Diagnostics;
using LoomEditor.Core.Diff;
using LoomEditor.Core.Text;

namespace LoomEditor.View.DiffEditor;

/// <summary>
/// A run of unchanged lines between two changes that can be collapsed, with how many of its lines stay visible at
/// the top and at the bottom (Monaco: <c>UnchangedRegion</c>). Monaco's observables are replaced by <see cref="Changed"/>,
/// raised once per mutating call and only when a value changed.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class UnchangedRegion
{
    private UnchangedRegionDragSide? _isDragged;

    /// <summary>Creates a region; the visible counts are clamped like Monaco does.</summary>
    public UnchangedRegion(int originalLineNumber, int modifiedLineNumber, int lineCount, int visibleLineCountTop, int visibleLineCountBottom)
    {
        OriginalLineNumber = originalLineNumber;
        ModifiedLineNumber = modifiedLineNumber;
        LineCount = lineCount;

        var top = Math.Max(Math.Min(visibleLineCountTop, lineCount), 0);
        var bottom = Math.Max(Math.Min(visibleLineCountBottom, lineCount - visibleLineCountTop), 0);
        Guard.Assert(visibleLineCountTop == top, "visibleLineCountTop out of range");
        Guard.Assert(visibleLineCountBottom == bottom, "visibleLineCountBottom out of range");
        VisibleLineCountTop = top;
        VisibleLineCountBottom = bottom;
    }

    /// <summary>Raised once per mutating call, only when a value changed.</summary>
    public event EventHandler? Changed;

    /// <summary>First line of the region in the original model.</summary>
    public int OriginalLineNumber { get; }

    /// <summary>First line of the region in the modified model.</summary>
    public int ModifiedLineNumber { get; }

    /// <summary>Number of lines in the region.</summary>
    public int LineCount { get; }

    /// <summary>Lines visible at the top of the region.</summary>
    public int VisibleLineCountTop { get; private set; }

    /// <summary>Lines visible at the bottom of the region.</summary>
    public int VisibleLineCountBottom { get; private set; }

    /// <summary>The edge being dragged, if any.</summary>
    public UnchangedRegionDragSide? IsDragged
    {
        get => _isDragged;
        set
        {
            if (_isDragged != value)
            {
                _isDragged = value;
                RaiseChanged();
            }
        }
    }

    /// <summary>The region in the original model.</summary>
    public LineRange OriginalUnchangedRange => LineRange.OfLength(OriginalLineNumber, LineCount);

    /// <summary>The region in the modified model.</summary>
    public LineRange ModifiedUnchangedRange => LineRange.OfLength(ModifiedLineNumber, LineCount);

    /// <summary>True when every line is visible and nothing is dragged.</summary>
    public bool ShouldHideControls => VisibleLineCountTop + VisibleLineCountBottom == LineCount && IsDragged is null;

    /// <summary>Monaco: <c>UnchangedRegion.fromDiffs</c>.</summary>
    public static IReadOnlyList<UnchangedRegion> FromDiffs(
        IReadOnlyList<DetailedLineRangeMapping> changes, int originalLineCount, int modifiedLineCount, int minHiddenLineCount, int minContext)
    {
        var inversed = LineRangeMapping.Inverse(changes, originalLineCount, modifiedLineCount);
        var result = new List<UnchangedRegion>();

        foreach (var mapping in inversed)
        {
            var origStart = mapping.Original.StartLineNumber;
            var modStart = mapping.Modified.StartLineNumber;
            var length = mapping.Original.Length;

            var atStart = origStart == 1 && modStart == 1;
            var atEnd = origStart + length == originalLineCount + 1 && modStart + length == modifiedLineCount + 1;

            if ((atStart || atEnd) && length >= minContext + minHiddenLineCount)
            {
                if (atStart && !atEnd)
                {
                    length -= minContext;
                }

                if (atEnd && !atStart)
                {
                    origStart += minContext;
                    modStart += minContext;
                    length -= minContext;
                }

                result.Add(new UnchangedRegion(origStart, modStart, length, 0, 0));
            }
            else if (length >= (minContext * 2) + minHiddenLineCount)
            {
                origStart += minContext;
                modStart += minContext;
                length -= minContext * 2;
                result.Add(new UnchangedRegion(origStart, modStart, length, 0, 0));
            }
        }

        return result;
    }

    /// <summary>Splits this region around the visible ranges; returns <c>this</c> (fully shown) when nothing is hidden.</summary>
    public IReadOnlyList<UnchangedRegion> SetVisibleRanges(IReadOnlyList<LineRangeMapping> visibleRanges)
    {
        var result = new List<UnchangedRegion>();
        var hiddenModified = new LineRangeSet([.. visibleRanges.Select(r => r.Modified)]).SubtractFrom(ModifiedUnchangedRange);

        var originalStart = OriginalLineNumber;
        var modifiedStart = ModifiedLineNumber;
        var modifiedEndEx = ModifiedLineNumber + LineCount;
        if (hiddenModified.Ranges.Count == 0)
        {
            ShowAll();
            result.Add(this);
        }
        else
        {
            for (var i = 0; i < hiddenModified.Ranges.Count; i++)
            {
                var r = hiddenModified.Ranges[i];
                var isLast = i == hiddenModified.Ranges.Count - 1;
                var length = (isLast ? modifiedEndEx : r.EndLineNumberExclusive) - modifiedStart;

                var newRegion = new UnchangedRegion(originalStart, modifiedStart, length, 0, 0);
                newRegion.SetHiddenModifiedRange(r);
                result.Add(newRegion);

                originalStart = newRegion.OriginalUnchangedRange.EndLineNumberExclusive;
                modifiedStart = newRegion.ModifiedUnchangedRange.EndLineNumberExclusive;
            }
        }

        return result;
    }

    /// <summary>The hidden lines in the original model.</summary>
    public LineRange GetHiddenOriginalRange() =>
        LineRange.OfLength(OriginalLineNumber + VisibleLineCountTop, LineCount - VisibleLineCountTop - VisibleLineCountBottom);

    /// <summary>The hidden lines in the modified model.</summary>
    public LineRange GetHiddenModifiedRange() =>
        LineRange.OfLength(ModifiedLineNumber + VisibleLineCountTop, LineCount - VisibleLineCountTop - VisibleLineCountBottom);

    /// <summary>Makes visible everything but <paramref name="range"/> (modified line numbers).</summary>
    public void SetHiddenModifiedRange(LineRange range) =>
        SetState(range.StartLineNumber - ModifiedLineNumber, ModifiedLineNumber + LineCount - range.EndLineNumberExclusive);

    /// <summary>The largest top count that keeps the bottom count.</summary>
    public int GetMaxVisibleLineCountTop() => LineCount - VisibleLineCountBottom;

    /// <summary>The largest bottom count that keeps the top count.</summary>
    public int GetMaxVisibleLineCountBottom() => LineCount - VisibleLineCountTop;

    /// <summary>Reveals <paramref name="count"/> more lines at the top.</summary>
    public void ShowMoreAbove(int count = 10) =>
        SetCounts(Math.Min(VisibleLineCountTop + count, GetMaxVisibleLineCountTop()), VisibleLineCountBottom);

    /// <summary>Reveals <paramref name="count"/> more lines at the bottom.</summary>
    public void ShowMoreBelow(int count = 10) =>
        SetCounts(VisibleLineCountTop, Math.Min(VisibleLineCountBottom + count, GetMaxVisibleLineCountBottom()));

    /// <summary>Reveals every line.</summary>
    public void ShowAll() => SetCounts(VisibleLineCountTop, LineCount - VisibleLineCountTop);

    /// <summary>Reveals a modified line (and the lines between it and the chosen side).</summary>
    public void ShowModifiedLine(int lineNumber, RevealPreference preference)
    {
        var top = lineNumber + 1 - (ModifiedLineNumber + VisibleLineCountTop);
        var bottom = ModifiedLineNumber - VisibleLineCountBottom + LineCount - lineNumber;
        if ((preference == RevealPreference.FromCloserSide && top < bottom) || preference == RevealPreference.FromTop)
        {
            SetCounts(VisibleLineCountTop + top, VisibleLineCountBottom);
        }
        else
        {
            SetCounts(VisibleLineCountTop, VisibleLineCountBottom + bottom);
        }
    }

    /// <summary>Reveals an original line (and the lines between it and the chosen side).</summary>
    public void ShowOriginalLine(int lineNumber, RevealPreference preference)
    {
        var top = lineNumber - OriginalLineNumber;
        var bottom = OriginalLineNumber + LineCount - lineNumber;
        if ((preference == RevealPreference.FromCloserSide && top < bottom) || preference == RevealPreference.FromTop)
        {
            SetCounts(Math.Min(VisibleLineCountTop + bottom - top, GetMaxVisibleLineCountTop()), VisibleLineCountBottom);
        }
        else
        {
            SetCounts(VisibleLineCountTop, Math.Min(VisibleLineCountBottom + top - bottom, GetMaxVisibleLineCountBottom()));
        }
    }

    /// <summary>Hides every line.</summary>
    public void CollapseAll() => SetCounts(0, 0);

    /// <summary>Sets both visible counts, clamped to the region.</summary>
    public void SetState(int visibleLineCountTop, int visibleLineCountBottom)
    {
        visibleLineCountTop = Math.Max(Math.Min(visibleLineCountTop, LineCount), 0);
        visibleLineCountBottom = Math.Max(Math.Min(visibleLineCountBottom, LineCount - visibleLineCountTop), 0);
        SetCounts(visibleLineCountTop, visibleLineCountBottom);
    }

    private void SetCounts(int top, int bottom)
    {
        if (top == VisibleLineCountTop && bottom == VisibleLineCountBottom)
        {
            return;
        }

        VisibleLineCountTop = top;
        VisibleLineCountBottom = bottom;
        RaiseChanged();
    }

    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
