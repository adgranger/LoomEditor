// Ported from microsoft/vscode: src/vs/editor/common/model.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Decorations;

/// <summary>A decoration to add (Monaco: <c>IModelDeltaDecoration</c>).</summary>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal readonly record struct ModelDeltaDecoration(Range Range, ModelDecorationOptions Options);
