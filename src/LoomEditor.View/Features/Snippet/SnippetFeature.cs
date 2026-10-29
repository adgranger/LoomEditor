// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetController2.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Snippet;

/// <summary>Snippets: tab stops and placeholders (Monaco: <c>SnippetController2</c>).</summary>
/// <remarks>
/// Placeholder: until P10-24 (the session) and P10-25 (the controller and the keys) the feature does nothing; only
/// <see cref="OneSnippet"/> exists. Threading: UI thread only.
/// </remarks>
internal sealed class SnippetFeature : IEditorFeature
{
    /// <inheritdoc />
    public string Id => "loom.snippet";

    /// <inheritdoc />
    public void Attach(IEditorContext context) => ArgumentNullException.ThrowIfNull(context);

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
