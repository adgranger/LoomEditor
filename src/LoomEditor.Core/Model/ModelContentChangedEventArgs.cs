// Ported from microsoft/vscode: src/vs/editor/common/textModelEvents.ts (IModelContentChangedEvent)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model;

/// <summary>Describes a change of the text of a <see cref="TextModel"/>.</summary>
/// <remarks>
/// <para>Monaco: <c>IModelContentChangedEvent</c>, without <c>detailedReasons</c> and <c>detailedReasonsChangeLengths</c>.</para>
/// <para>Threading: immutable; raised on the thread that edits the model.</para>
/// </remarks>
public sealed class ModelContentChangedEventArgs : EventArgs
{
    internal ModelContentChangedEventArgs(
        IReadOnlyList<ModelContentChange> changes,
        string eol,
        int versionId,
        bool isUndoing,
        bool isRedoing,
        bool isFlush,
        bool isEolChange)
    {
        Changes = changes;
        Eol = eol;
        VersionId = versionId;
        IsUndoing = isUndoing;
        IsRedoing = isRedoing;
        IsFlush = isFlush;
        IsEolChange = isEolChange;
    }

    /// <summary>The changes, ordered from the end of the document to the beginning, so they are safe to apply in sequence.</summary>
    public IReadOnlyList<ModelContentChange> Changes { get; }

    /// <summary>The (new) end-of-line sequence.</summary>
    public string Eol { get; }

    /// <summary>The new version id the model has transitioned to.</summary>
    public int VersionId { get; }

    /// <summary>True when the event was generated while undoing.</summary>
    public bool IsUndoing { get; }

    /// <summary>True when the event was generated while redoing.</summary>
    public bool IsRedoing { get; }

    /// <summary>True when the model has been reset to a new value (<see cref="TextModel.SetValue"/>).</summary>
    public bool IsFlush { get; }

    /// <summary>True when the event describes an end-of-line change (<see cref="TextModel.SetEol"/>).</summary>
    public bool IsEolChange { get; }
}
