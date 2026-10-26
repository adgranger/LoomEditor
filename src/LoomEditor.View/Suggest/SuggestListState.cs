// Inspired by microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggestWidget.ts (list focus)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Suggest;

/// <summary>
/// What the completion list shows: its items, the selected one and the window of visible rows (at most <see cref="MaxVisibleRows"/>).
/// Not ported: details pane, status bar, icons toggle, resizing.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class SuggestListState
{
    /// <summary>The list shows at most this many rows.</summary>
    public const int MaxVisibleRows = 12;

    /// <summary>Raised once per call that changed anything.</summary>
    public event EventHandler? Changed;

    /// <summary>Raised when the selected item changed, including to <see langword="null"/>.</summary>
    public event EventHandler<SuggestItem?>? SelectionChanged;

    /// <summary>All the items.</summary>
    public IReadOnlyList<SuggestItem> Items { get; private set; } = [];

    /// <summary>The selected index, -1 when empty.</summary>
    public int SelectedIndex { get; private set; } = -1;

    /// <summary>The first row shown.</summary>
    public int FirstVisibleIndex { get; private set; }

    /// <summary>Whether the items are the previous ones kept while nothing matches.</summary>
    public bool IsFrozen { get; private set; }

    /// <summary>Whether there are no items.</summary>
    public bool IsEmpty => Items.Count == 0;

    /// <summary>The number of rows shown.</summary>
    public int VisibleRowCount => Math.Min(Items.Count, MaxVisibleRows);

    /// <summary>The last row shown.</summary>
    public int LastVisibleIndex => FirstVisibleIndex + VisibleRowCount - 1;

    /// <summary>The selected item, if any.</summary>
    public SuggestItem? SelectedItem => SelectedIndex < 0 ? null : Items[SelectedIndex];

    /// <summary>The rows shown.</summary>
    public IReadOnlyList<SuggestItem> VisibleItems => [.. Items.Skip(FirstVisibleIndex).Take(VisibleRowCount)];

    /// <summary>Shows <paramref name="items"/> with <paramref name="selectedIndex"/> selected (clamped).</summary>
    public void Reset(IReadOnlyList<SuggestItem> items, int selectedIndex, bool isFrozen)
    {
        ArgumentNullException.ThrowIfNull(items);
        SuggestItem? previousItem = SelectedItem;
        IReadOnlyList<SuggestItem> previousItems = Items;
        int previousIndex = SelectedIndex;
        int previousFirst = FirstVisibleIndex;
        bool previousFrozen = IsFrozen;

        Items = items;
        IsFrozen = isFrozen;
        SelectedIndex = items.Count == 0 ? -1 : Math.Clamp(selectedIndex, 0, items.Count - 1);
        FirstVisibleIndex = 0;
        if (SelectedIndex >= 0)
        {
            Reveal(SelectedIndex);
        }

        bool same = previousItems.SequenceEqual(items) && previousIndex == SelectedIndex && previousFirst == FirstVisibleIndex && previousFrozen == isFrozen;
        Notify(!same, !ReferenceEquals(previousItem, SelectedItem));
    }

    /// <summary>Shows nothing.</summary>
    public void Clear() => Reset([], -1, false);

    /// <summary>Selects the next item, wrapping after the last.</summary>
    public bool SelectNext() => !IsEmpty && MoveTo((SelectedIndex + 1) % Items.Count);

    /// <summary>Selects the previous item, wrapping before the first.</summary>
    public bool SelectPrevious() => !IsEmpty && MoveTo((SelectedIndex - 1 + Items.Count) % Items.Count);

    /// <summary>Selects the first item.</summary>
    public bool SelectFirst() => !IsEmpty && MoveTo(0);

    /// <summary>Selects the last item.</summary>
    public bool SelectLast() => !IsEmpty && MoveTo(Items.Count - 1);

    /// <summary>Selects the last visible row, or when already there the item a page further.</summary>
    public bool SelectNextPage() =>
        !IsEmpty && MoveTo(SelectedIndex < LastVisibleIndex ? LastVisibleIndex : Math.Min(SelectedIndex + VisibleRowCount - 1, Items.Count - 1));

    /// <summary>Selects the first visible row, or when already there the item a page before.</summary>
    public bool SelectPreviousPage() =>
        !IsEmpty && MoveTo(SelectedIndex > FirstVisibleIndex ? FirstVisibleIndex : Math.Max(SelectedIndex - (VisibleRowCount - 1), 0));

    /// <summary>Selects the item at <paramref name="index"/>, clamped (a click or hover on a row).</summary>
    public bool Select(int index) => !IsEmpty && MoveTo(Math.Clamp(index, 0, Items.Count - 1));

    /// <summary>Moves the window by <paramref name="rows"/> without changing the selection (the mouse wheel).</summary>
    public bool ScrollBy(int rows)
    {
        if (IsEmpty)
        {
            return false;
        }

        int first = Math.Clamp(FirstVisibleIndex + rows, 0, Items.Count - VisibleRowCount);
        if (first == FirstVisibleIndex)
        {
            return false;
        }

        FirstVisibleIndex = first;
        Notify(true, false);
        return true;
    }

    /// <summary>Tells the presenter that the selected item changed in place (it was resolved).</summary>
    public void NotifyItemUpdated() => Changed?.Invoke(this, EventArgs.Empty);

    private bool MoveTo(int index)
    {
        SuggestItem? previousItem = SelectedItem;
        int previousIndex = SelectedIndex;
        int previousFirst = FirstVisibleIndex;
        SelectedIndex = index;
        Reveal(index);
        Notify(previousIndex != index || previousFirst != FirstVisibleIndex, !ReferenceEquals(previousItem, SelectedItem));
        return previousIndex != index;
    }

    private void Reveal(int index)
    {
        if (index < FirstVisibleIndex)
        {
            FirstVisibleIndex = index;
        }
        else if (index > LastVisibleIndex)
        {
            FirstVisibleIndex = index - VisibleRowCount + 1;
        }

        FirstVisibleIndex = Math.Clamp(FirstVisibleIndex, 0, Math.Max(Items.Count - VisibleRowCount, 0));
    }

    private void Notify(bool changed, bool selectionChanged)
    {
        if (selectionChanged)
        {
            SelectionChanged?.Invoke(this, SelectedItem);
        }

        if (changed || selectionChanged)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
