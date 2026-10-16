// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorMoveCommands.ts (namespace CursorMove)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Cursors;

/// <summary>Directions of the <c>cursorMove</c> command (Monaco: <c>CursorMove.Direction</c>).</summary>
/// <remarks>
/// Monaco's <c>SimpleMoveDirection</c> is the subset from <see cref="Left"/> to <see cref="WrappedLineLastNonWhitespaceCharacter"/>
/// and <c>ViewportDirection</c> the subset from <see cref="ViewPortTop"/> to <see cref="ViewPortIfOutside"/>. They are not
/// separate types here. The order of the members is Monaco's.
/// </remarks>
internal enum CursorMoveDirection
{
    Left,
    Right,
    Up,
    Down,
    PrevBlankLine,
    NextBlankLine,

    WrappedLineStart,
    WrappedLineFirstNonWhitespaceCharacter,
    WrappedLineColumnCenter,
    WrappedLineEnd,
    WrappedLineLastNonWhitespaceCharacter,

    ViewPortTop,
    ViewPortCenter,
    ViewPortBottom,

    ViewPortIfOutside,
}

/// <summary>Units of the <c>cursorMove</c> command (Monaco: <c>CursorMove.Unit</c>).</summary>
internal enum CursorMoveUnit
{
    None,
    Line,
    WrappedLine,
    Character,
    HalfLine,
    FoldedLine,
}

/// <summary>The strings of the <c>to</c> argument of <c>cursorMove</c> (Monaco: <c>CursorMove.RawDirection</c>).</summary>
internal static class CursorMoveRawDirection
{
    public const string Left = "left";
    public const string Right = "right";
    public const string Up = "up";
    public const string Down = "down";

    public const string PrevBlankLine = "prevBlankLine";
    public const string NextBlankLine = "nextBlankLine";

    public const string WrappedLineStart = "wrappedLineStart";
    public const string WrappedLineFirstNonWhitespaceCharacter = "wrappedLineFirstNonWhitespaceCharacter";
    public const string WrappedLineColumnCenter = "wrappedLineColumnCenter";
    public const string WrappedLineEnd = "wrappedLineEnd";
    public const string WrappedLineLastNonWhitespaceCharacter = "wrappedLineLastNonWhitespaceCharacter";

    public const string ViewPortTop = "viewPortTop";
    public const string ViewPortCenter = "viewPortCenter";
    public const string ViewPortBottom = "viewPortBottom";

    public const string ViewPortIfOutside = "viewPortIfOutside";
}

/// <summary>The strings of the <c>by</c> argument of <c>cursorMove</c> (Monaco: <c>CursorMove.RawUnit</c>).</summary>
internal static class CursorMoveRawUnit
{
    public const string Line = "line";
    public const string WrappedLine = "wrappedLine";
    public const string Character = "character";
    public const string HalfLine = "halfLine";
    public const string FoldedLine = "foldedLine";
}

/// <summary>The arguments of the <c>cursorMove</c> command (Monaco: <c>CursorMove.RawArguments</c>).</summary>
/// <remarks>
/// <see cref="Value"/> defaults to 1 here instead of <c>undefined</c>: <see cref="CursorMove.Parse"/> turns 0 into 1 anyway.
/// </remarks>
internal sealed record CursorMoveArguments(string To, bool Select = false, string? By = null, int Value = 1, bool NoHistory = false);

/// <summary>The parsed arguments of the <c>cursorMove</c> command (Monaco: <c>CursorMove.ParsedArguments</c>).</summary>
internal sealed record CursorMoveParsedArguments(CursorMoveDirection Direction, CursorMoveUnit Unit, bool Select, int Value, bool NoHistory);

/// <summary>Parser of the arguments of the <c>cursorMove</c> command (Monaco: <c>namespace CursorMove</c>).</summary>
/// <remarks>Threading: pure functions; thread-safe.</remarks>
internal static class CursorMove
{
    /// <summary>Parses the arguments; returns <see langword="null"/> for illegal arguments.</summary>
    public static CursorMoveParsedArguments? Parse(CursorMoveArguments args)
    {
        // Monaco: '!args.to' (undefined or empty)
        if (string.IsNullOrEmpty(args.To))
        {
            return null;
        }

        CursorMoveDirection direction;
        switch (args.To)
        {
            case CursorMoveRawDirection.Left:
                direction = CursorMoveDirection.Left;
                break;
            case CursorMoveRawDirection.Right:
                direction = CursorMoveDirection.Right;
                break;
            case CursorMoveRawDirection.Up:
                direction = CursorMoveDirection.Up;
                break;
            case CursorMoveRawDirection.Down:
                direction = CursorMoveDirection.Down;
                break;
            case CursorMoveRawDirection.PrevBlankLine:
                direction = CursorMoveDirection.PrevBlankLine;
                break;
            case CursorMoveRawDirection.NextBlankLine:
                direction = CursorMoveDirection.NextBlankLine;
                break;
            case CursorMoveRawDirection.WrappedLineStart:
                direction = CursorMoveDirection.WrappedLineStart;
                break;
            case CursorMoveRawDirection.WrappedLineFirstNonWhitespaceCharacter:
                direction = CursorMoveDirection.WrappedLineFirstNonWhitespaceCharacter;
                break;
            case CursorMoveRawDirection.WrappedLineColumnCenter:
                direction = CursorMoveDirection.WrappedLineColumnCenter;
                break;
            case CursorMoveRawDirection.WrappedLineEnd:
                direction = CursorMoveDirection.WrappedLineEnd;
                break;
            case CursorMoveRawDirection.WrappedLineLastNonWhitespaceCharacter:
                direction = CursorMoveDirection.WrappedLineLastNonWhitespaceCharacter;
                break;
            case CursorMoveRawDirection.ViewPortTop:
                direction = CursorMoveDirection.ViewPortTop;
                break;
            case CursorMoveRawDirection.ViewPortBottom:
                direction = CursorMoveDirection.ViewPortBottom;
                break;
            case CursorMoveRawDirection.ViewPortCenter:
                direction = CursorMoveDirection.ViewPortCenter;
                break;
            case CursorMoveRawDirection.ViewPortIfOutside:
                direction = CursorMoveDirection.ViewPortIfOutside;
                break;
            default:
                // illegal arguments
                return null;
        }

        CursorMoveUnit unit = args.By switch
        {
            CursorMoveRawUnit.Line => CursorMoveUnit.Line,
            CursorMoveRawUnit.WrappedLine => CursorMoveUnit.WrappedLine,
            CursorMoveRawUnit.Character => CursorMoveUnit.Character,
            CursorMoveRawUnit.HalfLine => CursorMoveUnit.HalfLine,
            CursorMoveRawUnit.FoldedLine => CursorMoveUnit.FoldedLine,
            _ => CursorMoveUnit.None,
        };

        // Monaco: 'args.value || 1' (0 becomes 1)
        return new CursorMoveParsedArguments(direction, unit, args.Select, args.Value == 0 ? 1 : args.Value, args.NoHistory);
    }
}
