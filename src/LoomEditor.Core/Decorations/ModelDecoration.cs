// Ported from microsoft/vscode: src/vs/editor/common/model.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Decorations;

/// <summary>A decoration of the model (Monaco: <c>IModelDecoration</c>, which is the interval node itself).</summary>
/// <remarks>Threading: immutable snapshot; thread-safe.</remarks>
internal readonly record struct ModelDecoration(string Id, int OwnerId, Range Range, ModelDecorationOptions Options);
