// Ported from microsoft/vscode: src/vs/editor/browser/editorBrowser.ts (MouseTargetType) and src/vs/editor/browser/controller/mouseTarget.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Controller;

/// <summary>
/// What is under the mouse (Monaco: <c>MouseTargetType</c>, the members the canvas can hit-test; same values).
/// Scrollbars, widgets, view zones, the minimap and overlays are not hit-tested on the canvas.
/// </summary>
/// <remarks>Threading: an enum; thread-safe.</remarks>
internal enum MouseTargetType
{
    /// <summary>Unknown.</summary>
    Unknown = 0,

    /// <summary>The glyph margin of the gutter.</summary>
    GutterGlyphMargin = 2,

    /// <summary>The line numbers of the gutter.</summary>
    GutterLineNumbers = 3,

    /// <summary>The line decorations of the gutter.</summary>
    GutterLineDecorations = 4,

    /// <summary>Text of a line.</summary>
    ContentText = 6,

    /// <summary>Empty space after a line or after the last line.</summary>
    ContentEmpty = 7,

    /// <summary>Outside the editor (used by mouse drags).</summary>
    OutsideEditor = 13,
}

/// <summary>The result of a hit test (Monaco: <c>IMouseTarget</c>, with the data of the target types above).</summary>
/// <param name="Type">What was hit.</param>
/// <param name="Position">The position of the target, if any.</param>
/// <param name="MouseColumn">The 1-based column of the mouse, counted in typical character widths (Monaco: <c>mouseColumn</c>).</param>
/// <param name="Range">The range of the target, if any: the whole line for the gutter, the position for text.</param>
/// <param name="IsAfterLines">For the gutter and empty content: the mouse is below the last line.</param>
/// <param name="HorizontalDistanceToText">For empty content inside a line: the distance from the end of the text.</param>
/// <param name="OutsidePosition">For <see cref="MouseTargetType.OutsideEditor"/>: on which side of the editor the mouse is.</param>
/// <param name="OutsideDistance">For <see cref="MouseTargetType.OutsideEditor"/>: the distance to the editor, in DIPs.</param>
/// <param name="InjectedText">For text: the injected text under the mouse, if any.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record MouseTarget(
    MouseTargetType Type,
    Position? Position,
    int MouseColumn,
    Range? Range,
    bool IsAfterLines,
    double? HorizontalDistanceToText,
    OutsidePosition? OutsidePosition = null,
    double OutsideDistance = 0,
    InjectedText? InjectedText = null)
{
    /// <summary>Monaco: <c>MouseTarget.createOutsideEditor</c>.</summary>
    public static MouseTarget CreateOutsideEditor(int mouseColumn, Position position, OutsidePosition outsidePosition, double outsideDistance) =>
        new(MouseTargetType.OutsideEditor, position, mouseColumn, null, false, null, outsidePosition, outsideDistance);
}

/// <summary>Where the mouse is relative to the editor (Monaco: <c>IMouseTargetOutsideEditor.outsidePosition</c>).</summary>
/// <remarks>Threading: an enum; thread-safe.</remarks>
internal enum OutsidePosition
{
    /// <summary>Above the editor.</summary>
    Above,

    /// <summary>Below the editor.</summary>
    Below,

    /// <summary>Left of the content.</summary>
    Left,

    /// <summary>Right of the content.</summary>
    Right,
}
