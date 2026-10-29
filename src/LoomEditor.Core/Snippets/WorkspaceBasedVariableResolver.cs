// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetVariables.ts (WorkspaceBasedVariableResolver)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Snippets;

/// <summary>Resolves <c>WORKSPACE_NAME</c> and <c>WORKSPACE_FOLDER</c> from the host (Monaco: <c>WorkspaceBasedVariableResolver</c>).</summary>
/// <remarks>Behavior difference: the host gives the name and the folder, there is no URI handling. Threading: UI thread only; resolvers are created per insertion.</remarks>
internal sealed class WorkspaceBasedVariableResolver(ISnippetVariableHost host) : IVariableResolver
{
    /// <inheritdoc />
    public string? Resolve(Variable variable)
    {
        ArgumentNullException.ThrowIfNull(variable);

        return variable.Name switch
        {
            "WORKSPACE_NAME" => host.WorkspaceName,
            "WORKSPACE_FOLDER" => host.WorkspaceFolder,
            _ => null,
        };
    }
}
