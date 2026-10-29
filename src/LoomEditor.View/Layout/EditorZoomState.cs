// Ported from microsoft/vscode: src/vs/editor/common/config/editorZoom.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>
/// The zoom level of one editor, from -5 to 20. The font size and the line height are multiplied by
/// <c>1 + level * 0.1</c> (see <see cref="BareFontInfo"/>).
/// </summary>
/// <remarks>
/// Monaco keeps one global level (<c>EditorZoom</c>); here every editor has its own (ADR 0013, decision 6).
/// Threading: UI thread only.
/// </remarks>
public sealed class EditorZoomState
{
    private const double MinLevel = -5;
    private const double MaxLevel = 20;

    /// <summary>The current level; 0 means 100 %.</summary>
    public double Level { get; private set; }

    /// <summary>Raised after <see cref="Level"/> really changed.</summary>
    public event EventHandler? Changed;

    /// <summary>Sets the level, clamped to [-5, 20]. Does nothing when the level does not change; <c>NaN</c> is ignored.</summary>
    /// <param name="level">The wanted level.</param>
    public void SetLevel(double level)
    {
        if (double.IsNaN(level))
        {
            return;
        }

        level = Math.Min(Math.Max(MinLevel, level), MaxLevel);
        if (level == Level)
        {
            return;
        }

        Level = level;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
