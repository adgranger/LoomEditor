// Ported from microsoft/vscode: src/vs/editor/contrib/hover/browser/hoverActions.ts (ShowOrFocusHoverAction, HideContentHoverAction)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Commands;
using LoomEditor.View.Hover;

namespace LoomEditor.View.Features.Hover;

/// <summary>Shows hovers (markers and provider answers) under the mouse and at the cursor.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class HoverFeature : IEditorFeature
{
    private readonly List<IDisposable> _registrations = [];
    private HoverController? _controller;
    private EditorHoverOptions _options = EditorHoverOptions.Default;

    /// <inheritdoc />
    public string Id => "loom.hover";

    /// <summary>The controller, or <see langword="null"/> before <see cref="Attach"/>.</summary>
    internal HoverController? Controller => _controller;

    /// <summary>The hover options.</summary>
    internal EditorHoverOptions Options
    {
        get => _options;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _options = value;
            if (_controller is not null)
            {
                _controller.Options = value;
            }
        }
    }

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.GetViewModel() is null)
        {
            return;
        }

        var controller = new HoverController(context, _options);
        _controller = controller;
        _registrations.Add(context.Commands.Register(new EditorCommand(HoverCommands.ShowHover, controller.ShowAtCursor)));
        _registrations.Add(context.Commands.Register(new EditorCommand(HoverCommands.HideHover, controller.Hide)));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (IDisposable registration in _registrations)
        {
            registration.Dispose();
        }

        _registrations.Clear();
        _controller?.Dispose();
        _controller = null;
    }
}
