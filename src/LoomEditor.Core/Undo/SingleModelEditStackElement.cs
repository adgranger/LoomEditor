// Ported from microsoft/vscode: src/vs/editor/common/model/editStack.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Undo;

/// <summary>One undo step of a model. While it is open, later edits are appended to it; once closed, they start a new step.</summary>
/// <remarks>
/// <para>
/// Behavior difference: Monaco serializes the data when the element is closed (to save memory) and deserializes it
/// when it is opened again. Here the data stays as it is and "open" is only a flag.
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal sealed class SingleModelEditStackElement
{
    public SingleModelEditStackElement(TextModel model, IReadOnlyList<Selection>? beforeCursorState)
    {
        Model = model;
        Data = SingleModelEditStackData.Create(model, beforeCursorState);
        IsOpen = true;
    }

    public TextModel Model { get; }

    public SingleModelEditStackData Data { get; }

    public bool IsOpen { get; private set; }

    public bool CanAppend(TextModel model) => ReferenceEquals(Model, model) && IsOpen;

    public void Append(
        IReadOnlyList<TextChange> textChanges,
        EndOfLineSequence afterEol,
        int afterVersionId,
        IReadOnlyList<Selection>? afterCursorState)
    {
        if (IsOpen)
        {
            Data.Append(textChanges, afterEol, afterVersionId, afterCursorState);
        }
    }

    public void Close() => IsOpen = false;

    public void Open() => IsOpen = true;

    public override string ToString() => string.Join(", ", Data.Changes.Select(static change => change.ToString()));
}
