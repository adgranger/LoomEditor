// Ported from microsoft/vscode: src/vs/editor/browser/controller/mouseHandler.ts (_setupMouseWheelZoomListener)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Controller;

/// <summary>
/// Turns wheel events into zoom levels: one step per notch for a physical wheel, an accumulated smooth zoom for a
/// touchpad gesture (events less than 50 ms apart).
/// </summary>
/// <remarks>Threading: UI thread only. The time is a parameter so that tests control it.</remarks>
public sealed class MouseWheelZoomGesture
{
    private const long GestureGapMs = 50;

    private long _prevTime = -GestureGapMs - 1;
    private double _gestureStartLevel;
    private bool _gestureHasModifiers;
    private double _gestureAccumulatedDelta;

    /// <summary>Handles one wheel event.</summary>
    /// <param name="deltaY">Notches, positive when the wheel moves forward (up).</param>
    /// <param name="isPhysicalWheel">Whether the classifier says the event comes from a physical wheel.</param>
    /// <param name="ctrl">Whether Ctrl is down.</param>
    /// <param name="shift">Whether Shift is down.</param>
    /// <param name="alt">Whether Alt is down.</param>
    /// <param name="win">Whether the Windows key is down.</param>
    /// <param name="timestampMs">The time of the event, in milliseconds.</param>
    /// <param name="currentLevel">The current zoom level.</param>
    /// <returns>The new zoom level, or <see langword="null"/> when the event is not a zoom.</returns>
    public double? OnWheel(
        double deltaY, bool isPhysicalWheel, bool ctrl, bool shift, bool alt, bool win, long timestampMs, double currentLevel)
    {
        // Monaco: hasMouseWheelZoomModifiers, Windows and Linux branch.
        bool hasModifiers = ctrl && !win && !shift && !alt;

        if (isPhysicalWheel)
        {
            return hasModifiers ? currentLevel + (deltaY > 0 ? 1 : -1) : null;
        }

        // Events less than 50 ms apart are one gesture; Ctrl pressed during the inertia phase does not count.
        if (timestampMs - _prevTime > GestureGapMs)
        {
            _gestureStartLevel = currentLevel;
            _gestureHasModifiers = hasModifiers;
            _gestureAccumulatedDelta = 0;
        }

        _prevTime = timestampMs;
        _gestureAccumulatedDelta += deltaY;

        return _gestureHasModifiers ? _gestureStartLevel + (_gestureAccumulatedDelta / 5) : null;
    }
}
