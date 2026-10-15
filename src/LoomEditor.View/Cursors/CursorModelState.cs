// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursor.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;

namespace LoomEditor.View.Cursors;

/// <summary>A snapshot of the cursors and of the model version (Monaco: <c>CursorModelState</c>).</summary>
/// <remarks>Threading: immutable; UI thread only.</remarks>
internal sealed class CursorModelState(int modelVersionId, IReadOnlyList<CursorState> cursorState)
{
    public int ModelVersionId { get; } = modelVersionId;

    public IReadOnlyList<CursorState> CursorState { get; } = cursorState;

    public static CursorModelState From(TextModel model, CursorsController cursor) =>
        new(model.VersionId, cursor.GetCursorStates());

    public bool Equals(CursorModelState? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ModelVersionId != other.ModelVersionId || CursorState.Count != other.CursorState.Count)
        {
            return false;
        }

        for (int i = 0; i < CursorState.Count; i++)
        {
            if (!CursorState[i].Equals(other.CursorState[i]))
            {
                return false;
            }
        }

        return true;
    }
}
