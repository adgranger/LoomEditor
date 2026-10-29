// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetVariables.ts (CompositeSnippetVariableResolver)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Snippets;

/// <summary>Asks its resolvers in order and keeps the first answer (Monaco: <c>CompositeSnippetVariableResolver</c>).</summary>
/// <remarks>An empty string is an answer: it wins over the resolvers after it. Threading: UI thread only; resolvers are created per insertion.</remarks>
internal sealed class CompositeVariableResolver(params IVariableResolver[] delegates) : IVariableResolver
{
    /// <inheritdoc />
    public string? Resolve(Variable variable)
    {
        foreach (IVariableResolver resolver in delegates)
        {
            if (resolver.Resolve(variable) is { } value)
            {
                return value;
            }
        }

        return null;
    }
}
