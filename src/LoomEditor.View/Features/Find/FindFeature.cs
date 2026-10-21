// Inspired by microsoft/vscode: src/vs/editor/contrib/find/browser/findController.ts (CommonFindController)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Find;

/// <summary>
/// Find and replace (Ctrl+F, Ctrl+H, F3…). For now only the shared state and the option records exist; the find
/// model, the controller and the commands come with the next cards.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
public sealed class FindFeature : IEditorFeature
{
    /// <inheritdoc />
    public string Id => "loom.find";

    /// <inheritdoc />
    public void Attach(IEditorContext context) => ArgumentNullException.ThrowIfNull(context);

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
