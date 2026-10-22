// Ported from microsoft/vscode: src/vs/editor/common/modelLineProjectionData.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;

namespace LoomEditor.View.Layout;

/// <summary>The injected text found at an output position (Monaco: <c>InjectedText</c>).</summary>
/// <param name="Options">The options of the injected text.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record InjectedText(ModelDecorationInjectedTextOptions Options);
