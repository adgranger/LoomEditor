// Ported from microsoft/vscode: src/vs/editor/browser/controller/mouseHandler.ts (MouseDownState)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.View.Controller;

/// <summary>The state of one mouse-down operation: the modifiers, the buttons and the click count.</summary>
/// <remarks>
/// Monaco reads <c>new Date().getTime()</c>; the clock is injected here.
/// Threading: UI thread only.
/// </remarks>
internal sealed class MouseDownState(TimeProvider timeProvider)
{
    private const int ClearMouseDownCountTime = 400; // ms

    private Position? _lastMouseDownPosition;
    private int _lastMouseDownPositionEqualCount;
    private long _lastSetMouseDownCountTime;

    public bool AltKey { get; private set; }

    public bool CtrlKey { get; private set; }

    public bool MetaKey { get; private set; }

    public bool ShiftKey { get; private set; }

    public bool LeftButton { get; private set; }

    public bool MiddleButton { get; private set; }

    public bool StartedOnLineNumbers { get; private set; }

    /// <summary>True while the press started a drag of the selection (Monaco: <c>isDragAndDrop</c>).</summary>
    public bool IsDragAndDrop { get; set; }

    /// <summary>The number of consecutive clicks.</summary>
    public int Count { get; private set; }

    public void SetModifiers(EditorMouseEvent source)
    {
        AltKey = source.AltKey;
        CtrlKey = source.CtrlKey;
        MetaKey = source.MetaKey;
        ShiftKey = source.ShiftKey;
    }

    public void SetStartButtons(EditorMouseEvent source)
    {
        LeftButton = source.LeftButton;
        MiddleButton = source.MiddleButton;
    }

    public void SetStartedOnLineNumbers(bool startedOnLineNumbers) => StartedOnLineNumbers = startedOnLineNumbers;

    public void TrySetCount(int setMouseDownCount, Position newMouseDownPosition)
    {
        // a. Invalidate multiple clicking if too much time has passed.
        long currentTime = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        if (currentTime - _lastSetMouseDownCountTime > ClearMouseDownCountTime)
        {
            setMouseDownCount = 1;
        }

        _lastSetMouseDownCountTime = currentTime;

        // b. Ensure that we don't jump from single click to triple click in one go.
        if (setMouseDownCount > Count + 1)
        {
            setMouseDownCount = Count + 1;
        }

        // c. Invalidate multiple clicking if the logical position is different.
        if (_lastMouseDownPosition is { } last && last == newMouseDownPosition)
        {
            _lastMouseDownPositionEqualCount++;
        }
        else
        {
            _lastMouseDownPositionEqualCount = 1;
        }

        _lastMouseDownPosition = newMouseDownPosition;

        // Finally set the lastMouseDownCount.
        Count = Math.Min(setMouseDownCount, _lastMouseDownPositionEqualCount);
    }
}
