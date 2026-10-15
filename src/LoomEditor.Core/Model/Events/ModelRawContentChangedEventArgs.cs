// Ported from microsoft/vscode: src/vs/editor/common/textModelEvents.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.Core.Model.Events;

/// <summary>Describes a change of the text of a <see cref="TextModel"/> per line (Monaco: <c>ModelRawContentChangedEvent</c>).</summary>
/// <remarks>
/// <para>Raised before <see cref="TextModel.ContentChanged"/>. <c>merge</c> is not ported yet.</para>
/// <para>Threading: immutable; raised on the thread that edits the model.</para>
/// </remarks>
public sealed class ModelRawContentChangedEventArgs : EventArgs
{
    internal ModelRawContentChangedEventArgs(IReadOnlyList<ModelRawChange> changes, int versionId, bool isUndoing, bool isRedoing, IReadOnlyList<Selection>? resultingSelection = null)
    {
        Changes = changes;
        VersionId = versionId;
        IsUndoing = isUndoing;
        IsRedoing = isRedoing;
        ResultingSelection = resultingSelection;
    }

    /// <summary>The changes.</summary>
    public IReadOnlyList<ModelRawChange> Changes { get; }

    /// <summary>The new version id the model has transitioned to.</summary>
    public int VersionId { get; }

    /// <summary>True when the event was generated while undoing.</summary>
    public bool IsUndoing { get; }

    /// <summary>True when the event was generated while redoing.</summary>
    public bool IsRedoing { get; }

    /// <summary>The selections to restore after an undo or a redo, or <see langword="null"/> (Monaco: <c>resultingSelection</c>).</summary>
    public IReadOnlyList<Selection>? ResultingSelection { get; }

    /// <summary>True when one of the changes is of the given kind (Monaco: <c>containsEvent</c>).</summary>
    public bool ContainsEvent(RawContentChangedType type)
    {
        foreach (ModelRawChange change in Changes)
        {
            if (change.ChangeType == type)
            {
                return true;
            }
        }

        return false;
    }
}
