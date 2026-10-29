// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetVariables.ts (the services the resolvers read)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Snippets;

/// <summary>What the variable resolvers need from the host: the file, the workspace and the clipboard (ADR 0013, decision 10).</summary>
/// <remarks>Every value is <see langword="null"/> when the host does not know it. Threading: UI thread only.</remarks>
internal interface ISnippetVariableHost
{
    /// <summary>A host that knows nothing: every value is <see langword="null"/>.</summary>
    static ISnippetVariableHost None => NoSnippetVariableHost.Instance;

    /// <summary>The path of the document (Monaco: the model URI's <c>fsPath</c>).</summary>
    string? FilePath { get; }

    /// <summary>The path of the document relative to the workspace (Monaco: <c>getUriLabel(uri, { relative: true })</c>).</summary>
    string? RelativeFilePath { get; }

    /// <summary>The name of the workspace.</summary>
    string? WorkspaceName { get; }

    /// <summary>The folder of the workspace.</summary>
    string? WorkspaceFolder { get; }

    /// <summary>The text of the clipboard (Monaco: <c>IReadClipboardText</c>).</summary>
    string? ReadClipboardText();
}

/// <summary>The host that knows nothing.</summary>
/// <remarks>Threading: stateless.</remarks>
internal sealed class NoSnippetVariableHost : ISnippetVariableHost
{
    public static NoSnippetVariableHost Instance { get; } = new();

    public string? FilePath => null;

    public string? RelativeFilePath => null;

    public string? WorkspaceName => null;

    public string? WorkspaceFolder => null;

    public string? ReadClipboardText() => null;
}
