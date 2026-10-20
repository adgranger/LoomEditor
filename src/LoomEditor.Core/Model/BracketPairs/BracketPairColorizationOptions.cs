// Ported from microsoft/vscode: src/vs/editor/common/model.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.BracketPairs;

/// <summary>How brackets are colored (Monaco: <c>BracketPairColorizationOptions</c>); the defaults are those of VS Code.</summary>
/// <param name="Enabled">Whether brackets are colored by nesting level.</param>
/// <param name="IndependentColorPoolPerBracketType">Whether each bracket type cycles over the colors on its own.</param>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal sealed record BracketPairColorizationOptions(bool Enabled = true, bool IndependentColorPoolPerBracketType = false);
