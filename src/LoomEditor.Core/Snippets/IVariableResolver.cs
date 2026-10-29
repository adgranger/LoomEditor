// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetParser.ts (VariableResolver)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Snippets;

/// <summary>Gives a value to the variables of a snippet (Monaco: <c>VariableResolver</c>).</summary>
/// <remarks>Threading: depends on the implementation.</remarks>
internal interface IVariableResolver
{
    /// <summary>The value of <paramref name="variable"/>, or <see langword="null"/> when the variable is unknown.</summary>
    string? Resolve(Variable variable);
}
