// Ported from microsoft/vscode: src/vs/editor/contrib/gotoSymbol/browser/goToCommands.ts (DefinitionAction), link/goToDefinitionAtPosition.ts (Ctrl+click)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Commands;
using LoomEditor.View.Controller;

namespace LoomEditor.View.Features.GotoDefinition;

/// <summary>Go to definition: <c>F12</c> and <c>Ctrl+click</c>.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class GotoDefinitionFeature : IEditorFeature
{
    private readonly List<IDisposable> _registrations = [];
    private GotoDefinitionController? _controller;
    private ViewUserInputEvents? _events;

    /// <inheritdoc />
    public string Id => "loom.gotoDefinition";

    /// <summary>The controller, or <see langword="null"/> before <see cref="Attach"/>.</summary>
    internal GotoDefinitionController? Controller => _controller;

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context is not EditorContext)
        {
            return;
        }

        var controller = new GotoDefinitionController(context);
        _controller = controller;
        _registrations.Add(context.Commands.Register(new EditorCommand(GotoDefinitionCommands.RevealDefinition, () => controller.Run())));
        _events = context.GetUserInputEvents();
        if (_events is not null)
        {
            _events.MouseUp += OnMouseUp;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (IDisposable registration in _registrations)
        {
            registration.Dispose();
        }

        _registrations.Clear();
        if (_events is not null)
        {
            _events.MouseUp -= OnMouseUp;
            _events = null;
        }

        _controller?.Dispose();
        _controller = null;
    }

    // Monaco: GotoDefinitionAtPositionEditorContribution.isEnabled (the click count is not checked: the control always reports 4)
    private void OnMouseUp(object? sender, EditorMouseEventArgs e)
    {
        if (_controller is { } controller
            && e.Event is { LeftButton: true, CtrlKey: true, AltKey: false, ShiftKey: false }
            && e.Target is { Type: MouseTargetType.ContentText, Position: { } position })
        {
            controller.Run(position);
        }
    }
}
