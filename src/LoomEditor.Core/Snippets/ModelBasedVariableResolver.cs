// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetVariables.ts (ModelBasedVariableResolver)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Snippets;

/// <summary>Resolves the file variables from the host's file path (Monaco: <c>ModelBasedVariableResolver</c>).</summary>
/// <remarks>
/// Behavior difference: the paths come from <see cref="ISnippetVariableHost"/> and are handled with <see cref="Path"/>
/// (no label service). Every value is <see langword="null"/> when the host gives <see langword="null"/>.
/// Threading: UI thread only; resolvers are created per insertion.
/// </remarks>
internal sealed class ModelBasedVariableResolver(ISnippetVariableHost host) : IVariableResolver
{
    /// <inheritdoc />
    public string? Resolve(Variable variable)
    {
        ArgumentNullException.ThrowIfNull(variable);

        if (variable.Name == "RELATIVE_FILEPATH")
        {
            return host.RelativeFilePath;
        }

        if (host.FilePath is not { } filePath)
        {
            return null;
        }

        switch (variable.Name)
        {
            case "TM_FILENAME":
                return Path.GetFileName(filePath);
            case "TM_FILENAME_BASE":
                string name = Path.GetFileName(filePath);
                int index = name.LastIndexOf('.');
                return index <= 0 ? name : name[..index];
            case "TM_DIRECTORY":
                return Path.GetDirectoryName(filePath) is { Length: > 0 } directory ? directory : string.Empty;
            case "TM_DIRECTORY_BASE":
                return Path.GetDirectoryName(filePath) is { Length: > 0 } baseDirectory ? Path.GetFileName(baseDirectory) : string.Empty;
            case "TM_FILEPATH":
                return filePath;
            default:
                return null;
        }
    }
}
