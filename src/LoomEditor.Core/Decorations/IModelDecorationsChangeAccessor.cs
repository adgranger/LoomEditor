// Ported from microsoft/vscode: src/vs/editor/common/model.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Decorations;

/// <summary>An accessor that can add, change or remove model decorations (Monaco: <c>IModelDecorationsChangeAccessor</c>).</summary>
/// <remarks>
/// It is valid only inside the callback of <c>TextModel.ChangeDecorations</c>: afterwards every method throws
/// <see cref="InvalidOperationException"/>. Threading: UI thread only.
/// </remarks>
internal interface IModelDecorationsChangeAccessor
{
    /// <summary>Adds a decoration and returns its id.</summary>
    string AddDecoration(Range range, ModelDecorationOptions options);

    /// <summary>Changes the range of an existing decoration (an unknown id is ignored).</summary>
    void ChangeDecoration(string id, Range newRange);

    /// <summary>Changes the options of an existing decoration (an unknown id is ignored).</summary>
    void ChangeDecorationOptions(string id, ModelDecorationOptions options);

    /// <summary>Removes a decoration (an unknown id is ignored).</summary>
    void RemoveDecoration(string id);

    /// <summary>
    /// Replaces the decorations <paramref name="oldDecorationIds"/> with <paramref name="newDecorations"/>, reusing
    /// nodes where possible, and returns the ids of the new decorations.
    /// </summary>
    string[] DeltaDecorations(IReadOnlyList<string> oldDecorationIds, IReadOnlyList<ModelDeltaDecoration> newDecorations);
}
