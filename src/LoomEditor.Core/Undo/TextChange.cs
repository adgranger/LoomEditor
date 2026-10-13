// Ported from microsoft/vscode: src/vs/editor/common/core/textChange.ts (TextChange, escapeNewLine)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Undo;

/// <summary>
/// The offset-based record of one edit: <see cref="OldText"/> at <see cref="OldPosition"/> became
/// <see cref="NewText"/> at <see cref="NewPosition"/>. This is what the undo stack stores.
/// </summary>
/// <remarks>
/// <para>
/// Behavior difference: Monaco's binary serialization (<c>write</c>, <c>read</c>, <c>writeSize</c>) is not ported;
/// nothing in LoomEditor persists undo stacks.
/// </para>
/// <para>Threading: immutable, safe to share between threads.</para>
/// </remarks>
internal sealed class TextChange
{
    public TextChange(int oldPosition, string oldText, int newPosition, string newText)
    {
        ArgumentNullException.ThrowIfNull(oldText);
        ArgumentNullException.ThrowIfNull(newText);
        OldPosition = oldPosition;
        OldText = oldText;
        NewPosition = newPosition;
        NewText = newText;
    }

    public int OldPosition { get; }

    public string OldText { get; }

    public int NewPosition { get; }

    public string NewText { get; }

    public int OldLength => OldText.Length;

    public int OldEnd => OldPosition + OldText.Length;

    public int NewLength => NewText.Length;

    public int NewEnd => NewPosition + NewText.Length;

    public override string ToString()
    {
        if (OldText.Length == 0)
        {
            return $"(insert@{OldPosition} \"{EscapeNewLine(NewText)}\")";
        }

        if (NewText.Length == 0)
        {
            return $"(delete@{OldPosition} \"{EscapeNewLine(OldText)}\")";
        }

        return $"(replace@{OldPosition} \"{EscapeNewLine(OldText)}\" with \"{EscapeNewLine(NewText)}\")";
    }

    private static string EscapeNewLine(string str)
    {
        return str.Replace("\n", "\\n", StringComparison.Ordinal).Replace("\r", "\\r", StringComparison.Ordinal);
    }
}
