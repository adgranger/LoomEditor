// Ported from microsoft/vscode: src/vs/editor/common/model/editStack.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Undo;

/// <summary>What one undo step remembers: the text changes, the EOL, the alternative version id and the cursor state, before and after.</summary>
/// <remarks>
/// <para>Behavior difference: Monaco's <c>serialize</c> / <c>deserialize</c> (binary form of a closed step) are not ported.</para>
/// <para>Threading: UI thread only; owned by one <see cref="SingleModelEditStackElement"/>.</para>
/// </remarks>
internal sealed class SingleModelEditStackData
{
    public SingleModelEditStackData(
        int beforeVersionId,
        int afterVersionId,
        EndOfLineSequence beforeEol,
        EndOfLineSequence afterEol,
        IReadOnlyList<Selection>? beforeCursorState,
        IReadOnlyList<Selection>? afterCursorState,
        IReadOnlyList<TextChange> changes)
    {
        BeforeVersionId = beforeVersionId;
        AfterVersionId = afterVersionId;
        BeforeEol = beforeEol;
        AfterEol = afterEol;
        BeforeCursorState = beforeCursorState;
        AfterCursorState = afterCursorState;
        Changes = changes;
    }

    public int BeforeVersionId { get; }

    public int AfterVersionId { get; private set; }

    public EndOfLineSequence BeforeEol { get; }

    public EndOfLineSequence AfterEol { get; private set; }

    public IReadOnlyList<Selection>? BeforeCursorState { get; }

    public IReadOnlyList<Selection>? AfterCursorState { get; private set; }

    public IReadOnlyList<TextChange> Changes { get; private set; }

    public static SingleModelEditStackData Create(TextModel model, IReadOnlyList<Selection>? beforeCursorState)
    {
        int alternativeVersionId = model.AlternativeVersionId;
        EndOfLineSequence eol = model.EndOfLineSequence;
        return new SingleModelEditStackData(
            alternativeVersionId, alternativeVersionId, eol, eol, beforeCursorState, beforeCursorState, []);
    }

    public void Append(
        IReadOnlyList<TextChange> textChanges,
        EndOfLineSequence afterEol,
        int afterVersionId,
        IReadOnlyList<Selection>? afterCursorState)
    {
        if (textChanges.Count > 0)
        {
            Changes = TextChangeCompressor.CompressConsecutiveTextChanges(Changes, textChanges);
        }

        AfterEol = afterEol;
        AfterVersionId = afterVersionId;
        AfterCursorState = afterCursorState;
    }
}
