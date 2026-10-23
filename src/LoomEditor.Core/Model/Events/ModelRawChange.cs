// Ported from microsoft/vscode: src/vs/editor/common/textModelEvents.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.Events;

/// <summary>A change of the model described per line (Monaco: <c>ModelRawChange</c>).</summary>
/// <remarks>
/// <para>The <c>injectedText</c> fields of Monaco are left out: injected text changes come through <c>TextModel.InjectedTextChanged</c>.</para>
/// <para>Threading: immutable.</para>
/// </remarks>
public abstract record ModelRawChange
{
    private protected ModelRawChange()
    {
    }

    /// <summary>The kind of this change.</summary>
    public abstract RawContentChangedType ChangeType { get; }
}

/// <summary>The model has been reset to a new value (Monaco: <c>ModelRawFlush</c>).</summary>
/// <remarks>Threading: immutable.</remarks>
public sealed record ModelRawFlush : ModelRawChange
{
    /// <inheritdoc/>
    public override RawContentChangedType ChangeType => RawContentChangedType.Flush;
}

/// <summary>A line has changed (Monaco: <c>ModelRawLineChanged</c>).</summary>
/// <remarks>Threading: immutable.</remarks>
/// <param name="LineNumber">The line number that has changed (before the change was applied).</param>
/// <param name="LineNumberPostEdit">The new line number the old one is mapped to (after the change was applied).</param>
public sealed record ModelRawLineChanged(int LineNumber, int LineNumberPostEdit) : ModelRawChange
{
    /// <inheritdoc/>
    public override RawContentChangedType ChangeType => RawContentChangedType.LineChanged;
}

/// <summary>Lines have been deleted (Monaco: <c>ModelRawLinesDeleted</c>).</summary>
/// <remarks>Threading: immutable.</remarks>
/// <param name="FromLineNumber">At what line the deletion began (inclusive).</param>
/// <param name="ToLineNumber">At what line the deletion stopped (inclusive).</param>
/// <param name="LastUntouchedLinePostEdit">The last unmodified line in the updated buffer after the deletion is made.</param>
public sealed record ModelRawLinesDeleted(int FromLineNumber, int ToLineNumber, int LastUntouchedLinePostEdit) : ModelRawChange
{
    /// <inheritdoc/>
    public override RawContentChangedType ChangeType => RawContentChangedType.LinesDeleted;
}

/// <summary>Lines have been inserted (Monaco: <c>ModelRawLinesInserted</c>).</summary>
/// <remarks>Threading: immutable.</remarks>
/// <param name="FromLineNumber">Before what line the insertion began.</param>
/// <param name="FromLineNumberPostEdit">The actual start line number in the updated buffer where the newly inserted content can be found.</param>
/// <param name="Count">The count of inserted lines.</param>
public sealed record ModelRawLinesInserted(int FromLineNumber, int FromLineNumberPostEdit, int Count) : ModelRawChange
{
    /// <inheritdoc/>
    public override RawContentChangedType ChangeType => RawContentChangedType.LinesInserted;

    /// <summary>The last inserted line, before the change (<see cref="FromLineNumber"/> + <see cref="Count"/> - 1).</summary>
    public int ToLineNumber => FromLineNumber + Count - 1;

    /// <summary>The actual end line number of the insertion in the updated buffer.</summary>
    public int ToLineNumberPostEdit => FromLineNumberPostEdit + Count - 1;
}

/// <summary>The end of line sequence has changed (Monaco: <c>ModelRawEOLChanged</c>).</summary>
/// <remarks>Threading: immutable.</remarks>
public sealed record ModelRawEolChanged : ModelRawChange
{
    /// <inheritdoc/>
    public override RawContentChangedType ChangeType => RawContentChangedType.EolChanged;
}
