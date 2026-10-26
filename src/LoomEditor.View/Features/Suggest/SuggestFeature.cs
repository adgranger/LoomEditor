// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggestController.ts (the registered commands, context keys)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Commands;
using LoomEditor.View.Suggest;

namespace LoomEditor.View.Features.Suggest;

/// <summary>Completion from the keyboard: owns the <see cref="SuggestController"/>, its commands and the context flags.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class SuggestFeature : IEditorFeature
{
    private readonly List<IDisposable> _registrations = [];
    private SuggestController? _controller;
    private EditorContext? _context;

    /// <inheritdoc />
    public string Id => "loom.suggest";

    /// <summary>The controller, or <see langword="null"/> before <see cref="Attach"/>.</summary>
    internal SuggestController? Controller => _controller;

    /// <summary>The options of the completion sessions; a change applies to the next session, not to the one in progress.</summary>
    public SuggestModelOptions Options { get; set; } = SuggestModelOptions.Default;

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context is not EditorContext editorContext || context.GetLanguageFeatures() is null)
        {
            return;
        }

        var controller = new SuggestController(context, Options);
        _controller = controller;
        _context = editorContext;
        controller.List.Changed += OnStateChanged;
        controller.Model.DidSuggest += OnStateChanged;
        controller.Model.DidCancel += OnStateChanged;

        Register(SuggestCommands.TriggerSuggest, () =>
        {
            // Monaco's precondition (writable && hasCompletionItemProvider && !suggestWidgetVisible): command execution is not filtered by `when`
            if (!controller.IsVisible && !editorContext.ViewModel.IsReadOnly && HasProvider(editorContext))
            {
                controller.Trigger();
            }
        });
        Register(SuggestCommands.AcceptSelectedSuggestion, () => WhenVisible(() => controller.AcceptSelected()));
        Register(SuggestCommands.HideSuggestWidget, () => WhenVisible(controller.Cancel));
        Register(SuggestCommands.SelectNextSuggestion, () => WhenVisible(() => controller.SelectNext()));
        Register(SuggestCommands.SelectPrevSuggestion, () => WhenVisible(() => controller.SelectPrevious()));
        Register(SuggestCommands.SelectNextPageSuggestion, () => WhenVisible(() => controller.SelectNextPage()));
        Register(SuggestCommands.SelectPrevPageSuggestion, () => WhenVisible(() => controller.SelectPreviousPage()));
        Register(SuggestCommands.SelectFirstSuggestion, () => WhenVisible(() => controller.SelectFirst()));
        Register(SuggestCommands.SelectLastSuggestion, () => WhenVisible(() => controller.SelectLast()));

        void Register(string id, Action run) => _registrations.Add(context.Commands.Register(new EditorCommand(id, run)));

        void WhenVisible(Action run)
        {
            if (controller.IsVisible)
            {
                run();
            }
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
        if (_controller is { } controller)
        {
            controller.List.Changed -= OnStateChanged;
            controller.Model.DidSuggest -= OnStateChanged;
            controller.Model.DidCancel -= OnStateChanged;
            controller.Dispose();
        }

        if (_context is not null)
        {
            _context.SuggestWidgetVisible = false;
            _context.SuggestWidgetMultipleSuggestions = false;
        }

        _controller = null;
        _context = null;
    }

    private static bool HasProvider(EditorContext context) => context.LanguageFeatures.CompletionProviders.Has(context.GetLanguageId());

    // Subscribed after the controller's own handlers, so the presenter has already been shown or hidden when this runs.
    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (_controller is { } controller && _context is { } context)
        {
            context.SuggestWidgetVisible = controller.IsVisible;
            context.SuggestWidgetMultipleSuggestions = controller.List.Items.Count > 1;
        }
    }
}
