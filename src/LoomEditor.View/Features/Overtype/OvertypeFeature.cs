// Ported from microsoft/vscode: src/vs/editor/common/inputMode.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Accessibility;
using LoomEditor.View.Commands;
using LoomEditor.View.Cursors;

namespace LoomEditor.View.Features.Overtype;

/// <summary>Registers <see cref="OvertypeCommands.ToggleOvertype"/>: flips <see cref="EditorContext.InputMode"/> and announces it.</summary>
/// <remarks>Threading: UI thread only.</remarks>
public sealed class OvertypeFeature : IEditorFeature
{
    private IDisposable? _registration;

    /// <inheritdoc />
    public string Id => "loom.overtype";

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context is not EditorContext editorContext)
        {
            return;
        }

        _registration = context.Commands.Register(new EditorCommand(
            OvertypeCommands.ToggleOvertype,
            () =>
            {
                bool toOvertype = editorContext.InputMode == EditorInputMode.Insert;
                editorContext.InputMode = toOvertype ? EditorInputMode.Overtype : EditorInputMode.Insert;
                context.GetAnnouncer().Announce(toOvertype ? AccessibilityMessages.OvertypeModeOn : AccessibilityMessages.InsertModeOn);
            },
            () => !editorContext.IsReadOnly));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _registration?.Dispose();
        _registration = null;
    }
}
