// Ported from microsoft/vscode: src/vs/editor/common/textModelEvents.ts (IModelDecorationsChangedEvent)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.Events;

/// <summary>Describes that the decorations of the model changed (Monaco: <c>IModelDecorationsChangedEvent</c>).</summary>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal sealed class ModelDecorationsChangedEventArgs(bool affectsMinimap, bool affectsOverviewRuler, bool affectsGlyphMargin, bool affectsLineNumber) : EventArgs
{
    public bool AffectsMinimap { get; } = affectsMinimap;

    public bool AffectsOverviewRuler { get; } = affectsOverviewRuler;

    public bool AffectsGlyphMargin { get; } = affectsGlyphMargin;

    public bool AffectsLineNumber { get; } = affectsLineNumber;
}
