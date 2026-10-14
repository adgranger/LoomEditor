// Ported from microsoft/vscode: src/vs/editor/common/viewLayout/linesLayout.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>
/// A whitespace (a vertical gap between two lines), as seen from outside the layout.
/// </summary>
/// <remarks>Threading: implementations are not thread-safe.</remarks>
internal interface IEditorWhitespace
{
    /// <summary>The whitespace id.</summary>
    string Id { get; }

    /// <summary>The line number this whitespace comes after (0 = before the first line).</summary>
    int AfterLineNumber { get; }

    /// <summary>The height in pixels.</summary>
    int Height { get; }
}

/// <summary>
/// A whitespace stored by <see cref="LinesLayout"/>. Mutable: the layout updates the position, the height and the prefix sum.
/// </summary>
/// <remarks>Threading: not thread-safe; owned by a single <see cref="LinesLayout"/>.</remarks>
internal sealed class EditorWhitespace : IEditorWhitespace
{
    public EditorWhitespace(string id, int afterLineNumber, int ordinal, int height, int minWidth)
    {
        Id = id;
        AfterLineNumber = afterLineNumber;
        Ordinal = ordinal;
        Height = height;
        MinWidth = minWidth;
        PrefixSum = 0;
    }

    public string Id { get; set; }

    public int AfterLineNumber { get; set; }

    public int Ordinal { get; set; }

    public int Height { get; set; }

    public int MinWidth { get; set; }

    public int PrefixSum { get; set; }
}
