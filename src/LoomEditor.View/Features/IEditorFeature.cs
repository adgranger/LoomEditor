// Inspired by microsoft/vscode: src/vs/editor/common/editorCommon.ts (IEditorContribution)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features;

/// <summary>
/// One feature of the editor (find, folding, bracket matching…). Each feature lives in its own folder
/// under <c>Features/</c> and is listed in <see cref="EditorFeatures.Default"/>.
/// </summary>
/// <remarks>
/// <para>
/// Lifecycle: the editor creates the feature, calls <see cref="Attach"/> once, and calls
/// <see cref="IDisposable.Dispose"/> when the editor is unloaded. In <c>Dispose</c>, undo everything
/// <see cref="Attach"/> did: unsubscribe from events and dispose command registrations.
/// </para>
/// <para>Threading: <see cref="Attach"/> and <c>Dispose</c> are called on the UI thread.</para>
/// <para>How to write one: docs/how-to/add-a-feature.md.</para>
/// </remarks>
public interface IEditorFeature : IDisposable
{
    /// <summary>Unique id of the feature, e.g. <c>"loom.folding"</c>.</summary>
    string Id { get; }

    /// <summary>
    /// Connects the feature to an editor: subscribe to events, register commands.
    /// </summary>
    /// <param name="context">The editor this feature is attached to.</param>
    void Attach(IEditorContext context);
}
