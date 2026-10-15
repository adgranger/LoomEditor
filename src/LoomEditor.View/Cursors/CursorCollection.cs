// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorCollection.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors;

/// <summary>The primary cursor and the secondary cursors of an editor (Monaco: <c>CursorCollection</c>).</summary>
/// <remarks>
/// <para>
/// <c>_lastAddedCursorIndex</c>: Monaco's comment says it indexes the secondary cursors, but the code stores a
/// full-array index (<c>cursors.length - 1</c>). This port follows the code.
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal sealed partial class CursorCollection : IDisposable
{
    private CursorContext _context;

    // _cursors[0] is the primary cursor, thus _cursors.Count >= 1 is always true. _cursors[1..] are secondary cursors.
    private readonly List<Cursor> _cursors;

    // An index which identifies the last cursor that was added / moved (think Ctrl+drag).
    private int _lastAddedCursorIndex;

    public CursorCollection(CursorContext context)
    {
        _context = context;
        _cursors = [new Cursor(context)];
        _lastAddedCursorIndex = 0;
    }

    public void Dispose()
    {
        foreach (Cursor cursor in _cursors)
        {
            cursor.Dispose(_context);
        }
    }

    public void StartTrackingSelections()
    {
        foreach (Cursor cursor in _cursors)
        {
            cursor.StartTrackingSelection(_context);
        }
    }

    public void StopTrackingSelections()
    {
        foreach (Cursor cursor in _cursors)
        {
            cursor.StopTrackingSelection(_context);
        }
    }

    public void UpdateContext(CursorContext context) => _context = context;

    public void EnsureValidState()
    {
        foreach (Cursor cursor in _cursors)
        {
            cursor.EnsureValidState(_context);
        }
    }

    public List<Selection> ReadSelectionFromMarkers() => _cursors.Select(c => c.ReadSelectionFromMarkers(_context)).ToList();

    public List<CursorState> GetAll() => _cursors.Select(c => c.AsCursorState()).ToList();

    public List<Position> GetViewPositions() => _cursors.Select(c => c.ViewState.Position).ToList();

    public Position GetTopMostViewPosition()
    {
        // Monaco: findFirstMin: the first cursor of the minimum position
        Position result = _cursors[0].ViewState.Position;
        for (int i = 1; i < _cursors.Count; i++)
        {
            Position position = _cursors[i].ViewState.Position;
            if (position < result)
            {
                result = position;
            }
        }

        return result;
    }

    public Position GetBottomMostViewPosition()
    {
        // Monaco: findLastMax: the last cursor of the maximum position
        Position result = _cursors[0].ViewState.Position;
        for (int i = 1; i < _cursors.Count; i++)
        {
            Position position = _cursors[i].ViewState.Position;
            if (position >= result)
            {
                result = position;
            }
        }

        return result;
    }

    public List<Selection> GetSelections() => _cursors.Select(c => c.ModelState.Selection).ToList();

    public List<Selection> GetViewSelections() => _cursors.Select(c => c.ViewState.Selection).ToList();

    public void SetSelections(IReadOnlyList<Selection> selections) => SetStates(CursorState.FromModelSelections(selections));

    public CursorState GetPrimaryCursor() => _cursors[0].AsCursorState();

    public void SetStates(IReadOnlyList<PartialCursorState>? states)
    {
        if (states is null)
        {
            return;
        }

        _cursors[0].SetState(_context, states[0].ModelState, states[0].ViewState);
        SetSecondaryStates(states.Skip(1).ToList());
    }

    // Creates or disposes secondary cursors as necessary to match the number of secondary states.
    private void SetSecondaryStates(List<PartialCursorState> secondaryStates)
    {
        int secondaryCursorsLength = _cursors.Count - 1;
        int secondaryStatesLength = secondaryStates.Count;

        if (secondaryCursorsLength < secondaryStatesLength)
        {
            int createCnt = secondaryStatesLength - secondaryCursorsLength;
            for (int i = 0; i < createCnt; i++)
            {
                AddSecondaryCursor();
            }
        }
        else if (secondaryCursorsLength > secondaryStatesLength)
        {
            int removeCnt = secondaryCursorsLength - secondaryStatesLength;
            for (int i = 0; i < removeCnt; i++)
            {
                RemoveSecondaryCursor(_cursors.Count - 2);
            }
        }

        for (int i = 0; i < secondaryStatesLength; i++)
        {
            _cursors[i + 1].SetState(_context, secondaryStates[i].ModelState, secondaryStates[i].ViewState);
        }
    }

    public void KillSecondaryCursors() => SetSecondaryStates([]);

    private void AddSecondaryCursor()
    {
        _cursors.Add(new Cursor(_context));
        _lastAddedCursorIndex = _cursors.Count - 1;
    }

    public int GetLastAddedCursorIndex()
    {
        if (_cursors.Count == 1 || _lastAddedCursorIndex == 0)
        {
            return 0;
        }

        return _lastAddedCursorIndex;
    }

    private void RemoveSecondaryCursor(int removeIndex)
    {
        if (_lastAddedCursorIndex >= removeIndex + 1)
        {
            _lastAddedCursorIndex--;
        }

        _cursors[removeIndex + 1].Dispose(_context);
        _cursors.RemoveAt(removeIndex + 1);
    }
}
