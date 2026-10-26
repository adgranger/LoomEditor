// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/completionModel.ts (LineContext)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Suggest;

/// <summary>What the user typed on the line before the cursor and how many characters were added since the request (Monaco: <c>LineContext</c>).</summary>
/// <remarks>Threading: immutable value type, safe from any thread.</remarks>
internal readonly record struct CompletionLineContext(string LeadingLineContent, int CharacterCountDelta);
