// Ported from microsoft/vscode: src/vs/editor/browser/coreCommands.ts (namespace EditorScroll_)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Commands;

/// <summary>The arguments of the <c>editorScroll</c> command (Monaco: <c>EditorScroll_.RawArguments</c>).</summary>
/// <param name="To">The direction: <c>"up"</c>, <c>"down"</c>, <c>"left"</c> or <c>"right"</c>.</param>
/// <param name="By">The unit: <c>"line"</c>, <c>"wrappedLine"</c>, <c>"page"</c>, <c>"halfPage"</c>, <c>"editor"</c> or <c>"column"</c>; <c>wrappedLine</c> when <see langword="null"/> or unknown.</param>
/// <param name="Value">The number of units; 0 means 1.</param>
/// <param name="RevealCursor">True to move the cursors inside the new viewport.</param>
/// <param name="Select">True to extend the selection while moving the cursor.</param>
/// <param name="Source">Who asked for the scroll.</param>
internal sealed record EditorScrollArguments(
    string To,
    string? By = null,
    int Value = 1,
    bool RevealCursor = false,
    bool Select = false,
    string? Source = null) : BaseCommandArgs(Source);

/// <summary>Directions of an editor scroll (Monaco: <c>EditorScroll_.Direction</c>).</summary>
internal enum EditorScrollDirection
{
    Up = 1,
    Right = 2,
    Down = 3,
    Left = 4,
}

/// <summary>Units of an editor scroll (Monaco: <c>EditorScroll_.Unit</c>).</summary>
internal enum EditorScrollUnit
{
    Line = 1,
    WrappedLine = 2,
    Page = 3,
    HalfPage = 4,
    Editor = 5,
    Column = 6,
}

/// <summary>The checked arguments of <c>editorScroll</c> (Monaco: <c>EditorScroll_.ParsedArguments</c>).</summary>
internal sealed record EditorScrollParsedArguments(
    EditorScrollDirection Direction,
    EditorScrollUnit Unit,
    int Value,
    bool RevealCursor,
    bool Select);

/// <summary>Parses the arguments of <c>editorScroll</c> (Monaco: <c>EditorScroll_.parse</c>; the JSON <c>metadata</c> is not ported).</summary>
/// <remarks>Threading: stateless; thread-safe.</remarks>
internal static class EditorScroll
{
    /// <summary>The arguments, or <see langword="null"/> when <see cref="EditorScrollArguments.To"/> is not a direction.</summary>
    public static EditorScrollParsedArguments? Parse(EditorScrollArguments args)
    {
        ArgumentNullException.ThrowIfNull(args);

        EditorScrollDirection direction;
        switch (args.To)
        {
            case "up":
                direction = EditorScrollDirection.Up;
                break;
            case "right":
                direction = EditorScrollDirection.Right;
                break;
            case "down":
                direction = EditorScrollDirection.Down;
                break;
            case "left":
                direction = EditorScrollDirection.Left;
                break;
            default:
                return null;
        }

        EditorScrollUnit unit = args.By switch
        {
            "line" => EditorScrollUnit.Line,
            "wrappedLine" => EditorScrollUnit.WrappedLine,
            "page" => EditorScrollUnit.Page,
            "halfPage" => EditorScrollUnit.HalfPage,
            "editor" => EditorScrollUnit.Editor,
            "column" => EditorScrollUnit.Column,
            _ => EditorScrollUnit.WrappedLine,
        };

        // Monaco: Math.floor(args.value || 1)
        int value = args.Value == 0 ? 1 : args.Value;

        return new EditorScrollParsedArguments(direction, unit, value, args.RevealCursor, args.Select);
    }
}
