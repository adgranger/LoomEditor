// Ported from microsoft/vscode: src/vs/editor/common/viewModelEventDispatcher.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;

namespace LoomEditor.View.Events;

/// <summary>The kinds of outgoing events (Monaco: <c>OutgoingViewModelEventKind</c>, only the kinds of phase 3).</summary>
internal enum OutgoingViewModelEventKind
{
    CursorStateChanged,
    ReadOnlyEditAttempt,
    FocusChanged,
    HiddenAreasChanged,
}

/// <summary>An event for the outside world, delivered once the view has handled its own events (Monaco: <c>OutgoingViewModelEvent</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal abstract class OutgoingViewModelEvent
{
    public abstract OutgoingViewModelEventKind Kind { get; }

    /// <summary>True when the event says nothing new and must not be delivered.</summary>
    public abstract bool IsNoOp();

    /// <summary>Merges <paramref name="other"/> into this event, or returns <see langword="null"/> when they cannot be merged.</summary>
    public abstract OutgoingViewModelEvent? AttemptToMerge(OutgoingViewModelEvent other);
}

/// <summary>The selections of the cursors changed (Monaco: <c>CursorStateChangedEvent</c>).</summary>
/// <remarks>Threading: immutable; UI thread only.</remarks>
internal sealed class CursorStateChangedEvent(
    IReadOnlyList<Selection>? oldSelections,
    IReadOnlyList<Selection> selections,
    int oldModelVersionId,
    int modelVersionId,
    string source,
    CursorChangeReason reason,
    bool reachedMaxCursorCount) : OutgoingViewModelEvent
{
    public override OutgoingViewModelEventKind Kind => OutgoingViewModelEventKind.CursorStateChanged;

    public IReadOnlyList<Selection>? OldSelections { get; } = oldSelections;

    public IReadOnlyList<Selection> Selections { get; } = selections;

    public int OldModelVersionId { get; } = oldModelVersionId;

    public int ModelVersionId { get; } = modelVersionId;

    public string Source { get; } = source;

    public CursorChangeReason Reason { get; } = reason;

    public bool ReachedMaxCursorCount { get; } = reachedMaxCursorCount;

    // Monaco: _selectionsAreEqual (Selection equality is Monaco's equalsSelection)
    private static bool SelectionsAreEqual(IReadOnlyList<Selection>? a, IReadOnlyList<Selection>? b)
    {
        if (a is null && b is null)
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        if (a.Count != b.Count)
        {
            return false;
        }

        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i])
            {
                return false;
            }
        }

        return true;
    }

    public override bool IsNoOp() => SelectionsAreEqual(OldSelections, Selections) && OldModelVersionId == ModelVersionId;

    public override OutgoingViewModelEvent? AttemptToMerge(OutgoingViewModelEvent other)
    {
        if (other is not CursorStateChangedEvent otherEvent)
        {
            return null;
        }

        return new CursorStateChangedEvent(
            OldSelections,
            otherEvent.Selections,
            OldModelVersionId,
            otherEvent.ModelVersionId,
            otherEvent.Source,
            otherEvent.Reason,
            ReachedMaxCursorCount || otherEvent.ReachedMaxCursorCount);
    }
}

/// <summary>Someone tried to edit a read-only editor (Monaco: <c>ReadOnlyEditAttemptEvent</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class ReadOnlyEditAttemptEvent : OutgoingViewModelEvent
{
    public override OutgoingViewModelEventKind Kind => OutgoingViewModelEventKind.ReadOnlyEditAttempt;

    public override bool IsNoOp() => false;

    public override OutgoingViewModelEvent? AttemptToMerge(OutgoingViewModelEvent other) =>
        other.Kind == Kind ? this : null;
}

/// <summary>The editor gained or lost the focus (Monaco: <c>FocusChangedEvent</c>).</summary>
/// <remarks>Threading: immutable; UI thread only.</remarks>
internal sealed class FocusChangedEvent(bool oldHasFocus, bool hasFocus) : OutgoingViewModelEvent
{
    public override OutgoingViewModelEventKind Kind => OutgoingViewModelEventKind.FocusChanged;

    public bool OldHasFocus { get; } = oldHasFocus;

    public bool HasFocus { get; } = hasFocus;

    public override bool IsNoOp() => OldHasFocus == HasFocus;

    public override OutgoingViewModelEvent? AttemptToMerge(OutgoingViewModelEvent other) =>
        other is FocusChangedEvent otherEvent ? new FocusChangedEvent(OldHasFocus, otherEvent.HasFocus) : null;
}

/// <summary>The hidden areas changed the line mapping (Monaco: <c>HiddenAreasChangedEvent</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class HiddenAreasChangedEvent : OutgoingViewModelEvent
{
    public override OutgoingViewModelEventKind Kind => OutgoingViewModelEventKind.HiddenAreasChanged;

    public override bool IsNoOp() => false;

    public override OutgoingViewModelEvent? AttemptToMerge(OutgoingViewModelEvent other) =>
        other.Kind == Kind ? this : null;
}
