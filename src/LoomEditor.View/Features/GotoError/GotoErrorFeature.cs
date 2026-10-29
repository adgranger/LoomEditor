// Ported from microsoft/vscode: src/vs/editor/contrib/gotoError/browser/gotoError.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.GotoError;

/// <summary>Placeholder until P10-28 adds go to next / previous problem.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class GotoErrorFeature : IEditorFeature
{
    /// <inheritdoc />
    public string Id => "loom.gotoError";

    /// <inheritdoc />
    public void Attach(IEditorContext context) => ArgumentNullException.ThrowIfNull(context);

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
