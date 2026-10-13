// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorMoveCommands.ts (CursorMoveCommands.selectAll)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Commands;

namespace LoomEditor.View.Features.SelectAll;

/// <summary>
/// Adds the <see cref="SelectAllCommands.SelectAll"/> command. This is the smallest possible feature and the
/// example used in docs/how-to/add-a-feature.md.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
public sealed class SelectAllFeature : IEditorFeature
{
    private IDisposable? _registration;

    /// <inheritdoc />
    public string Id => "loom.selectAll";

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _registration = context.Commands.Register(new EditorCommand(
            SelectAllCommands.SelectAll,
            () => context.Selection = ComputeSelection(context.Lines)));
    }

    /// <summary>
    /// Returns the selection covering the whole document: from (1,1) to the end of the last line,
    /// with the caret at the end (like Monaco).
    /// </summary>
    /// <remarks>Pure function: easy to test without an editor.</remarks>
    public static Selection ComputeSelection(IReadOnlyList<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        if (lines.Count == 0)
        {
            return Selection.Collapsed(new Position(1, 1));
        }

        int lastLineNumber = lines.Count;
        int lastLineMaxColumn = lines[^1].Length + 1;
        return new Selection(1, 1, lastLineNumber, lastLineMaxColumn);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _registration?.Dispose();
        _registration = null;
    }
}
