using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Lsp.Protocol;

namespace LoomEditor.Lsp.Editor;

/// <summary>Conversions between LSP types (zero based, UTF-16) and the editor's types (one based).</summary>
/// <remarks>Threading: pure functions; the overloads taking a model must run on the thread that owns it.</remarks>
internal static class LspConvert
{
    public static LspPosition ToLsp(Position position)
        => new() { Line = position.LineNumber - 1, Character = position.Column - 1 };

    public static LspRange ToLsp(Range range) => new() { Start = ToLsp(range.Start), End = ToLsp(range.End) };

    /// <summary>A negative value counts as 0; a line or character out of range is clamped by the model (the LSP rule).</summary>
    public static Position ToPosition(LspPosition position, TextModel model)
        => model.ValidatePosition(new Position(Math.Max(position.Line, 0) + 1, Math.Max(position.Character, 0) + 1));

    /// <summary>Both ends are validated; an end before the start is swapped.</summary>
    public static Range ToRange(LspRange range, TextModel model)
    {
        Position start = ToPosition(range.Start, model);
        Position end = ToPosition(range.End, model);
        return end < start ? new Range(end, start) : new Range(start, end);
    }
}
